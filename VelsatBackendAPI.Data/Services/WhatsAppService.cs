using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

        public async Task<bool> EnviarMensajeAsync(string telefono, string mensaje)
        {
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

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var response = await _httpClient.PostAsync(baseUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"WhatsApp: fallo al enviar a {telefono} (HTTP {(int)response.StatusCode}): {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"WhatsApp: error de red al enviar a {telefono}: {ex.Message}");
                return false;
            }
        }
    }
}
