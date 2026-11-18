using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Hubs;
using VelsatBackendAPI.Model.ServiciosRutas;

namespace VelsatBackendAPI.Controllers
{
    // Rutas con paraderos (geocercas), servicios programados sobre esas rutas y configuración.
    // La cuenta (accountID) va en la URL: api/servicios-rutas/{accountId}/rutas, etc.
    // Sin [Authorize] por ahora (la app móvil usa otro login). Cuando se active, hay que validar que
    // la cuenta del token coincida con {accountId}; si no, cualquiera podría pedir los datos de otra cuenta.
    [Route("api/servicios-rutas/{accountId}")]
    [ApiController]
    public class ServiciosRutasController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;
        private readonly IUnitOfWork _uow;
        private readonly IHubContext<ServiciosRutasHub> _hub;
        private readonly PosicionesEnVivoStore _posiciones;

        public ServiciosRutasController(
            IReadOnlyUnitOfWork readOnlyUow,
            IUnitOfWork uow,
            IHubContext<ServiciosRutasHub> hub,
            PosicionesEnVivoStore posiciones)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
            _hub = hub;
            _posiciones = posiciones;
        }

        // Cuenta tomada de la URL ({accountId}). Se declara como propiedad enlazada a la ruta para que
        // todas las acciones la usen sin repetir el parámetro (Swagger la muestra en cada endpoint).
        [FromRoute(Name = "accountId")]
        public string AccountId { get; set; } = string.Empty;

        private const int MaxDiasReporte = 31;

        private static readonly string[] EstadosServicio = { "PROGRAMADO", "EN_CURSO", "FINALIZADO", "CANCELADO" };

        // Transiciones permitidas: estado actual -> estados a los que puede pasar.
        private static readonly Dictionary<string, string[]> Transiciones = new()
        {
            ["PROGRAMADO"] = new[] { "EN_CURSO", "CANCELADO" },
            ["EN_CURSO"] = new[] { "FINALIZADO", "CANCELADO" }
        };

        // =====================================================================
        // RUTAS
        // =====================================================================

        [HttpGet("rutas")]
        public async Task<ActionResult<IEnumerable<RutaServicio>>> GetRutas([FromQuery] bool incluirInactivas = false)
        {
            try
            {
                var result = await _readOnlyUow.ServiciosRutasRepository.GetRutas(AccountId!, incluirInactivas);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las rutas", error = ex.Message });
            }
        }

        // Devuelve la ruta con sus paraderos ordenados, con latitud, longitud, radio y minutos acumulados.
        // Es el endpoint pensado para la app móvil.
        [HttpGet("rutas/{id:long}")]
        public async Task<ActionResult<RutaServicio>> GetRuta(long id)
        {
            try
            {
                var result = await _readOnlyUow.ServiciosRutasRepository.GetRutaById(id, AccountId!);
                if (result == null)
                    return NotFound(new { message = "Ruta no encontrada" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la ruta", error = ex.Message });
            }
        }

        [HttpPost("rutas")]
        public async Task<ActionResult> CrearRuta([FromBody] RutaRequest request)
        {
            try
            {
                request.AccountID = AccountId;
                var error = ValidarRuta(request);
                if (error != null)
                    return BadRequest(new { message = error });

                if (await _uow.ServiciosRutasRepository.ExisteCodigoRuta(AccountId!, request.Codigo!, null))
                    return Conflict(new { message = $"Ya existe una ruta con el código '{request.Codigo}'" });

                var id = await _uow.ServiciosRutasRepository.InsertRuta(request);
                _uow.SaveChanges();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear la ruta", error = ex.Message });
            }
        }

        // Reemplaza datos y lista de paraderos. Los paraderos con Id se actualizan, los sin Id se crean y
        // los que no vengan en la lista se desactivan (conservan su historial).
        [HttpPut("rutas/{id:long}")]
        public async Task<ActionResult> ActualizarRuta(long id, [FromBody] RutaRequest request)
        {
            try
            {
                request.AccountID = AccountId;
                var error = ValidarRuta(request);
                if (error != null)
                    return BadRequest(new { message = error });

                var existente = await _uow.ServiciosRutasRepository.GetRutaById(id, AccountId!);
                if (existente == null)
                    return NotFound(new { message = "Ruta no encontrada" });

                if (await _uow.ServiciosRutasRepository.ExisteCodigoRuta(AccountId!, request.Codigo!, id))
                    return Conflict(new { message = $"Ya existe otra ruta con el código '{request.Codigo}'" });

                var idsRuta = existente.Paraderos!.Select(p => p.Id).ToHashSet();
                var ajenos = request.Paraderos
                    .Where(p => p.Id.HasValue && p.Id.Value > 0 && !idsRuta.Contains(p.Id.Value))
                    .Select(p => p.Id!.Value)
                    .ToList();
                if (ajenos.Count > 0)
                    return BadRequest(new { message = $"Los paraderos {string.Join(", ", ajenos)} no pertenecen a esta ruta" });

                var repetidos = request.Paraderos
                    .Where(p => p.Id.HasValue && p.Id.Value > 0)
                    .GroupBy(p => p.Id!.Value)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if (repetidos.Count > 0)
                    return BadRequest(new { message = $"El paradero {repetidos[0]} está repetido en la lista" });

                await _uow.ServiciosRutasRepository.UpdateRuta(id, request);
                _uow.SaveChanges();
                return Ok(new { message = "Ruta actualizada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la ruta", error = ex.Message });
            }
        }

        // No hay DELETE: las rutas con servicios no se pueden borrar. Se activan / desactivan.
        [HttpPatch("rutas/{id:long}/activo")]
        public async Task<ActionResult> SetRutaActivo(long id, [FromBody] ActivoRequest request)
        {
            try
            {
                var existente = await _uow.ServiciosRutasRepository.GetRutaById(id, AccountId!);
                if (existente == null)
                    return NotFound(new { message = "Ruta no encontrada" });

                await _uow.ServiciosRutasRepository.SetRutaActivo(id, AccountId!, request.Activo);
                _uow.SaveChanges();
                return Ok(new { id, activo = request.Activo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cambiar el estado de la ruta", error = ex.Message });
            }
        }

        // =====================================================================
        // SERVICIOS
        // =====================================================================

        // desde / hasta en formato dd/MM/yyyy. estado: PROGRAMADO | EN_CURSO | FINALIZADO | CANCELADO.
        [HttpGet("servicios")]
        public async Task<ActionResult<IEnumerable<ServicioRuta>>> GetServicios(
            [FromQuery] string? desde, [FromQuery] string? hasta,
            [FromQuery] string? estado, [FromQuery] string? deviceID, [FromQuery] string? conductorBrevete)
        {
            try
            {
                if (!TryParseFecha(desde, out var fechaDesde))
                    return BadRequest(new { message = "El parámetro 'desde' debe tener formato dd/MM/yyyy" });
                if (!TryParseFecha(hasta, out var fechaHasta))
                    return BadRequest(new { message = "El parámetro 'hasta' debe tener formato dd/MM/yyyy" });

                string? estadoNormalizado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpperInvariant();
                if (estadoNormalizado != null && !EstadosServicio.Contains(estadoNormalizado))
                    return BadRequest(new { message = $"Estado inválido. Valores: {string.Join(", ", EstadosServicio)}" });

                var result = await _readOnlyUow.ServiciosRutasRepository.GetServicios(
                    AccountId!, fechaDesde, fechaHasta, estadoNormalizado,
                    string.IsNullOrWhiteSpace(deviceID) ? null : deviceID.Trim(),
                    string.IsNullOrWhiteSpace(conductorBrevete) ? null : conductorBrevete.Trim());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los servicios", error = ex.Message });
            }
        }

        // Reporte de cumplimiento por servicio: cada servicio con sus paraderos (hora esperada, llegada y
        // salida reales, estancia, diferencia y estado) y los totales del rango, todo en una llamada.
        // desde / hasta (dd/MM/yyyy) obligatorios, máximo 31 días. Filtros opcionales: estado, deviceID, conductorBrevete.
        [HttpGet("reportes/servicios")]
        public async Task<ActionResult<ReporteServiciosResponse>> GetReporteServicios(
            [FromQuery] string? desde, [FromQuery] string? hasta,
            [FromQuery] string? estado, [FromQuery] string? deviceID, [FromQuery] string? conductorBrevete)
        {
            try
            {
                if (!TryParseFecha(desde, out var fechaDesde) || fechaDesde == null)
                    return BadRequest(new { message = "El parámetro 'desde' es obligatorio y debe tener formato dd/MM/yyyy" });
                if (!TryParseFecha(hasta, out var fechaHasta) || fechaHasta == null)
                    return BadRequest(new { message = "El parámetro 'hasta' es obligatorio y debe tener formato dd/MM/yyyy" });
                if (fechaHasta < fechaDesde)
                    return BadRequest(new { message = "'hasta' no puede ser anterior a 'desde'" });
                if ((fechaHasta.Value - fechaDesde.Value).TotalDays > MaxDiasReporte - 1)
                    return BadRequest(new { message = $"El rango no puede superar {MaxDiasReporte} días" });

                string? estadoNormalizado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpperInvariant();
                if (estadoNormalizado != null && !EstadosServicio.Contains(estadoNormalizado))
                    return BadRequest(new { message = $"Estado inválido. Valores: {string.Join(", ", EstadosServicio)}" });

                var servicios = await _readOnlyUow.ServiciosRutasRepository.GetReporteServicios(
                    AccountId!, fechaDesde, fechaHasta, estadoNormalizado,
                    string.IsNullOrWhiteSpace(deviceID) ? null : deviceID.Trim(),
                    string.IsNullOrWhiteSpace(conductorBrevete) ? null : conductorBrevete.Trim());

                var resumen = ReporteServiciosCalculos.Calcular(servicios);
                int tolerancia = await _readOnlyUow.ServiciosRutasRepository.GetTolerancia(AccountId!);

                return Ok(new ReporteServiciosResponse
                {
                    ToleranciaMin = tolerancia,
                    Resumen = resumen,
                    Servicios = servicios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar el reporte de servicios", error = ex.Message });
            }
        }

        [HttpGet("servicios/{id:long}")]
        public async Task<ActionResult<ServicioRuta>> GetServicio(long id)
        {
            try
            {
                var result = await _readOnlyUow.ServiciosRutasRepository.GetServicioById(id, AccountId!);
                if (result == null)
                    return NotFound(new { message = "Servicio no encontrado" });

                // La ruta completa (trazado y paraderos con coordenadas y radio) viaja dentro del servicio:
                // la app del conductor entra a un servicio y ya tiene todo para navegar y marcar geocercas.
                result.Ruta = await _readOnlyUow.ServiciosRutasRepository.GetRutaById(result.RutaId, AccountId!);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el servicio", error = ex.Message });
            }
        }

        [HttpPost("servicios")]
        public async Task<ActionResult> CrearServicio([FromBody] ServicioRuta servicio)
        {
            try
            {
                servicio.AccountID = AccountId!;
                var error = await ValidarServicio(servicio);
                if (error != null)
                    return error;

                var id = await _uow.ServiciosRutasRepository.InsertServicio(servicio);
                _uow.SaveChanges();
                await Notificar(new AvisoServicio { ServicioId = id, Tipo = "CREADO", DeviceID = servicio.DeviceID });
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el servicio", error = ex.Message });
            }
        }

        // Solo se pueden editar servicios PROGRAMADO. Regenera las horas esperadas por paradero.
        [HttpPut("servicios/{id:long}")]
        public async Task<ActionResult> ActualizarServicio(long id, [FromBody] ServicioRuta servicio)
        {
            try
            {
                var existente = await _uow.ServiciosRutasRepository.GetServicioById(id, AccountId!);
                if (existente == null)
                    return NotFound(new { message = "Servicio no encontrado" });

                if (existente.Estado != "PROGRAMADO")
                    return Conflict(new { message = $"Solo se pueden editar servicios PROGRAMADO (estado actual: {existente.Estado})" });

                servicio.Id = id;
                servicio.AccountID = AccountId!;
                var error = await ValidarServicio(servicio);
                if (error != null)
                    return error;

                await _uow.ServiciosRutasRepository.UpdateServicio(servicio);
                _uow.SaveChanges();
                await Notificar(new AvisoServicio { ServicioId = id, Tipo = "EDITADO", DeviceID = servicio.DeviceID });
                return Ok(new { message = "Servicio actualizado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el servicio", error = ex.Message });
            }
        }

        // Reprograma un servicio PROGRAMADO o EN_CURSO: nueva fecha/hora de salida. Las horas esperadas
        // de los paraderos sin llegada se corren la misma diferencia; las llegadas ya marcadas se conservan.
        [HttpPatch("servicios/{id:long}/reprogramar")]
        public async Task<ActionResult> Reprogramar(long id, [FromBody] ReprogramarServicioRequest request)
        {
            try
            {
                if (!request.FechaSalida.HasValue)
                    return BadRequest(new { message = "La fecha de salida es obligatoria (dd/MM/yyyy)" });
                if (!request.HoraSalida.HasValue)
                    return BadRequest(new { message = "La hora de salida es obligatoria (HH:mm)" });
                if (request.HoraSalida.Value < TimeSpan.Zero || request.HoraSalida.Value >= TimeSpan.FromDays(1))
                    return BadRequest(new { message = "La hora de salida debe estar entre 00:00 y 23:59" });

                string? motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim();
                if (motivo != null && motivo.Length > 200)
                    return BadRequest(new { message = "El motivo no puede superar 200 caracteres" });

                var existente = await _uow.ServiciosRutasRepository.GetServicioById(id, AccountId!);
                if (existente == null)
                    return NotFound(new { message = "Servicio no encontrado" });

                if (existente.Estado != "PROGRAMADO" && existente.Estado != "EN_CURSO")
                    return Conflict(new { message = $"Solo se pueden reprogramar servicios PROGRAMADO o EN_CURSO (estado actual: {existente.Estado})" });

                DateTime actual = existente.FechaSalida!.Value.Date + existente.HoraSalida!.Value;
                DateTime nueva = request.FechaSalida.Value.Date + request.HoraSalida.Value;
                int desplazamiento = (int)Math.Round((nueva - actual).TotalMinutes);
                if (desplazamiento == 0)
                    return BadRequest(new { message = "La nueva fecha y hora es igual a la actual" });

                int filas = await _uow.ServiciosRutasRepository.ReprogramarServicio(
                    id, AccountId!, existente.Estado, nueva, desplazamiento, motivo);
                if (filas == 0)
                    return Conflict(new { message = "El estado del servicio cambió mientras se procesaba la solicitud" });

                _uow.SaveChanges();
                await Notificar(new AvisoServicio { ServicioId = id, Tipo = "REPROGRAMADO", DeviceID = existente.DeviceID });
                return Ok(new { id, desplazamientoMin = desplazamiento, message = "Servicio reprogramado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al reprogramar el servicio", error = ex.Message });
            }
        }

        // PROGRAMADO -> EN_CURSO | CANCELADO ; EN_CURSO -> FINALIZADO | CANCELADO.
        // inicio_real / fin_real se registran en hora de Perú (igual que fecha/hora de salida y
        // hora_esperada), para que las diferencias en minutos se calculen en la misma zona.
        [HttpPatch("servicios/{id:long}/estado")]
        public async Task<ActionResult> CambiarEstado(long id, [FromBody] CambiarEstadoServicioRequest request)
        {
            try
            {
                string nuevo = (request.Estado ?? string.Empty).Trim().ToUpperInvariant();
                if (!EstadosServicio.Contains(nuevo))
                    return BadRequest(new { message = $"Estado inválido. Valores: {string.Join(", ", EstadosServicio)}" });

                if (request.DistanciaRealKm.HasValue && request.DistanciaRealKm.Value < 0)
                    return BadRequest(new { message = "La distancia real no puede ser negativa" });

                var existente = await _uow.ServiciosRutasRepository.GetServicioById(id, AccountId!);
                if (existente == null)
                    return NotFound(new { message = "Servicio no encontrado" });

                if (!Transiciones.TryGetValue(existente.Estado, out var permitidos) || !permitidos.Contains(nuevo))
                    return Conflict(new { message = $"No se puede pasar de {existente.Estado} a {nuevo}" });

                int filas = await _uow.ServiciosRutasRepository.CambiarEstadoServicio(
                    id, AccountId!, existente.Estado, nuevo, AhoraPeru(),
                    request.DistanciaRealKm, request.Observaciones);
                if (filas == 0)
                    return Conflict(new { message = "El estado del servicio cambió mientras se procesaba la solicitud" });

                _uow.SaveChanges();
                await Notificar(new AvisoServicio
                {
                    ServicioId = id, Tipo = "ESTADO", DeviceID = existente.DeviceID, EstadoServicio = nuevo
                });
                return Ok(new { id, estado = nuevo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cambiar el estado del servicio", error = ex.Message });
            }
        }

        // =====================================================================
        // SEGUIMIENTO POR PARADERO
        // =====================================================================

        // Registra la llegada de la unidad a un paradero (orden = posición en la ruta, 1 = origen).
        // El estado sale de comparar con la hora esperada y la tolerancia de la cuenta:
        // |diferencia| <= tolerancia => A_TIEMPO ; antes => ADELANTADO ; después => RETRASADO.
        // Los paraderos anteriores sin llegada pasan a OMITIDO.
        [HttpPatch("servicios/{id:long}/paraderos/{orden:int}/llegada")]
        public async Task<ActionResult> RegistrarLlegada(long id, int orden, [FromBody] RegistrarMarcaRequest? request)
        {
            try
            {
                if (!TryParseMarca(request?.Hora, out var hora))
                    return BadRequest(new { message = "La hora debe tener formato dd/MM/yyyy HH:mm" });

                var servicio = await _uow.ServiciosRutasRepository.GetServicioById(id, AccountId!);
                if (servicio == null)
                    return NotFound(new { message = "Servicio no encontrado" });

                if (servicio.Estado != "EN_CURSO")
                    return Conflict(new { message = $"El servicio debe estar EN_CURSO para registrar llegadas (estado actual: {servicio.Estado})" });

                var paradero = servicio.Paraderos!.FirstOrDefault(p => p.Orden == orden);
                if (paradero == null)
                    return NotFound(new { message = $"El servicio no tiene un paradero con orden {orden}" });

                if (paradero.HoraLlegadaReal.HasValue)
                    return Conflict(new { message = "La llegada a este paradero ya fue registrada" });

                DateTime horaLlegada = hora ?? AhoraPeru();
                int tolerancia = await _uow.ServiciosRutasRepository.GetTolerancia(AccountId!);

                // Misma regla de truncado que TIMESTAMPDIFF(MINUTE) de la columna diferencia_min.
                int diferencia = (int)Math.Truncate((horaLlegada - paradero.HoraEsperada).TotalMinutes);
                string estado = Math.Abs(diferencia) <= tolerancia
                    ? "A_TIEMPO"
                    : (diferencia < 0 ? "ADELANTADO" : "RETRASADO");

                int filas = await _uow.ServiciosRutasRepository.RegistrarLlegada(id, orden, horaLlegada, estado);
                if (filas == 0)
                    return Conflict(new { message = "La llegada a este paradero ya fue registrada" });

                await _uow.ServiciosRutasRepository.MarcarOmitidosAnteriores(id, orden);
                _uow.SaveChanges();
                await Notificar(new AvisoServicio
                {
                    ServicioId = id, Tipo = "LLEGADA", DeviceID = servicio.DeviceID,
                    Orden = orden, Paradero = paradero.Nombre, Hora = horaLlegada.ToString("HH:mm:ss"),
                    EstadoParadero = estado, DiferenciaMin = diferencia
                });
                return Ok(new { id, orden, estado, diferenciaMin = diferencia, toleranciaMin = tolerancia });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al registrar la llegada", error = ex.Message });
            }
        }

        // Registra la salida del paradero. Requiere que la llegada ya esté registrada.
        [HttpPatch("servicios/{id:long}/paraderos/{orden:int}/salida")]
        public async Task<ActionResult> RegistrarSalida(long id, int orden, [FromBody] RegistrarMarcaRequest? request)
        {
            try
            {
                if (!TryParseMarca(request?.Hora, out var hora))
                    return BadRequest(new { message = "La hora debe tener formato dd/MM/yyyy HH:mm" });

                var servicio = await _uow.ServiciosRutasRepository.GetServicioById(id, AccountId!);
                if (servicio == null)
                    return NotFound(new { message = "Servicio no encontrado" });

                if (servicio.Estado != "EN_CURSO")
                    return Conflict(new { message = $"El servicio debe estar EN_CURSO para registrar salidas (estado actual: {servicio.Estado})" });

                var paradero = servicio.Paraderos!.FirstOrDefault(p => p.Orden == orden);
                if (paradero == null)
                    return NotFound(new { message = $"El servicio no tiene un paradero con orden {orden}" });

                if (!paradero.HoraLlegadaReal.HasValue)
                    return Conflict(new { message = "Primero debe registrarse la llegada a este paradero" });

                if (paradero.HoraSalidaReal.HasValue)
                    return Conflict(new { message = "La salida de este paradero ya fue registrada" });

                DateTime horaSalida = hora ?? AhoraPeru();
                int filas = await _uow.ServiciosRutasRepository.RegistrarSalida(id, orden, horaSalida);
                if (filas == 0)
                    return Conflict(new { message = "La salida de este paradero ya fue registrada" });

                _uow.SaveChanges();
                await Notificar(new AvisoServicio
                {
                    ServicioId = id, Tipo = "SALIDA", DeviceID = servicio.DeviceID,
                    Orden = orden, Paradero = paradero.Nombre, Hora = horaSalida.ToString("HH:mm:ss"),
                    PermanenciaMin = (int)Math.Max(0, Math.Truncate((horaSalida - paradero.HoraLlegadaReal!.Value).TotalMinutes))
                });
                return Ok(new { id, orden });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al registrar la salida", error = ex.Message });
            }
        }

        // =====================================================================
        // CONFIGURACIÓN
        // =====================================================================

        // Tolerancia (±minutos) para considerar un paradero "a tiempo". 5 si la cuenta no la configuró.
        [HttpGet("configuracion")]
        public async Task<ActionResult<ConfigServiciosRutas>> GetConfiguracion()
        {
            try
            {
                int tolerancia = await _readOnlyUow.ServiciosRutasRepository.GetTolerancia(AccountId!);
                return Ok(new ConfigServiciosRutas { AccountID = AccountId!, ToleranciaMin = tolerancia });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la configuración", error = ex.Message });
            }
        }

        [HttpPut("configuracion")]
        public async Task<ActionResult> ActualizarConfiguracion([FromBody] ToleranciaRequest request)
        {
            try
            {
                if (request.ToleranciaMin < 0 || request.ToleranciaMin > 240)
                    return BadRequest(new { message = "La tolerancia debe estar entre 0 y 240 minutos" });

                await _uow.ServiciosRutasRepository.SetTolerancia(AccountId!, request.ToleranciaMin);
                _uow.SaveChanges();
                return Ok(new { toleranciaMin = request.ToleranciaMin });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar la configuración", error = ex.Message });
            }
        }

        // Avisa a la web (evento "ServicioActualizado") que algo cambió en un servicio: llegada/salida de un
        // paradero, cambio de estado, etc. Es solo una notificación: lo importante ya quedó guardado en la
        // base. Si falla el aviso, no se hace fallar la operación (la web igual se actualiza en su refresco).
        private async Task Notificar(AvisoServicio aviso)
        {
            try
            {
                await _hub.Clients.Group(AccountId).SendAsync("ServicioActualizado", aviso);
            }
            catch
            {
                // Sin conexión SignalR no pasa nada: el refresco periódico de la web lo cubre.
            }
        }

        // =====================================================================
        // PLANTILLAS (servicios automáticos)
        // El backend crea cada día los servicios de las plantillas completas
        // (ver ServiciosRutasAutomaticosService). Aquí se administran.
        // =====================================================================

        [HttpGet("plantillas")]
        public async Task<ActionResult<IEnumerable<ServicioPlantilla>>> GetPlantillas()
        {
            try
            {
                var result = await _readOnlyUow.ServiciosRutasRepository.GetPlantillas(AccountId!, soloActivas: false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las plantillas", error = ex.Message });
            }
        }

        [HttpPost("plantillas")]
        public async Task<ActionResult> CrearPlantilla([FromBody] ServicioPlantilla plantilla)
        {
            try
            {
                plantilla.AccountID = AccountId!;
                var error = await ValidarPlantilla(plantilla);
                if (error != null)
                    return error;

                var id = await _uow.ServiciosRutasRepository.InsertPlantilla(plantilla);
                _uow.SaveChanges();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear la plantilla", error = ex.Message });
            }
        }

        [HttpPut("plantillas/{id:long}")]
        public async Task<ActionResult> ActualizarPlantilla(long id, [FromBody] ServicioPlantilla plantilla)
        {
            try
            {
                var existente = await _uow.ServiciosRutasRepository.GetPlantillaById(id, AccountId!);
                if (existente == null)
                    return NotFound(new { message = "Plantilla no encontrada" });

                plantilla.Id = id;
                plantilla.AccountID = AccountId!;
                var error = await ValidarPlantilla(plantilla);
                if (error != null)
                    return error;

                await _uow.ServiciosRutasRepository.UpdatePlantilla(plantilla);
                _uow.SaveChanges();
                return Ok(new { message = "Plantilla actualizada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la plantilla", error = ex.Message });
            }
        }

        // No toca los servicios ya generados: esos se administran como cualquier servicio.
        [HttpDelete("plantillas/{id:long}")]
        public async Task<ActionResult> EliminarPlantilla(long id)
        {
            try
            {
                int filas = await _uow.ServiciosRutasRepository.DeletePlantilla(id, AccountId!);
                if (filas == 0)
                    return NotFound(new { message = "Plantilla no encontrada" });

                _uow.SaveChanges();
                return Ok(new { message = "Plantilla eliminada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar la plantilla", error = ex.Message });
            }
        }

        // Hora (Perú) desde la cual se crean los servicios del día siguiente.
        [HttpGet("plantillas/configuracion")]
        public async Task<ActionResult> GetConfigPlantillas()
        {
            try
            {
                var hora = await _readOnlyUow.ServiciosRutasRepository.GetHoraGeneracion(AccountId!);
                return Ok(new { horaGeneracion = hora.ToString(@"hh\:mm") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la configuración", error = ex.Message });
            }
        }

        [HttpPut("plantillas/configuracion")]
        public async Task<ActionResult> SetConfigPlantillas([FromBody] ConfigPlantillasRequest request)
        {
            try
            {
                if (!TimeSpan.TryParseExact(request.HoraGeneracion?.Trim(), new[] { @"hh\:mm", @"hh\:mm\:ss" },
                        CultureInfo.InvariantCulture, out var hora) || hora < TimeSpan.Zero || hora >= TimeSpan.FromDays(1))
                    return BadRequest(new { message = "La hora de creación debe tener formato HH:mm (00:00 a 23:59)" });

                await _uow.ServiciosRutasRepository.SetHoraGeneracion(AccountId!, hora);
                _uow.SaveChanges();
                return Ok(new { horaGeneracion = hora.ToString(@"hh\:mm") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar la configuración", error = ex.Message });
            }
        }

        // La plantilla puede guardarse incompleta (hora, unidad o conductor vacíos): simplemente no genera
        // servicios hasta completarse. Lo que sí debe ser válido es lo que venga informado.
        private async Task<ActionResult?> ValidarPlantilla(ServicioPlantilla p)
        {
            if (p.RutaId <= 0)
                return BadRequest(new { message = "La ruta es obligatoria" });

            p.Dias = (p.Dias ?? new List<int>()).Where(d => d >= 1 && d <= 7).Distinct().OrderBy(d => d).ToList();
            if (p.Dias.Count == 0)
                return BadRequest(new { message = "Selecciona al menos un día de la semana" });

            if (p.HoraSalida.HasValue && (p.HoraSalida.Value < TimeSpan.Zero || p.HoraSalida.Value >= TimeSpan.FromDays(1)))
                return BadRequest(new { message = "La hora de salida debe estar entre 00:00 y 23:59" });

            static string? Limpio(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
            p.DeviceID = Limpio(p.DeviceID);
            p.ConductorBrevete = Limpio(p.ConductorBrevete);

            if (p.DeviceID != null &&
                !await _uow.ConfiguracionRepository.DispositivoPerteneceACuentaAsync(p.AccountID, p.DeviceID))
                return BadRequest(new { message = "La unidad no existe o no pertenece a tu cuenta" });

            int paraderos = await _uow.ServiciosRutasRepository.ContarParaderosDeRutaActiva(p.RutaId, p.AccountID);
            if (paraderos < 2)
                return BadRequest(new { message = "La ruta no existe, está inactiva o no tiene paraderos suficientes" });

            return null;
        }

        // =====================================================================
        // NOTIFICACIONES DEL CONDUCTOR (sugerencias, alertas, incidencias)
        // El conductor las envía desde la app; llegan a la web en tiempo real por el hub (evento
        // "NotificacionRecibida") y quedan guardadas. Son solo informativas: no cambian rutas ni paraderos.
        // =====================================================================

        private static readonly string[] CategoriasNotificacion = { "SUGERENCIA", "ALERTA", "INCIDENCIA", "INFO" };
        private static readonly string[] EstadosNotificacion = { "PENDIENTE", "ACEPTADA", "RECHAZADA" };

        // Mensajes predefinidos. La app del conductor usa soloActivos=true (valor por defecto);
        // la web de administración pide soloActivos=false para verlos todos.
        [HttpGet("notificaciones/tipos")]
        public async Task<ActionResult<IEnumerable<NotificacionTipo>>> GetNotificacionTipos([FromQuery] bool soloActivos = true)
        {
            try
            {
                var result = await _readOnlyUow.ServiciosRutasRepository.GetNotificacionTipos(AccountId!, soloActivos);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los tipos de notificación", error = ex.Message });
            }
        }

        [HttpPost("notificaciones/tipos")]
        public async Task<ActionResult> CrearNotificacionTipo([FromBody] NotificacionTipo tipo)
        {
            try
            {
                tipo.AccountID = AccountId!;
                tipo.Id = 0;
                var error = await ValidarNotificacionTipo(tipo);
                if (error != null)
                    return error;

                var id = await _uow.ServiciosRutasRepository.InsertNotificacionTipo(tipo);
                await _uow.ServiciosRutasRepository.ReordenarNotificacionTipos(AccountId!, id, tipo.Orden);
                _uow.SaveChanges();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el tipo de notificación", error = ex.Message });
            }
        }

        [HttpPut("notificaciones/tipos/{id:long}")]
        public async Task<ActionResult> ActualizarNotificacionTipo(long id, [FromBody] NotificacionTipo tipo)
        {
            try
            {
                if (await _uow.ServiciosRutasRepository.GetNotificacionTipoById(id, AccountId!) == null)
                    return NotFound(new { message = "Tipo de notificación no encontrado" });

                tipo.Id = id;
                tipo.AccountID = AccountId!;
                var error = await ValidarNotificacionTipo(tipo);
                if (error != null)
                    return error;

                await _uow.ServiciosRutasRepository.UpdateNotificacionTipo(tipo);
                await _uow.ServiciosRutasRepository.ReordenarNotificacionTipos(AccountId!, id, tipo.Orden);
                _uow.SaveChanges();
                return Ok(new { message = "Tipo de notificación actualizado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el tipo de notificación", error = ex.Message });
            }
        }

        // El historial conserva su propia copia del texto, así que borrar un tipo no afecta lo ya enviado.
        [HttpDelete("notificaciones/tipos/{id:long}")]
        public async Task<ActionResult> EliminarNotificacionTipo(long id)
        {
            try
            {
                int filas = await _uow.ServiciosRutasRepository.DeleteNotificacionTipo(id, AccountId!);
                if (filas == 0)
                    return NotFound(new { message = "Tipo de notificación no encontrado" });

                _uow.SaveChanges();
                return Ok(new { message = "Tipo de notificación eliminado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar el tipo de notificación", error = ex.Message });
            }
        }

        // La app del conductor envía la notificación. Se guarda y se avisa a la web por el hub.
        [HttpPost("notificaciones")]
        public async Task<ActionResult> EnviarNotificacion([FromBody] EnviarNotificacionRequest request)
        {
            try
            {
                if (request.Latitud < -90 || request.Latitud > 90 || request.Longitud < -180 || request.Longitud > 180
                    || (request.Latitud == 0 && request.Longitud == 0))
                    return BadRequest(new { message = "Las coordenadas (latitud y longitud) son obligatorias y deben ser válidas" });

                if (!TryParseMarca(request.FechaEvento, out var fechaEvento))
                    return BadRequest(new { message = "FechaEvento debe tener formato dd/MM/yyyy HH:mm:ss" });

                var tipo = await _uow.ServiciosRutasRepository.GetNotificacionTipoById(request.TipoId, AccountId!);
                if (tipo == null || !tipo.Activo)
                    return BadRequest(new { message = "El tipo de notificación no existe o está inactivo" });

                string? comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim();
                if (comentario != null && comentario.Length > 500)
                    return BadRequest(new { message = "El comentario no puede superar los 500 caracteres" });
                if (tipo.RequiereComentario && comentario == null)
                    return BadRequest(new { message = "Este tipo de notificación requiere un comentario" });

                var n = new Notificacion
                {
                    AccountID = AccountId!,
                    TipoId = tipo.Id,
                    TipoCodigo = tipo.Codigo,
                    Categoria = tipo.Categoria,
                    Titulo = tipo.Titulo,
                    Comentario = comentario,
                    Latitud = request.Latitud,
                    Longitud = request.Longitud,
                    FechaEvento = fechaEvento,
                    CreatedAt = AhoraPeru()
                };

                // Unidad, conductor y ruta salen del servicio; sin servicio, de lo que mande la app.
                if (request.ServicioId.HasValue)
                {
                    var servicio = await _uow.ServiciosRutasRepository.GetServicioById(request.ServicioId.Value, AccountId!);
                    if (servicio == null)
                        return BadRequest(new { message = "El servicio no existe o no pertenece a tu cuenta" });

                    n.ServicioId = servicio.Id;
                    n.RutaId = servicio.RutaId;
                    n.RutaNombre = servicio.RutaNombre;
                    n.DeviceID = servicio.DeviceID;
                    n.ConductorBrevete = servicio.ConductorBrevete;
                    n.ConductorNombre = servicio.ConductorNombre;
                }
                else
                {
                    n.DeviceID = string.IsNullOrWhiteSpace(request.DeviceID) ? null : request.DeviceID.Trim();
                    n.ConductorBrevete = string.IsNullOrWhiteSpace(request.ConductorBrevete) ? null : request.ConductorBrevete.Trim();
                    n.ConductorNombre = string.IsNullOrWhiteSpace(request.ConductorNombre) ? null : request.ConductorNombre.Trim();
                    if (request.RutaId.HasValue)
                    {
                        n.RutaNombre = await _uow.ServiciosRutasRepository.GetNombreRuta(request.RutaId.Value, AccountId!);
                        if (n.RutaNombre == null)
                            return BadRequest(new { message = "La ruta no existe o no pertenece a tu cuenta" });
                        n.RutaId = request.RutaId;
                    }
                }

                // Paradero señalado: debe pertenecer a la ruta.
                if (request.RutaParaderoId.HasValue)
                {
                    if (!n.RutaId.HasValue)
                        return BadRequest(new { message = "Para señalar un paradero envía el servicio o la ruta" });

                    n.ParaderoNombre = await _uow.ServiciosRutasRepository.GetNombreParadero(request.RutaParaderoId.Value, n.RutaId.Value);
                    if (n.ParaderoNombre == null)
                        return BadRequest(new { message = "El paradero no pertenece a la ruta" });
                    n.RutaParaderoId = request.RutaParaderoId;
                }
                else if (tipo.RequiereParadero)
                {
                    return BadRequest(new { message = "Este tipo de notificación requiere indicar el paradero" });
                }

                n.Id = await _uow.ServiciosRutasRepository.InsertNotificacion(n);
                _uow.SaveChanges();

                // Aviso en tiempo real a la web. Si falla, la notificación ya quedó guardada.
                try
                {
                    await _hub.Clients.Group(AccountId).SendAsync("NotificacionRecibida", n);
                }
                catch
                {
                    // La web igual la verá al consultar.
                }

                return Ok(new { id = n.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al enviar la notificación", error = ex.Message });
            }
        }

        // Historial. desde / hasta (dd/MM/yyyy) por fecha de llegada, máximo 500 filas (más recientes primero).
        [HttpGet("notificaciones")]
        public async Task<ActionResult<IEnumerable<Notificacion>>> GetNotificaciones(
            [FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? estado,
            [FromQuery] string? tipo, [FromQuery] bool soloNoLeidas = false, [FromQuery] int limite = 200)
        {
            try
            {
                if (!TryParseFecha(desde, out var fechaDesde))
                    return BadRequest(new { message = "'desde' debe tener formato dd/MM/yyyy" });
                if (!TryParseFecha(hasta, out var fechaHasta))
                    return BadRequest(new { message = "'hasta' debe tener formato dd/MM/yyyy" });
                if (fechaDesde.HasValue && fechaHasta.HasValue && fechaHasta < fechaDesde)
                    return BadRequest(new { message = "'hasta' no puede ser anterior a 'desde'" });

                string? estadoNormalizado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpperInvariant();
                if (estadoNormalizado != null && !EstadosNotificacion.Contains(estadoNormalizado))
                    return BadRequest(new { message = $"Estado inválido. Valores: {string.Join(", ", EstadosNotificacion)}" });

                var result = await _readOnlyUow.ServiciosRutasRepository.GetNotificaciones(
                    AccountId!, fechaDesde, fechaHasta, estadoNormalizado,
                    string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim(), soloNoLeidas, Math.Clamp(limite, 1, 500));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las notificaciones", error = ex.Message });
            }
        }

        [HttpGet("notificaciones/{id:long}")]
        public async Task<ActionResult<Notificacion>> GetNotificacion(long id)
        {
            try
            {
                var result = await _readOnlyUow.ServiciosRutasRepository.GetNotificacionById(id, AccountId!);
                return result == null ? NotFound(new { message = "Notificación no encontrada" }) : Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la notificación", error = ex.Message });
            }
        }

        // Para el contador de la campana.
        [HttpGet("notificaciones/no-leidas/cantidad")]
        public async Task<ActionResult> GetCantidadNoLeidas()
        {
            try
            {
                int cantidad = await _readOnlyUow.ServiciosRutasRepository.ContarNotificacionesNoLeidas(AccountId!);
                return Ok(new { cantidad });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al contar las notificaciones", error = ex.Message });
            }
        }

        [HttpPatch("notificaciones/{id:long}/leida")]
        public async Task<ActionResult> MarcarLeida(long id)
        {
            try
            {
                int filas = await _uow.ServiciosRutasRepository.MarcarNotificacionLeida(id, AccountId!, AhoraPeru());
                if (filas == 0)
                    return NotFound(new { message = "Notificación no encontrada" });

                _uow.SaveChanges();
                return Ok(new { message = "Notificación marcada como leída" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al marcar la notificación", error = ex.Message });
            }
        }

        [HttpPatch("notificaciones/leidas")]
        public async Task<ActionResult> MarcarTodasLeidas()
        {
            try
            {
                int filas = await _uow.ServiciosRutasRepository.MarcarTodasNotificacionesLeidas(AccountId!, AhoraPeru());
                _uow.SaveChanges();
                return Ok(new { marcadas = filas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al marcar las notificaciones", error = ex.Message });
            }
        }

        // El operador acepta o rechaza (con una nota opcional). Es solo un registro: no cambia ninguna ruta.
        [HttpPatch("notificaciones/{id:long}/gestion")]
        public async Task<ActionResult> GestionarNotificacion(long id, [FromBody] GestionarNotificacionRequest request)
        {
            try
            {
                string estado = (request.Estado ?? string.Empty).Trim().ToUpperInvariant();
                if (!EstadosNotificacion.Contains(estado))
                    return BadRequest(new { message = $"Estado inválido. Valores: {string.Join(", ", EstadosNotificacion)}" });

                string? respuesta = string.IsNullOrWhiteSpace(request.Respuesta) ? null : request.Respuesta.Trim();
                if (respuesta != null && respuesta.Length > 500)
                    return BadRequest(new { message = "La respuesta no puede superar los 500 caracteres" });

                string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? AccountId! : request.Usuario.Trim();
                if (usuario.Length > 50) usuario = usuario[..50];

                int filas = await _uow.ServiciosRutasRepository.GestionarNotificacion(
                    id, AccountId!, estado, respuesta, usuario, AhoraPeru());
                if (filas == 0)
                    return NotFound(new { message = "Notificación no encontrada" });

                _uow.SaveChanges();
                return Ok(new { message = "Notificación actualizada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al gestionar la notificación", error = ex.Message });
            }
        }

        private async Task<ActionResult?> ValidarNotificacionTipo(NotificacionTipo t)
        {
            t.Codigo = (t.Codigo ?? string.Empty).Trim().ToUpperInvariant();
            t.Categoria = (t.Categoria ?? string.Empty).Trim().ToUpperInvariant();
            t.Titulo = (t.Titulo ?? string.Empty).Trim();
            t.Descripcion = string.IsNullOrWhiteSpace(t.Descripcion) ? null : t.Descripcion.Trim();

            if (t.Codigo.Length == 0 || t.Codigo.Length > 40)
                return BadRequest(new { message = "El código es obligatorio (máximo 40 caracteres)" });
            if (!System.Text.RegularExpressions.Regex.IsMatch(t.Codigo, "^[A-Z0-9_]+$"))
                return BadRequest(new { message = "El código solo puede tener letras, números y guion bajo" });
            if (!CategoriasNotificacion.Contains(t.Categoria))
                return BadRequest(new { message = $"Categoría inválida. Valores: {string.Join(", ", CategoriasNotificacion)}" });
            if (t.Titulo.Length == 0 || t.Titulo.Length > 120)
                return BadRequest(new { message = "El título es obligatorio (máximo 120 caracteres)" });
            if (t.Descripcion != null && t.Descripcion.Length > 255)
                return BadRequest(new { message = "La descripción no puede superar los 255 caracteres" });

            if (await _uow.ServiciosRutasRepository.ExisteCodigoNotificacionTipo(t.AccountID, t.Codigo, t.Id > 0 ? t.Id : null))
                return Conflict(new { message = "Ya existe un tipo de notificación con ese código" });

            return null;
        }

        // =====================================================================
        // POSICIONES EN VIVO (no se guardan en la base de datos)
        // =====================================================================

        // La app del conductor la llama cada pocos segundos. Se reenvía por SignalR al grupo de la cuenta
        // (evento "PosicionActualizada") y se recuerda la última posición de la unidad solo en memoria.
        [HttpPost("posiciones")]
        public async Task<ActionResult> EnviarPosicion([FromBody] EnviarPosicionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceID))
                return BadRequest(new { message = "El deviceID es obligatorio" });
            if (request.DeviceID.Trim().Length > 50)
                return BadRequest(new { message = "El deviceID no puede superar 50 caracteres" });
            if (request.Latitud < -90 || request.Latitud > 90 || request.Longitud < -180 || request.Longitud > 180)
                return BadRequest(new { message = "Coordenadas fuera de rango" });
            if (request.Latitud == 0 && request.Longitud == 0)
                return BadRequest(new { message = "Coordenadas inválidas (0,0)" });

            var deviceID = request.DeviceID.Trim();
            var posicion = new PosicionEnVivo
            {
                Clave = deviceID,
                DeviceID = deviceID,
                Conductor = string.IsNullOrWhiteSpace(request.Conductor) ? null : request.Conductor.Trim(),
                RutaId = request.RutaId,
                ServicioId = request.ServicioId,
                Latitud = request.Latitud,
                Longitud = request.Longitud,
                Rumbo = request.Rumbo.HasValue ? ((request.Rumbo.Value % 360) + 360) % 360 : 0,
                Velocidad = request.Velocidad,
                Fecha = DateTime.UtcNow
            };

            _posiciones.Guardar(AccountId, posicion);

            try
            {
                await _hub.Clients.Group(AccountId).SendAsync("PosicionActualizada", posicion);

                // Pasajeros que están mirando esta ruta (ver ServiciosRutasHub.UnirseARuta).
                if (posicion.RutaId.HasValue)
                    await _hub.Clients.Group(ServiciosRutasHub.GrupoRuta(AccountId, posicion.RutaId.Value))
                        .SendAsync("PosicionActualizada", posicion);
            }
            catch (Exception ex)
            {
                // Un fallo al notificar a la web no debe hacer fallar el envío del celular.
                return Ok(new { recibida = true, notificada = false, error = ex.Message });
            }

            return Ok(new { recibida = true, notificada = true });
        }

        // Última posición de cada unidad que reportó en los últimos 10 minutos (la web la usa una sola vez
        // al abrirse; luego recibe todo por SignalR). Viene de memoria, no de la base de datos.
        [HttpGet("posiciones")]
        public ActionResult<IEnumerable<PosicionEnVivo>> GetPosiciones([FromQuery] long? rutaId = null)
        {
            var vigentes = _posiciones.Vigentes(AccountId);
            if (rutaId.HasValue)
                vigentes = vigentes.Where(p => p.RutaId == rutaId.Value).ToList();
            return Ok(vigentes);
        }

        // =====================================================================
        // Validaciones
        // =====================================================================

        private static string? ValidarRuta(RutaRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.AccountID)) return "No se pudo determinar la cuenta";
            if (string.IsNullOrWhiteSpace(r.Codigo)) return "El código es obligatorio";
            if (string.IsNullOrWhiteSpace(r.Nombre)) return "El nombre es obligatorio";
            if (r.Codigo.Length > 30) return "El código no puede superar 30 caracteres";
            if (r.Nombre.Length > 150) return "El nombre no puede superar 150 caracteres";
            if (!string.IsNullOrWhiteSpace(r.Grupo) && r.Grupo.Trim().Length > 50)
                return "El grupo no puede superar 50 caracteres";
            if (r.DistanciaKm.HasValue && r.DistanciaKm.Value < 0) return "La distancia no puede ser negativa";
            if (r.Paraderos == null || r.Paraderos.Count < 2)
                return "La ruta necesita al menos 2 paraderos (origen y destino)";

            if (r.Trazado != null)
            {
                if (r.Trazado.Count > 20000) return "El trazado no puede superar 20000 puntos";
                for (int i = 0; i < r.Trazado.Count; i++)
                {
                    var pt = r.Trazado[i];
                    if (pt == null || pt.Length != 2)
                        return $"Trazado, punto {i + 1}: debe ser un par [latitud, longitud]";
                    if (pt[0] < -90 || pt[0] > 90 || pt[1] < -180 || pt[1] > 180)
                        return $"Trazado, punto {i + 1}: coordenadas fuera de rango";
                }
            }

            r.Codigo = r.Codigo.Trim();
            r.Nombre = r.Nombre.Trim();
            r.Grupo = string.IsNullOrWhiteSpace(r.Grupo) ? null : r.Grupo.Trim();

            if (string.IsNullOrWhiteSpace(r.Sentido))
                r.Sentido = null;
            else
            {
                r.Sentido = r.Sentido.Trim().ToUpperInvariant();
                if (r.Sentido != "IDA" && r.Sentido != "REGRESO")
                    return "El sentido debe ser IDA o REGRESO";
            }

            for (int i = 0; i < r.Paraderos.Count; i++)
            {
                var p = r.Paraderos[i];
                string etiqueta = $"Paradero {i + 1}";
                if (string.IsNullOrWhiteSpace(p.Nombre)) return $"{etiqueta}: el nombre es obligatorio";
                if (p.Nombre.Length > 150) return $"{etiqueta}: el nombre no puede superar 150 caracteres";
                if (p.Latitud < -90 || p.Latitud > 90) return $"{etiqueta}: la latitud debe estar entre -90 y 90";
                if (p.Longitud < -180 || p.Longitud > 180) return $"{etiqueta}: la longitud debe estar entre -180 y 180";
                if (p.RadioMetros <= 0) return $"{etiqueta}: el radio debe ser mayor a 0";
                if (p.MinutosDesdeAnterior < 0) return $"{etiqueta}: los minutos no pueden ser negativos";
                p.Nombre = p.Nombre.Trim();
            }

            return null;
        }

        private async Task<ActionResult?> ValidarServicio(ServicioRuta s)
        {
            if (string.IsNullOrWhiteSpace(s.DeviceID))
                return BadRequest(new { message = "La unidad (deviceID) es obligatoria" });
            if (s.RutaId <= 0)
                return BadRequest(new { message = "La ruta es obligatoria" });
            if (!s.FechaSalida.HasValue)
                return BadRequest(new { message = "La fecha de salida es obligatoria (dd/MM/yyyy)" });
            if (!s.HoraSalida.HasValue)
                return BadRequest(new { message = "La hora de salida es obligatoria (HH:mm)" });
            if (s.HoraSalida.Value < TimeSpan.Zero || s.HoraSalida.Value >= TimeSpan.FromDays(1))
                return BadRequest(new { message = "La hora de salida debe estar entre 00:00 y 23:59" });
            if (string.IsNullOrWhiteSpace(s.ConductorNombre))
                return BadRequest(new { message = "El nombre del conductor es obligatorio" });
            if (string.IsNullOrWhiteSpace(s.ConductorBrevete))
                return BadRequest(new { message = "El brevete del conductor es obligatorio" });
            if (string.IsNullOrWhiteSpace(s.ConductorCelular))
                return BadRequest(new { message = "El celular del conductor es obligatorio" });

            s.DeviceID = s.DeviceID.Trim();
            s.ConductorNombre = s.ConductorNombre.Trim();
            s.ConductorBrevete = s.ConductorBrevete.Trim();
            s.ConductorCelular = s.ConductorCelular.Trim();

            if (!await _uow.ConfiguracionRepository.DispositivoPerteneceACuentaAsync(s.AccountID, s.DeviceID))
                return BadRequest(new { message = "La unidad no existe o no pertenece a tu cuenta" });

            int paraderos = await _uow.ServiciosRutasRepository.ContarParaderosDeRutaActiva(s.RutaId, s.AccountID);
            if (paraderos < 2)
                return BadRequest(new { message = "La ruta no existe, está inactiva o no tiene paraderos suficientes" });

            return null;
        }

        // Hora local de Perú (UTC-5, sin horario de verano), la misma convención del resto del backend.
        private static DateTime AhoraPeru() => DateTime.UtcNow.AddHours(-5);

        private static readonly string[] FormatosMarca = { "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss" };

        // Sin valor es válido (el servidor usa la hora actual); con valor debe respetar el formato.
        private static bool TryParseMarca(string? valor, out DateTime? hora)
        {
            hora = null;
            if (string.IsNullOrWhiteSpace(valor))
                return true;

            if (DateTime.TryParseExact(valor.Trim(), FormatosMarca, CultureInfo.InvariantCulture, DateTimeStyles.None, out var h))
            {
                hora = h;
                return true;
            }
            return false;
        }

        private static bool TryParseFecha(string? valor, out DateTime? fecha)
        {
            fecha = null;
            if (string.IsNullOrWhiteSpace(valor))
                return true;

            if (DateTime.TryParseExact(valor.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
            {
                fecha = f;
                return true;
            }
            return false;
        }
    }
}
