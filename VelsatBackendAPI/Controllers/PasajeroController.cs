using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasajeroController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public PasajeroController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CodNomPas>>> GetCodigo()
        {
            return Ok(await _unitOfWork.PasajerosRepository.GetCodigo());
        }

        [HttpGet("Detail/{codcliente}")]
        public async Task<ActionResult<IEnumerable<Pasajero>>> GetPasajero(int codcliente)
        {
            return Ok(await _unitOfWork.PasajerosRepository.GetPasajero(codcliente));
        }

        [HttpGet("Tarifa/{codusuario}")]
        public async Task<ActionResult<IEnumerable<Tarifa>>> GetTarifa(string codusuario)
        {
            return Ok(await _unitOfWork.PasajerosRepository.GetTarifa(codusuario));
        }

        [HttpPost("New/{codusuario}")]
        public async Task<ActionResult> InsertPasajero([FromBody] Pasajero pasajero, string codusuario)
        {
            var result = await _unitOfWork.PasajerosRepository.InsertPasajero(pasajero, codusuario);

            return Ok(result);
        }

        [HttpPut("Update/{codusuario}/{codcliente}/{codlan}")]
        public async Task<ActionResult> UpdatePasajero([FromBody] Pasajero pasajero, string codusuario, int codcliente, string codlan)
        {
            return Ok(await _unitOfWork.PasajerosRepository.UpdatePasajero(pasajero, codusuario, codcliente, codlan));
        }

        [HttpDelete("Delete/{codcliente}/{codusuario}")]
        public async Task<ActionResult> DeletePasajero(int codcliente, string codusuario)
        {
            var result = await _unitOfWork.PasajerosRepository.DeletePasajero(codcliente, codusuario);
            return Ok(result);
        }

        [HttpGet("GetPasajerosCodigo")]
        public async Task<IActionResult> GetPasajerosCodigo([FromQuery] string codlan)
        {
            try
            {
                var pasajeros = await _unitOfWork.PasajerosRepository.GetPasajerosCodigo(codlan);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound("No se encontraron pasajeros.");
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }


        [HttpPost("NewDestino/{codusuario}")]
        public async Task<ActionResult> InsertDestino([FromBody] Pasajero pasajero, string codusuario)
        {
            var result = await _unitOfWork.PasajerosRepository.InsertDestino(pasajero, codusuario);

            return Ok(result);
        }
    }
}
