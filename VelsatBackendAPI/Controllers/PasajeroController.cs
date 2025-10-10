using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.GestionPasajeros;

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

        // GET - No necesita SaveChanges
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CodNomPas>>> GetCodigo()
        {
            var result = await _unitOfWork.PasajerosRepository.GetCodigo();
            return Ok(result);
        }

        // GET - No necesita SaveChanges
        [HttpGet("Detail/{codcliente}")]
        public async Task<ActionResult<IEnumerable<Pasajero>>> GetPasajero(int codcliente)
        {
            var result = await _unitOfWork.PasajerosRepository.GetPasajero(codcliente);
            return Ok(result);
        }

        // GET - No necesita SaveChanges
        [HttpGet("Tarifa/{codusuario}")]
        public async Task<ActionResult<IEnumerable<Tarifa>>> GetTarifa(string codusuario)
        {
            var result = await _unitOfWork.PasajerosRepository.GetTarifa(codusuario);
            return Ok(result);
        }

        // POST - Necesita SaveChanges
        [HttpPost("New/{codusuario}")]
        public async Task<ActionResult> InsertPasajero([FromBody] Pasajero pasajero, string codusuario)
        {
            try
            {
                var result = await _unitOfWork.PasajerosRepository.InsertPasajero(pasajero, codusuario);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar pasajero", error = ex.Message });
            }
        }

        // PUT - Necesita SaveChanges
        [HttpPut("Update/{codusuario}/{codcliente}/{codlan}")]
        public async Task<ActionResult> UpdatePasajero([FromBody] Pasajero pasajero, string codusuario, int codcliente, string codlan)
        {
            try
            {
                var result = await _unitOfWork.PasajerosRepository.UpdatePasajero(pasajero, codusuario, codcliente, codlan);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar pasajero", error = ex.Message });
            }
        }

        // DELETE - Necesita SaveChanges
        [HttpDelete("Delete/{codcliente}/{codusuario}")]
        public async Task<ActionResult> DeletePasajero(int codcliente, string codusuario)
        {
            try
            {
                var result = await _unitOfWork.PasajerosRepository.DeletePasajero(codcliente, codusuario);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar pasajero", error = ex.Message });
            }
        }

        // GET - No necesita SaveChanges
        [HttpGet("GetPasajerosCodigo")]
        public async Task<IActionResult> GetPasajerosCodigo([FromQuery] string codlan)
        {
            try
            {
                var pasajeros = await _unitOfWork.PasajerosRepository.GetPasajerosCodigo(codlan);
                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound(new { message = "No se encontraron pasajeros." });
                }
                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Hubo un error al procesar la solicitud.", error = ex.Message });
            }
        }

        // POST - Necesita SaveChanges
        [HttpPost("NewDestino/{codusuario}")]
        public async Task<ActionResult> InsertDestino([FromBody] Pasajero pasajero, string codusuario)
        {
            try
            {
                var result = await _unitOfWork.PasajerosRepository.InsertDestino(pasajero, codusuario);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar destino", error = ex.Message });
            }
        }
    }
}