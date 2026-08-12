using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServTurismoController : Controller
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        public ServTurismoController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        // GET api/servturismo?fechaInicio=01/07/2026&fechaFin=31/07/2026&brevete=Q12345678
        // "brevete" es opcional: lo usa la app móvil para traer solo los servicios del conductor logueado.
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string fechaInicio, [FromQuery] string fechaFin, [FromQuery] string? brevete = null)
        {
            if (string.IsNullOrEmpty(fechaInicio) || string.IsNullOrEmpty(fechaFin))
            {
                return BadRequest(new { mensaje = "Los parámetros fechaInicio y fechaFin son requeridos" });
            }

            if (!DateTime.TryParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaInicioDt)
                || !DateTime.TryParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaFinDt))
            {
                return BadRequest(new { mensaje = "Formato de fecha incorrecto. Debe ser dd/MM/yyyy" });
            }

            try
            {
                var servicios = await _readOnlyUow.ServTurismoRepository.GetByFechas(fechaInicioDt, fechaFinDt, brevete);

                if (servicios == null || servicios.Count == 0)
                {
                    return NotFound(new { mensaje = "No se encontraron servicios de turismo en el rango de fechas indicado." });
                }

                return Ok(servicios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los servicios de turismo.", error = ex.Message });
            }
        }

        // POST api/servturismo
        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] ServTurismo servicio)
        {
            if (servicio == null)
            {
                return BadRequest(new { mensaje = "El servicio no puede ser nulo." });
            }

            try
            {
                int idservicio = await _uow.ServTurismoRepository.Insert(servicio);
                _uow.SaveChanges();

                return Ok(new { mensaje = "Servicio de turismo creado correctamente.", idservicio });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear el servicio de turismo.", error = ex.Message });
            }
        }

        // POST api/servturismo/lote
        // Inserción en lote (ej. carga desde Excel en el front). Ejecuta INSERTs multi-VALUES por bloques
        // para minimizar los round-trips a la base de datos y evitar lentitud con listas grandes.
        [HttpPost("lote")]
        public async Task<IActionResult> InsertLote([FromBody] List<ServTurismo> servicios)
        {
            if (servicios == null || servicios.Count == 0)
            {
                return BadRequest(new { mensaje = "La lista de servicios está vacía o es nula." });
            }

            try
            {
                int insertados = await _uow.ServTurismoRepository.InsertBatch(servicios);
                _uow.SaveChanges();

                return Ok(new { mensaje = "Servicios de turismo insertados correctamente.", insertados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al insertar los servicios de turismo en lote.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}?limpiarNulos=true
        // limpiarNulos=true: los campos que llegan en null SÍ se aplican (borran el dato en la BD).
        // Pensado para el formulario de edición del front, que siempre envía el objeto completo.
        [HttpPatch("{idservicio}")]
        public async Task<IActionResult> Patch(int idservicio, [FromBody] ServTurismo campos, [FromQuery] bool limpiarNulos = false)
        {
            if (campos == null)
            {
                return BadRequest(new { mensaje = "El servicio no puede ser nulo." });
            }

            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.Patch(idservicio, campos, limpiarNulos);

                if (filasAfectadas == -1)
                {
                    return Conflict(new { mensaje = "El servicio está cancelado y ya no se puede editar." });
                }

                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Servicio de turismo actualizado correctamente.", filasAfectadas });
                }

                return NotFound(new { mensaje = "No se encontró el servicio o no se realizaron cambios." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el servicio de turismo.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/visto
        // La app móvil lo llama automáticamente cuando el servicio se muestra en pantalla
        // (equivale al doble check azul de WhatsApp). Es idempotente.
        [HttpPatch("{idservicio}/visto")]
        public async Task<IActionResult> MarcarVisto(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarVisto(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio marcado como visto.", idservicio, visto = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al marcar el servicio como visto.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/confirmado
        // La app móvil lo llama cuando el conductor desliza la tarjeta del servicio,
        // confirmando que lo recibió. Es idempotente.
        [HttpPatch("{idservicio}/confirmado")]
        public async Task<IActionResult> MarcarConfirmado(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarConfirmado(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio confirmado por el conductor.", idservicio, confirmado = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al confirmar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/finalizar
        // La app móvil lo llama cuando el conductor desliza la tarjeta hacia la derecha y confirma
        // el modal de advertencia (acción irreversible). Es idempotente.
        [HttpPatch("{idservicio}/finalizar")]
        public async Task<IActionResult> MarcarFinalizado(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarFinalizado(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio finalizado por el conductor.", idservicio, finalizado = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al finalizar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/cancelar
        // Cancela el servicio (estado = "Cancelado") en vez de eliminarlo físicamente. Es idempotente.
        [HttpPatch("{idservicio}/cancelar")]
        public async Task<IActionResult> MarcarCancelado(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarCancelado(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio cancelado correctamente.", idservicio, cancelado = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cancelar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/standby
        // Pausa el servicio (el conductor deja de verlo) sin cancelarlo definitivamente. Es idempotente.
        [HttpPatch("{idservicio}/standby")]
        public async Task<IActionResult> MarcarStandby(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarStandby(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio puesto en Stand By correctamente.", idservicio, standby = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al poner el servicio en Stand By.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/reanudar
        // Reanuda un servicio que estaba en Stand By, volviéndolo a mostrar al conductor. Es idempotente.
        [HttpPatch("{idservicio}/reanudar")]
        public async Task<IActionResult> MarcarReanudar(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarReanudar(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio reanudado correctamente.", idservicio, standby = 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al reanudar el servicio.", error = ex.Message });
            }
        }

        // DELETE api/servturismo/{idservicio}
        [HttpDelete("{idservicio}")]
        public async Task<IActionResult> Delete(int idservicio)
        {
            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.Delete(idservicio);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Servicio de turismo eliminado correctamente." });
                }

                return NotFound(new { mensaje = "No se encontró el servicio a eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar el servicio de turismo.", error = ex.Message });
            }
        }

        // ===================== TAXI (CONDUCTORES) CRUD =====================

        // GET api/servturismo/taxi?codusuario=...
        [HttpGet("taxi")]
        public async Task<IActionResult> GetTaxis([FromQuery] string codusuario)
        {
            if (string.IsNullOrEmpty(codusuario))
            {
                return BadRequest(new { mensaje = "El parámetro codusuario es requerido" });
            }

            try
            {
                var taxis = await _readOnlyUow.ServTurismoRepository.GetTaxis(codusuario);

                if (taxis == null || taxis.Count == 0)
                {
                    return NotFound(new { mensaje = "No se encontraron conductores." });
                }

                return Ok(taxis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los conductores.", error = ex.Message });
            }
        }

        // GET api/servturismo/taxi/{codtaxi}
        [HttpGet("taxi/{codtaxi}")]
        public async Task<IActionResult> GetTaxiById(int codtaxi)
        {
            try
            {
                var taxi = await _readOnlyUow.ServTurismoRepository.GetTaxiById(codtaxi);

                if (taxi == null)
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }

                return Ok(taxi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el conductor.", error = ex.Message });
            }
        }

        // POST api/servturismo/taxi
        [HttpPost("taxi")]
        public async Task<IActionResult> InsertTaxi([FromBody] ConductorTurismo taxi)
        {
            if (taxi == null)
            {
                return BadRequest(new { mensaje = "El conductor no puede ser nulo." });
            }

            try
            {
                int codtaxi = await _uow.ServTurismoRepository.InsertTaxi(taxi);
                _uow.SaveChanges();

                return Ok(new { mensaje = "Conductor creado correctamente.", codtaxi });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear el conductor.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/taxi/{codtaxi}
        [HttpPatch("taxi/{codtaxi}")]
        public async Task<IActionResult> PatchTaxi(int codtaxi, [FromBody] ConductorTurismo campos)
        {
            if (campos == null)
            {
                return BadRequest(new { mensaje = "El conductor no puede ser nulo." });
            }

            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.PatchTaxi(codtaxi, campos);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Conductor actualizado correctamente.", filasAfectadas });
                }

                return NotFound(new { mensaje = "No se encontró el conductor o no se realizaron cambios." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el conductor.", error = ex.Message });
            }
        }

        // DELETE api/servturismo/taxi/{codtaxi}
        [HttpDelete("taxi/{codtaxi}")]
        public async Task<IActionResult> DeleteTaxi(int codtaxi)
        {
            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.DeleteTaxi(codtaxi);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Conductor eliminado correctamente." });
                }

                return NotFound(new { mensaje = "No se encontró el conductor a eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar el conductor.", error = ex.Message });
            }
        }
    }
}
