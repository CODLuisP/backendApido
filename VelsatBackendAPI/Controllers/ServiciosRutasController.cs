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

                var errorPareja = await ValidarPareja(null, request.Sentido ?? "IDA", request.RutaParejaId);
                if (errorPareja != null)
                    return BadRequest(new { message = errorPareja });

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

                var errorPareja = await ValidarPareja(id, request.Sentido ?? existente.Sentido, request.RutaParejaId);
                if (errorPareja != null)
                    return BadRequest(new { message = errorPareja });

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
            }
            catch (Exception ex)
            {
                // Un fallo al notificar a la web no debe hacer fallar el envío del celular.
                return Ok(new { recibida = true, notificada = false, error = ex.Message });
            }

            return Ok(new { recibida = true, notificada = true });
        }

        // Última posición de cada unidad que reportó en los últimos 2 minutos (la web la usa una sola vez
        // al abrirse; luego recibe todo por SignalR). Viene de memoria, no de la base de datos.
        [HttpGet("posiciones")]
        public ActionResult<IEnumerable<PosicionEnVivo>> GetPosiciones()
        {
            return Ok(_posiciones.Vigentes(AccountId));
        }

        // =====================================================================
        // Validaciones
        // =====================================================================

        // La pareja debe existir en la misma cuenta, no ser la propia ruta y tener el sentido contrario.
        private async Task<string?> ValidarPareja(long? rutaId, string sentido, long? parejaId)
        {
            if (!parejaId.HasValue) return null;
            if (rutaId.HasValue && parejaId.Value == rutaId.Value)
                return "Una ruta no puede ser su propia pareja";

            var pareja = await _uow.ServiciosRutasRepository.GetRutaById(parejaId.Value, AccountId!);
            if (pareja == null) return "La ruta pareja no existe";
            if (string.Equals(pareja.Sentido, sentido, StringComparison.OrdinalIgnoreCase))
                return $"La ruta pareja debe ser de sentido {(sentido == "IDA" ? "REGRESO" : "IDA")}";
            return null;
        }

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
            if (r.RutaParejaId.HasValue && r.RutaParejaId.Value <= 0) r.RutaParejaId = null;

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
