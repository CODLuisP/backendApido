using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Data.Services
{
    public interface IFirebaseService
    {
        Task<bool> SendPushNotificationAsync(string fcmToken, string titulo, string mensaje);
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

        public async Task<bool> SendPushNotificationAsync(string fcmToken, string titulo, string mensaje)
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
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando notificación Firebase: {ex.Message}");
                return false;
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
