using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Data.Services
{
    public interface ICorreoService
    {
        // Devuelve false si la API de correos no pudo enviarlo (el detalle queda en el log).
        Task<bool> EnviarAsync(string destinatario, string asunto, string html);
    }

    // Envía correos por la API propia (CorreosVelsat). Configuración:
    //   Correos:Url    (por defecto https://do.velsat.pe:8443/correos/api/email/send)
    //   Correos:ApiKey (variable de entorno Correos__ApiKey)
    // Sin "from": la API usa su remitente por defecto (solo acepta dominios permitidos, p. ej. velsat.pe).
    public class CorreoService : ICorreoService
    {
        private const string RemitenteNombre = "Velsat SAC";
        private static readonly TimeSpan TimeoutEnvio = TimeSpan.FromSeconds(15);

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IConfiguration configuration, HttpClient httpClient, ILogger<CorreoService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> EnviarAsync(string destinatario, string asunto, string html)
        {
            using var cts = new CancellationTokenSource(TimeoutEnvio);

            try
            {
                string url = _configuration["Correos:Url"] ?? "https://do.velsat.pe:8443/correos/api/email/send";
                string apiKey = _configuration["Correos:ApiKey"] ?? "";

                var payload = new
                {
                    fromName = RemitenteNombre,
                    to = new[] { destinatario },
                    subject = asunto,
                    html
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-API-Key", apiKey);

                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Correos: fallo al enviar a {Destinatario} (HTTP {Status}): {Body}",
                        destinatario, (int)response.StatusCode, body);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Correos: error enviando a {Destinatario}", destinatario);
                return false;
            }
        }
    }

    public static class CorreoPlantillas
    {
        public static string ResetPassword(string nombre, string enlace, int minutosValidez)
        {
            string saludo = string.IsNullOrWhiteSpace(nombre) ? "Hola" : $"Hola {WebUtility.HtmlEncode(nombre)}";
            string href = WebUtility.HtmlEncode(enlace);

            return $@"
<html>
  <body style='font-family: Arial, sans-serif; color: #333; margin: 0; padding: 0;'>
    <div style='max-width: 600px; margin: auto; background-color: #f4f4f4; padding: 20px;'>
      <h2 style='margin: 0 0 15px 0; font-size: 14px; color: #001d3d; text-align: center;'>CENTRAL DE MONITOREO Y GESTIÓN</h2>
      <div style='background-color: #fff; border-radius: 8px; padding: 24px;'>
        <p style='font-size: 14px;'>{saludo},</p>
        <p style='font-size: 13px;'>Recibimos una solicitud para cambiar la contraseña de tu cuenta. Haz clic en el botón para elegir una nueva:</p>
        <p style='text-align: center; margin: 28px 0;'>
          <a href='{href}' style='background-color: #113EB9; color: #fff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-size: 13px; font-weight: bold;'>Cambiar contraseña</a>
        </p>
        <p style='font-size: 12px; color: #666;'>El enlace es válido por {minutosValidez} minutos y solo se puede usar una vez.</p>
        <p style='font-size: 12px; color: #666;'>Si el botón no funciona, copia y pega este enlace en tu navegador:<br/><span style='word-break: break-all;'>{href}</span></p>
        <p style='font-size: 12px; color: #666;'>Si no solicitaste este cambio, ignora este correo: tu contraseña seguirá siendo la misma.</p>
      </div>
      <p style='text-align: center; font-size: 11px; color: #001d3d; margin: 15px 0 0 0;'>© {DateTime.Now.Year} Velsat. Todos los derechos reservados.</p>
    </div>
  </body>
</html>";
        }
    }
}
