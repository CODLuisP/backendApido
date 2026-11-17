using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Hubs;
using VelsatBackendAPI.Model.ServiciosRutas;

namespace VelsatBackendAPI.Services
{
    // Crea automáticamente los servicios de Rutas a partir de las plantillas (servicio_plantilla).
    //
    // Reglas:
    //  - Revisa cada N minutos (50 por defecto, configurable desde la web) y al arrancar. Es idempotente: servicio_plantilla_generado guarda
    //    cada (plantilla, fecha) ya creada, así que no duplica ni recrea uno que el operador eliminó.
    //  - Genera los servicios de hoy y, desde la hora de creación configurada (18:00 por defecto, hora Perú), los de mañana.
    //  - Solo genera si la plantilla está activa, toca ese día de la semana y está COMPLETA
    //    (hora, unidad y brevete de un conductor de rutas activo; nombre y celular se leen del conductor). Si falta algo, se omite.
    //  - No genera servicios cuya hora de salida ya pasó.
    //  - Usa la misma lógica de creación que la web (InsertServicio: genera el seguimiento por paradero).
    public class ServiciosRutasAutomaticosService : BackgroundService
    {
        private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ServiciosRutasHub> _hub;
        private readonly ILogger<ServiciosRutasAutomaticosService> _logger;

        public ServiciosRutasAutomaticosService(
            IServiceScopeFactory scopeFactory,
            IHubContext<ServiciosRutasHub> hub,
            ILogger<ServiciosRutasAutomaticosService> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(EsperaInicial, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await GenerarAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al generar los servicios automáticos de rutas");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(await LeerIntervaloAsync()), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Apagado normal de la aplicación.
            }
        }

        private async Task<int> LeerIntervaloAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var lectura = scope.ServiceProvider.GetRequiredService<IReadOnlyUnitOfWork>();
                return Math.Max(1, await lectura.ServiciosRutasRepository.GetIntervaloGeneracionMinimo());
            }
            catch
            {
                return 50;
            }
        }

        // Hora local de Perú (UTC-5, sin horario de verano), la misma convención del resto del backend.
        private static DateTime AhoraPeru() => DateTime.UtcNow.AddHours(-5);

        // ISO: lunes = 1 ... domingo = 7 (igual que la columna `dias` de la plantilla).
        private static int DiaIso(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

        private async Task GenerarAsync()
        {
            DateTime ahora = AhoraPeru();
            var fechas = new List<DateTime> { ahora.Date, ahora.Date.AddDays(1) };

            List<ServicioPlantilla> plantillas;
            HashSet<(long PlantillaId, DateTime Fecha)> generadas;
            Dictionary<string, TimeSpan> horasGeneracion;

            using (var scope = _scopeFactory.CreateScope())
            {
                var lectura = scope.ServiceProvider.GetRequiredService<IReadOnlyUnitOfWork>();
                plantillas = (await lectura.ServiciosRutasRepository.GetPlantillas(null, soloActivas: true)).ToList();
                generadas = await lectura.ServiciosRutasRepository.GetPlantillasGeneradas(fechas.Min(), fechas.Max());
                horasGeneracion = await lectura.ServiciosRutasRepository.GetHorasGeneracion();
            }

            int creados = 0;
            foreach (var fecha in fechas)
            {
                foreach (var plantilla in plantillas)
                {
                    // Sin hora, unidad o brevete no se genera. Si el brevete ya no corresponde a un conductor
                    // (dado de baja o brevete cambiado), el nombre no se resuelve y tampoco se genera.
                    if (!plantilla.EstaCompleta) continue;
                    if (!plantilla.Dias.Contains(DiaIso(fecha))) continue;

                    // Los de mañana solo desde la hora de creación de esa cuenta (editable desde la web).
                    if (fecha.Date > ahora.Date)
                    {
                        var horaCreacion = horasGeneracion.TryGetValue(plantilla.AccountID, out var h)
                            ? h : ServiciosRutasRepository.HoraGeneracionPorDefecto;
                        if (ahora.TimeOfDay < horaCreacion) continue;
                    }
                    if (generadas.Contains((plantilla.Id, fecha.Date))) continue;
                    if (fecha.Date + plantilla.HoraSalida!.Value <= ahora) continue;

                    try
                    {
                        if (await CrearServicioAsync(plantilla, fecha.Date))
                            creados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo crear el servicio automático de la plantilla {PlantillaId} para {Fecha:yyyy-MM-dd}",
                            plantilla.Id, fecha);
                    }
                }
            }

            if (creados > 0)
                _logger.LogInformation("Servicios automáticos de rutas creados: {Cantidad}", creados);
        }

        private async Task<bool> CrearServicioAsync(ServicioPlantilla p, DateTime fecha)
        {
            long servicioId;
            string deviceId = p.DeviceID!.Trim();

            // Un UnitOfWork por servicio: si uno falla, no arrastra a los demás.
            using (var scope = _scopeFactory.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                if (!await uow.ConfiguracionRepository.DispositivoPerteneceACuentaAsync(p.AccountID, deviceId))
                {
                    _logger.LogWarning("Plantilla {PlantillaId}: la unidad {DeviceId} no existe o no pertenece a la cuenta; se omite", p.Id, deviceId);
                    return false;
                }

                int paraderos = await uow.ServiciosRutasRepository.ContarParaderosDeRutaActiva(p.RutaId, p.AccountID);
                // Solo omite ESTA plantilla: si alguien ya creó a mano ese servicio (misma ruta, fecha y hora),
                // no se duplica; las demás plantillas siguen su curso.
                if (await uow.ServiciosRutasRepository.ExisteServicioDeRuta(p.AccountID, p.RutaId, fecha, p.HoraSalida!.Value))
                    return false;

                if (paraderos < 2)
                {
                    _logger.LogWarning("Plantilla {PlantillaId}: la ruta {RutaId} no existe, está inactiva o no tiene paraderos; se omite", p.Id, p.RutaId);
                    return false;
                }

                // Reserva primero: si otra ejecución ya lo creó, no se duplica.
                if (!await uow.ServiciosRutasRepository.ReservarGeneracion(p.Id, fecha))
                    return false;

                servicioId = await uow.ServiciosRutasRepository.InsertServicio(new ServicioRuta
                {
                    AccountID = p.AccountID,
                    DeviceID = deviceId,
                    RutaId = p.RutaId,
                    FechaSalida = fecha,
                    HoraSalida = p.HoraSalida,
                    ConductorNombre = p.ConductorNombre!.Trim(),
                    ConductorBrevete = p.ConductorBrevete!.Trim(),
                    ConductorCelular = p.ConductorCelular!.Trim(),
                    Observaciones = "Generado automáticamente",
                    CreatedBy = null
                });

                await uow.ServiciosRutasRepository.VincularGeneracion(p.Id, fecha, servicioId);
                uow.SaveChanges();
            }

            try
            {
                await _hub.Clients.Group(p.AccountID).SendAsync("ServicioActualizado",
                    new AvisoServicio { ServicioId = servicioId, Tipo = "CREADO", DeviceID = deviceId });
            }
            catch
            {
                // Es solo un aviso: la web igual se actualiza con su refresco.
            }

            return true;
        }
    }
}
