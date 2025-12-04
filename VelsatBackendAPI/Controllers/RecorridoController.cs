using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.RecorridoServicios;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecorridoController : Controller
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow; // ✅ Cambiar a ReadOnly

        public RecorridoController(IReadOnlyUnitOfWork readOnlyUow) // ✅ Cambiar
        {
            _readOnlyUow = readOnlyUow;
        }

        [HttpGet("SelectServicio")]
        public async Task<ActionResult<List<SelectedServicio>>> GetSelectServicio([FromQuery] string fecha, [FromQuery] string empresa, [FromQuery] string usuario)
        {
            if (string.IsNullOrWhiteSpace(fecha))
                return BadRequest("La fecha es obligatoria.");

            try
            {
                var resultado = await _readOnlyUow.RecorridoRepository.GetSelectServicio(fecha, empresa, usuario);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los servicios", error = ex.Message });
            }

        }

        [HttpGet("DatoServicio")]
        public async Task<ActionResult<RecorridoServicio>> GetDatosServicio([FromQuery] string fecha, [FromQuery] string numero, [FromQuery] string empresa, [FromQuery] string usuario)
        {
            if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(numero))
                return BadRequest("Los parámetros 'fecha' y 'numero' son obligatorios.");

            try
            {
                var resultado = await _readOnlyUow.RecorridoRepository.GetDatosServicio(fecha, numero, empresa, usuario);

                if (resultado == null)
                    return NotFound("No se encontró el servicio solicitado.");

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el detalle del servicio", error = ex.Message });
            }

        }

        [HttpGet("PasajerosServicio/{codservicio}")]
        public async Task<IActionResult> GetPasajerosPorServicio(string codservicio)
        {
            if (string.IsNullOrWhiteSpace(codservicio))
                return BadRequest("El código de servicio es requerido.");
            try
            {
                var pasajeros = await _readOnlyUow.RecorridoRepository.GetPasajerosServicio(codservicio);

                if (pasajeros == null || !pasajeros.Any())
                    return NotFound("No se encontraron pasajeros para este servicio.");

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los pasajeros", error = ex.Message });
            }

        }
    }
}
