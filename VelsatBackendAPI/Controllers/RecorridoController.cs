using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.RecorridoServicios;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecorridoController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecorridoController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("SelectServicio")]
        public async Task<ActionResult<List<SelectedServicio>>> GetSelectServicio([FromQuery] string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha))
            {
                return BadRequest("La fecha es obligatoria.");
            }

            try
            {
                var resultado = await _unitOfWork.RecorridoRepository.GetSelectServicio(fecha);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Logear el error si lo deseas
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("DatoServicio")]
        public async Task<ActionResult<RecorridoServicio>> GetDatosServicio([FromQuery] string fecha, [FromQuery] string numero)
        {
            if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(numero))
            {
                return BadRequest("Los parámetros 'fecha' y 'numero' son obligatorios.");
            }

            try
            {
                var resultado = await _unitOfWork.RecorridoRepository.GetDatosServicio(fecha, numero);

                if (resultado == null)
                {
                    return NotFound("No se encontró el servicio solicitado.");
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("PasajerosServicio/{codservicio}")]
        public async Task<IActionResult> GetPasajerosPorServicio(string codservicio)
        {
            if (string.IsNullOrWhiteSpace(codservicio))
                return BadRequest("El código de servicio es requerido.");

            var pasajeros = await _unitOfWork.RecorridoRepository.GetPasajerosServicio(codservicio);

            if (pasajeros == null || !pasajeros.Any())
                return NotFound("No se encontraron pasajeros para este servicio.");

            return Ok(pasajeros);
        }
    }
}
