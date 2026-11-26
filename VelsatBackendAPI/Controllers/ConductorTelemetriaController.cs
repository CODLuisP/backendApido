using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Data.Services;
using VelsatBackendAPI.Model.Telemetria;

namespace VelsatBackendAPI.Controllers
{
    // CRUD de conductores de telemetría. Primero se guarda en la base de datos y, solo si eso sale
    // bien, se replica en Traccar. Si Traccar falla no se deshace nada: la respuesta lo informa.
    [Route("api/[controller]")]
    [ApiController]
    public class ConductorTelemetriaController : ControllerBase
    {
        private const int ErrorClaveDuplicada = 1062;
        private static readonly Regex FormatoIbutton = new("^[0-9A-F]{16}$", RegexOptions.Compiled);

        private readonly IReadOnlyUnitOfWork _readOnlyUow;
        private readonly IUnitOfWork _uow;
        private readonly ITraccarService _traccarService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConductorTelemetriaController> _logger;

        public ConductorTelemetriaController(
            IReadOnlyUnitOfWork readOnlyUow,
            IUnitOfWork uow,
            ITraccarService traccarService,
            IServiceScopeFactory scopeFactory,
            ILogger<ConductorTelemetriaController> logger)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
            _traccarService = traccarService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // GET api/conductortelemetria?codusuario=...
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string codusuario)
        {
            if (string.IsNullOrWhiteSpace(codusuario))
            {
                return BadRequest(new { mensaje = "El parámetro codusuario es requerido." });
            }

            try
            {
                var conductores = await _readOnlyUow.ConductorTelemetriaRepository.GetByUsuario(codusuario.Trim());
                return Ok(conductores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los conductores.", error = ex.Message });
            }
        }

        // GET api/conductortelemetria/{codtaxi}
        [HttpGet("{codtaxi:int}")]
        public async Task<IActionResult> GetById(int codtaxi)
        {
            try
            {
                var conductor = await _readOnlyUow.ConductorTelemetriaRepository.GetById(codtaxi);

                if (conductor == null)
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }

                return Ok(conductor);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el conductor.", error = ex.Message });
            }
        }

        // POST api/conductortelemetria
        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] ConductorTelemetriaRequest conductor)
        {
            string? error = NormalizarYValidar(conductor);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            int codtaxi;

            try
            {
                if (conductor.CodigoIbutton != null
                    && await _readOnlyUow.ConductorTelemetriaRepository.ExisteCodigoIbutton(conductor.CodigoIbutton))
                {
                    return CodigoRepetido();
                }

                codtaxi = await _uow.ConductorTelemetriaRepository.Insert(conductor);
                _uow.SaveChanges();
            }
            catch (MySqlException ex) when (ex.Number == ErrorClaveDuplicada)
            {
                return CodigoRepetido();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear el conductor.", error = ex.Message });
            }

            // Sin código de llave no se llama a Traccar: se enviará cuando se le asigne al editar.
            if (conductor.CodigoIbutton == null)
            {
                return Ok(new { mensaje = "Conductor creado correctamente.", codtaxi, traccarSync = false });
            }

            var traccar = await _traccarService.CrearConductorAsync(NombreCompleto(conductor), conductor.CodigoIbutton);

            if (!traccar.Exito)
            {
                return Ok(new { mensaje = "Conductor creado, pero no se pudo registrar en Traccar.", codtaxi, traccarSync = false, errorTraccar = traccar.Error });
            }

            if (!GuardarEstadoTraccar(codtaxi, traccar.DriverId, true, out string? errorBd))
            {
                return Ok(new { mensaje = "Conductor creado en la base de datos y en Traccar, pero no se pudo guardar el id de Traccar.", codtaxi, traccarDriverId = traccar.DriverId, traccarSync = false, error = errorBd });
            }

            return Ok(new { mensaje = "Conductor creado correctamente.", codtaxi, traccarDriverId = traccar.DriverId, traccarSync = true });
        }

        // PUT api/conductortelemetria/{codtaxi}
        // Reemplaza todos los campos: un campo que no se envíe queda vacío.
        [HttpPut("{codtaxi:int}")]
        public async Task<IActionResult> Update(int codtaxi, [FromBody] ConductorTelemetriaRequest conductor)
        {
            string? error = NormalizarYValidar(conductor);
            if (error != null)
            {
                return BadRequest(new { mensaje = error });
            }

            int? traccarDriverId;

            try
            {
                var actual = await _readOnlyUow.ConductorTelemetriaRepository.GetById(codtaxi);
                if (actual == null)
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }

                if (conductor.CodigoIbutton != null
                    && await _readOnlyUow.ConductorTelemetriaRepository.ExisteCodigoIbutton(conductor.CodigoIbutton, codtaxi))
                {
                    return CodigoRepetido();
                }

                traccarDriverId = actual.TraccarDriverId;

                await _uow.ConductorTelemetriaRepository.Update(codtaxi, conductor);
                _uow.SaveChanges();
            }
            catch (MySqlException ex) when (ex.Number == ErrorClaveDuplicada)
            {
                return CodigoRepetido();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el conductor.", error = ex.Message });
            }

            TraccarResultado traccar;
            int? nuevoDriverId = traccarDriverId;

            if (conductor.CodigoIbutton == null)
            {
                // Nunca estuvo en Traccar y sigue sin llave: no hay nada que replicar.
                if (traccarDriverId == null)
                {
                    return Ok(new { mensaje = "Conductor actualizado correctamente.", codtaxi, traccarSync = false });
                }

                // Se le quitó la llave: se elimina de Traccar.
                traccar = await _traccarService.EliminarConductorAsync(traccarDriverId.Value);
                nuevoDriverId = null;
            }
            else if (traccarDriverId == null)
            {
                traccar = await _traccarService.CrearConductorAsync(NombreCompleto(conductor), conductor.CodigoIbutton);
                nuevoDriverId = traccar.DriverId;
            }
            else
            {
                traccar = await _traccarService.EditarConductorAsync(traccarDriverId.Value, NombreCompleto(conductor), conductor.CodigoIbutton);
            }

            if (!traccar.Exito)
            {
                return Ok(new { mensaje = "Conductor actualizado, pero no se pudo actualizar en Traccar.", codtaxi, traccarDriverId, traccarSync = false, errorTraccar = traccar.Error });
            }

            bool sincronizado = nuevoDriverId != null;

            if (!GuardarEstadoTraccar(codtaxi, nuevoDriverId, sincronizado, out string? errorBd))
            {
                return Ok(new { mensaje = "Conductor actualizado y replicado en Traccar, pero no se pudo guardar el estado de sincronización.", codtaxi, traccarDriverId = nuevoDriverId, traccarSync = false, error = errorBd });
            }

            return Ok(new { mensaje = "Conductor actualizado correctamente.", codtaxi, traccarDriverId = nuevoDriverId, traccarSync = sincronizado });
        }

        // DELETE api/conductortelemetria/{codtaxi}
        // Eliminación física en la base de datos y, después, en Traccar.
        [HttpDelete("{codtaxi:int}")]
        public async Task<IActionResult> Delete(int codtaxi)
        {
            int? traccarDriverId;

            try
            {
                var actual = await _readOnlyUow.ConductorTelemetriaRepository.GetById(codtaxi);
                if (actual == null)
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }

                traccarDriverId = actual.TraccarDriverId;

                int filas = await _uow.ConductorTelemetriaRepository.Delete(codtaxi);
                _uow.SaveChanges();

                if (filas == 0)
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar el conductor.", error = ex.Message });
            }

            if (traccarDriverId == null)
            {
                return Ok(new { mensaje = "Conductor eliminado correctamente.", codtaxi });
            }

            var traccar = await _traccarService.EliminarConductorAsync(traccarDriverId.Value);

            if (!traccar.Exito)
            {
                // La fila ya no existe: este log y la respuesta son el único rastro del id en Traccar.
                _logger.LogError("Conductor {Codtaxi} eliminado de la BD pero sigue en Traccar con id {DriverId}", codtaxi, traccarDriverId);
                return Ok(new { mensaje = "Conductor eliminado de la base de datos, pero no se pudo eliminar en Traccar.", codtaxi, traccarDriverId, errorTraccar = traccar.Error });
            }

            return Ok(new { mensaje = "Conductor eliminado correctamente.", codtaxi });
        }

        // _uow ya se confirmó (solo admite un SaveChanges), así que el resultado de Traccar se
        // guarda con uno nuevo. No lanza: el conductor ya quedó guardado.
        private bool GuardarEstadoTraccar(int codtaxi, int? traccarDriverId, bool traccarSync, out string? error)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                uow.ConductorTelemetriaRepository
                    .ActualizarEstadoTraccar(codtaxi, traccarDriverId, traccarSync)
                    .GetAwaiter().GetResult();
                uow.SaveChanges();

                error = null;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar el estado de Traccar del conductor {Codtaxi} (id Traccar {DriverId})", codtaxi, traccarDriverId);
                error = ex.Message;
                return false;
            }
        }

        // Recorta los textos, convierte los vacíos en null y normaliza el código iButton
        // (mayúsculas y sin espacios). Devuelve el mensaje de error o null si todo es válido.
        private static string? NormalizarYValidar(ConductorTelemetriaRequest conductor)
        {
            static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

            conductor.Nombres = Limpiar(conductor.Nombres);
            conductor.Apellidos = Limpiar(conductor.Apellidos);
            conductor.Dni = Limpiar(conductor.Dni);
            conductor.Telefono = Limpiar(conductor.Telefono);
            conductor.Brevete = Limpiar(conductor.Brevete);
            conductor.Codusuario = Limpiar(conductor.Codusuario);

            string codigo = new string((conductor.CodigoIbutton ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
            conductor.CodigoIbutton = codigo.Length == 0 ? null : codigo;

            if (conductor.Nombres == null || conductor.Apellidos == null || conductor.Codusuario == null)
            {
                return "Los campos nombres, apellidos y codusuario son requeridos.";
            }

            if (conductor.CodigoIbutton != null && !FormatoIbutton.IsMatch(conductor.CodigoIbutton))
            {
                return "El código iButton debe tener 16 caracteres hexadecimales (0-9, A-F).";
            }

            return null;
        }

        private static string NombreCompleto(ConductorTelemetriaRequest conductor) =>
            $"{conductor.Nombres} {conductor.Apellidos}";

        private ConflictObjectResult CodigoRepetido() =>
            Conflict(new { mensaje = "El código iButton ya está asignado a otro conductor." });
    }
}
