using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VelsatBackendAPI.Data.Services
{
    public record TraccarResultado(bool Exito, int? DriverId = null, string? Error = null);

    public interface ITraccarService
    {
        Task<TraccarResultado> CrearConductorAsync(string nombre, string uniqueId);

        Task<TraccarResultado> EditarConductorAsync(int driverId, string nombre, string uniqueId);

        Task<TraccarResultado> EliminarConductorAsync(int driverId);
    }

    // Administra los conductores (drivers) en Traccar con Basic Auth. Las credenciales salen de la
    // sección "Traccar" de appsettings.json. Nunca lanza: los fallos se devuelven en el resultado
    // para que no deshagan lo que ya se guardó en la base de datos.
    public class TraccarService : ITraccarService
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TraccarService> _logger;

        public TraccarService(IConfiguration configuration, HttpClient httpClient, ILogger<TraccarService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<TraccarResultado> CrearConductorAsync(string nombre, string uniqueId) =>
            EnviarAsync(HttpMethod.Post, "api/drivers", new { name = nombre, uniqueId }, leerId: true);

        public Task<TraccarResultado> EditarConductorAsync(int driverId, string nombre, string uniqueId) =>
            EnviarAsync(HttpMethod.Put, $"api/drivers/{driverId}", new { id = driverId, name = nombre, uniqueId });

        public Task<TraccarResultado> EliminarConductorAsync(int driverId) =>
            EnviarAsync(HttpMethod.Delete, $"api/drivers/{driverId}");

        private async Task<TraccarResultado> EnviarAsync(HttpMethod metodo, string ruta, object? cuerpo = null, bool leerId = false)
        {
            string? url = _configuration["Traccar:Url"];
            string? usuario = _configuration["Traccar:User"];
            string? clave = _configuration["Traccar:Password"];

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            {
                _logger.LogError("Traccar: falta configurar Traccar:Url, Traccar:User o Traccar:Password en appsettings.json");
                return new TraccarResultado(false, Error: "Traccar no está configurado en el servidor.");
            }

            using var cts = new CancellationTokenSource(Timeout);

            try
            {
                using var request = new HttpRequestMessage(metodo, $"{url.TrimEnd('/')}/{ruta}");

                string credenciales = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{clave}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciales);

                if (cuerpo != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json");
                }

                using var response = await _httpClient.SendAsync(request, cts.Token);
                string respuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Traccar: {Metodo} {Ruta} falló (HTTP {Status}): {Respuesta}",
                        metodo, ruta, (int)response.StatusCode, respuesta);
                    return new TraccarResultado(false, Error: $"Traccar respondió HTTP {(int)response.StatusCode}.");
                }

                if (!leerId)
                {
                    return new TraccarResultado(true);
                }

                using var json = JsonDocument.Parse(respuesta);
                return new TraccarResultado(true, json.RootElement.GetProperty("id").GetInt32());
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Traccar: timeout en {Metodo} {Ruta}", metodo, ruta);
                return new TraccarResultado(false, Error: "Traccar no respondió a tiempo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Traccar: error en {Metodo} {Ruta}", metodo, ruta);
                return new TraccarResultado(false, Error: "No se pudo comunicar con Traccar.");
            }
        }
    }
}
