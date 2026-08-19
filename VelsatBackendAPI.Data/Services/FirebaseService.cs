using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Data.Services
{
    public readonly struct PushSendResult
    {
        public bool Success { get; }
        // true cuando Firebase confirma que el token ya no es válido (UNREGISTERED/NOT_FOUND):
        // el caller debe borrarlo de UserFCMTokens para no seguir intentando.
        public bool TokenInvalido { get; }

        public PushSendResult(bool success, bool tokenInvalido)
        {
            Success = success;
            TokenInvalido = tokenInvalido;
        }

        public static readonly PushSendResult Ok = new PushSendResult(true, false);
        public static readonly PushSendResult Fallo = new PushSendResult(false, false);
        public static readonly PushSendResult TokenMuerto = new PushSendResult(false, true);
    }

    public interface IFirebaseService
    {
        Task<PushSendResult> SendPushNotificationAsync(string fcmToken, string titulo, string mensaje);
    }

    // Réplica de VelsatMobile.Services.FirebaseService.FirebaseService (repo VelsatMobile/DocumentosNotificacionJob):
    // llama directo a la API v1 de FCM en vez de usar el SDK FirebaseAdmin.
    public class FirebaseService : IFirebaseService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FirebaseService> _logger;

        public FirebaseService(IConfiguration configuration,
                               HttpClient httpClient,
                               ILogger<FirebaseService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PushSendResult> SendPushNotificationAsync(string fcmToken, string titulo, string mensaje)
        {
            try
            {
                string accessToken = await GetAccessTokenAsync();

                var payload = new
                {
                    message = new
                    {
                        token = fcmToken,
                        notification = new
                        {
                            title = titulo,
                            body = mensaje
                        }
                    }
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                string projectId = _configuration["Firebase:ProjectId"];
                string url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Firebase error: {error}");

                    // FCM v1 responde 404/NOT_FOUND con errorCode UNREGISTERED cuando el token
                    // ya no existe (app desinstalada, token rotado, etc.). Ese token está muerto
                    // para siempre: no tiene sentido reintentarlo.
                    bool tokenInvalido = response.StatusCode == HttpStatusCode.NotFound
                        || error.Contains("UNREGISTERED")
                        || error.Contains("INVALID_ARGUMENT");

                    return tokenInvalido ? PushSendResult.TokenMuerto : PushSendResult.Fallo;
                }

                return PushSendResult.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando notificación Firebase: {ex.Message}");
                return PushSendResult.Fallo;
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            string jsonPath = _configuration["Firebase:CredentialsPath"];

            if (!Path.IsPathRooted(jsonPath))
                jsonPath = Path.Combine(AppContext.BaseDirectory, jsonPath);

            GoogleCredential credential;
            using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential
                    .FromStream(stream)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            }

            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }
    }
}
