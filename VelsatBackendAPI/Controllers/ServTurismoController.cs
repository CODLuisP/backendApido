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

        // GET api/servturismo?fechaInicio=01/07/2026&fechaFin=31/07/2026
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string fechaInicio, [FromQuery] string fechaFin)
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
                var servicios = await _readOnlyUow.ServTurismoRepository.GetByFechas(fechaInicioDt, fechaFinDt);

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

        // PATCH api/servturismo/{idservicio}
        [HttpPatch("{idservicio}")]
        public async Task<IActionResult> Patch(int idservicio, [FromBody] ServTurismo campos)
        {
            if (campos == null)
            {
                return BadRequest(new { mensaje = "El servicio no puede ser nulo." });
            }

            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.Patch(idservicio, campos);
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
    }
}
