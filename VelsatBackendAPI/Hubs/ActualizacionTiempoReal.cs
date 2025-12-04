using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;

namespace VelsatBackendAPI.Hubs
{
    public class ActualizacionTiempoReal : Hub
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<ActualizacionTiempoReal> _hubContext;
        private readonly ILogger<ActualizacionTiempoReal> _logger; // ⭐ NUEVO
        private static readonly Dictionary<string, Timer> _userTimers = new Dictionary<string, Timer>();
        private static readonly object _lockObject = new object();

        public ActualizacionTiempoReal(
            IServiceScopeFactory serviceScopeFactory,
            IHubContext<ActualizacionTiempoReal> hubContext,
            ILogger<ActualizacionTiempoReal> logger) // ⭐ NUEVO
        {
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
            _logger = logger; // ⭐ NUEVO
        }

        public async Task UnirGrupo()
        {
            var username = GetUsernameFromRoute();

            if (string.IsNullOrEmpty(username))
            {
                await Clients.Caller.SendAsync("Error", "Username no encontrado en la ruta");
                return;
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, username);
                IniciarTimer(username);
                await Clients.Caller.SendAsync("ConectadoExitosamente", username);

                _logger.LogInformation("[SignalR] Usuario {Username} se unió al grupo desde la ruta", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error uniendo al grupo para usuario {Username}", username);
                await Clients.Caller.SendAsync("Error", $"Error al unirse al grupo: {ex.Message}");
            }
        }

        public async Task DejarGrupo()
        {
            var username = GetUsernameFromRoute();

            if (string.IsNullOrEmpty(username))
                return;

            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
                DetenerTimer(username);

                _logger.LogInformation("[SignalR] Usuario {Username} dejó el grupo", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error dejando el grupo para usuario {Username}", username);
            }
        }

        public override async Task OnConnectedAsync()
        {
            var username = GetUsernameFromRoute();
            _logger.LogInformation("[SignalR] Cliente conectado: {ConnectionId}, Username: {Username}",
                Context.ConnectionId, username);

            if (!string.IsNullOrEmpty(username))
            {
                await UnirGrupo();
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = GetUsernameFromRoute();

            if (!string.IsNullOrEmpty(username))
            {
                DetenerTimer(username);
                _logger.LogInformation("[SignalR] Usuario {Username} desconectado, timer detenido", username);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private string GetUsernameFromRoute()
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext?.Request.RouteValues.TryGetValue("username", out var usernameObj) == true)
                {
                    return usernameObj?.ToString() ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error obteniendo username de la ruta");
                return string.Empty;
            }
        }

        private void IniciarTimer(string username)
        {
            if (string.IsNullOrEmpty(username))
                return;

            lock (_lockObject)
            {
                // ⭐ MEJORADO: Detener timer existente antes de crear uno nuevo
                if (_userTimers.ContainsKey(username))
                {
                    try
                    {
                        _userTimers[username].Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SignalR] Error al disponer timer existente para {Username}", username);
                    }
                    _userTimers.Remove(username);
                }

                // ⭐ MEJORADO: Usar callback asíncrono con manejo de excepciones
                var timer = new Timer(
                    async _ =>
                    {
                        try
                        {
                            await EnviarDatosDirectamente(username);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[SignalR] Error no capturado en timer para {Username}", username);
                        }
                    },
                    null,
                    TimeSpan.FromSeconds(1),      // Primer envío después de 1 segundo
                    TimeSpan.FromSeconds(5));     // Luego cada 5 segundos

                _userTimers[username] = timer;
            }

            _logger.LogInformation("[SignalR] Timer iniciado para: {Username}", username);
        }

        private void DetenerTimer(string username)
        {
            if (string.IsNullOrEmpty(username))
                return;

            lock (_lockObject)
            {
                if (_userTimers.ContainsKey(username))
                {
                    try
                    {
                        _userTimers[username].Dispose();
                        _userTimers.Remove(username);
                        _logger.LogInformation("[SignalR] Timer detenido para: {Username}", username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SignalR] Error al detener timer para {Username}", username);
                    }
                }
            }
        }

        // ⭐ OPTIMIZADO: Usa ReadOnlyUnitOfWork (sin transacciones)
        private async Task EnviarDatosDirectamente(string username)
        {
            IServiceScope? scope = null;
            IReadOnlyUnitOfWork? readOnlyUow = null;

            try
            {
                scope = _serviceScopeFactory.CreateScope();

                // ✅ Obtener ReadOnlyUnitOfWork (sin transacciones)
                readOnlyUow = scope.ServiceProvider.GetRequiredService<IReadOnlyUnitOfWork>();

                var datosCargaActualizados = await readOnlyUow.DatosCargainicialService
                    .ObtenerDatosCargaInicialAsync(username);

                datosCargaActualizados.FechaActual = DateTime.Now;

                await _hubContext.Clients.Group(username)
                    .SendAsync("ActualizarDatos", datosCargaActualizados);

                _logger.LogDebug("[SignalR] ✅ Datos enviados correctamente a {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] ❌ Error enviando datos para {Username}", username);

                if (EsErrorCritico(ex))
                {
                    _logger.LogCritical("[SignalR] 🚨 Error crítico - Deteniendo timer para {Username}", username);
                    DetenerTimer(username);
                }
            }
            finally
            {
                // ✅ CRÍTICO: Disponer en orden inverso
                readOnlyUow?.Dispose();
                scope?.Dispose();
            }
        }

        // ⭐ NUEVO: Método helper para detectar errores críticos
        private bool EsErrorCritico(Exception ex)
        {
            // Errores que justifican detener el timer
            return ex is OutOfMemoryException ||
                   ex is StackOverflowException ||
                   ex is AccessViolationException ||
                   (ex.InnerException != null && EsErrorCritico(ex.InnerException));
        }
    }
}