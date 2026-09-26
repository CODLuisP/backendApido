using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Data.Services
{
    public interface IWhatsAppService
    {
        Task<bool> EnviarMensajeAsync(string telefono, string mensaje);
    }

    // Envía mensajes de texto por WhatsApp a través del gateway propio (do.velsat.pe:8443), el
    // mismo que antes llamaba el front directamente. El teléfono ya debe venir normalizado
    // (51 + 9 dígitos); este servicio no valida ni normaliza el número.
    public class WhatsAppService : IWhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(IConfiguration configuration, HttpClient httpClient, ILogger<WhatsAppService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        // Timeout corto propio (independiente del Timeout global del HttpClient inyectado por
        // AddHttpClient) para que un gateway de WhatsApp caído o desvinculado no cuelgue la
        // request casi 100s: eso alargaba tanto la respuesta de InsertLote que el front la daba
        // por caída y reintentaba la carga completa, duplicando servicios.
        private static readonly TimeSpan TimeoutEnvio = TimeSpan.FromSeconds(10);

        public async Task<bool> EnviarMensajeAsync(string telefono, string mensaje)
        {
            using var cts = new CancellationTokenSource(TimeoutEnvio);

            try
            {
                string baseUrl = _configuration["WhatsApp:BaseUrl"];
                string apiKey = _configuration["WhatsApp:ApiKey"];

                var payload = new
                {
                    phone = telefono,
                    type = "texto",
                    text = mensaje
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // El header va en cada request (no en DefaultRequestHeaders) para que varios envíos
                // en paralelo (ver ServTurismoController.InsertLote) no se pisen entre sí.
                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = content };
                request.Headers.Add("x-api-key", apiKey);

                var response = await _httpClient.SendAsync(request, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"WhatsApp: fallo al enviar a {telefono} (HTTP {(int)response.StatusCode}): {error}");
                    return false;
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError($"WhatsApp: timeout ({TimeoutEnvio.TotalSeconds}s) al enviar a {telefono} (¿gateway desvinculado o caído?)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"WhatsApp: error de red al enviar a {telefono}: {ex.Message}");
                return false;
            }
        }
    }
}
