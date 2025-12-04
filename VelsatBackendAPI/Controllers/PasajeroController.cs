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
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        // ✅ CAMBIO: Inyectar Factory en lugar de UnitOfWork
        public PasajeroController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CodNomPas>>> GetCodigo()
        {

            var result = await _readOnlyUow.PasajerosRepository.GetCodigo();
            return Ok(result);

        }

        [HttpGet("Detail/{codcliente}")]
        public async Task<ActionResult<IEnumerable<Pasajero>>> GetPasajero(int codcliente)
        {

            var result = await _readOnlyUow.PasajerosRepository.GetPasajero(codcliente);
            return Ok(result);

        }

        [HttpGet("Tarifa/{codusuario}")]
        public async Task<ActionResult<IEnumerable<Tarifa>>> GetTarifa(string codusuario)
        {
            var result = await _readOnlyUow.PasajerosRepository.GetTarifa(codusuario);
            return Ok(result);

        }

        [HttpPost("New/{codusuario}")]
        public async Task<ActionResult> InsertPasajero([FromBody] Pasajero pasajero, string codusuario)
        {
            try
            {
                var result = await _uow.PasajerosRepository.InsertPasajero(pasajero, codusuario);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar pasajero", error = ex.Message });
            }

        }

        [HttpPut("Update/{codusuario}/{codcliente}/{codlan}/{codlugar}")]
        public async Task<ActionResult> UpdatePasajero([FromBody] Pasajero pasajero, string codusuario, int codcliente, string codlan, string codlugar)
        {
            try
            {
                var result = await _uow.PasajerosRepository.UpdatePasajero(pasajero, codusuario, codcliente, codlan, codlugar);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar pasajero", error = ex.Message });
            }

        }

        [HttpDelete("Delete/{codcliente}/{codusuario}")]
        public async Task<ActionResult> DeletePasajero(int codcliente, string codusuario)
        {

            try
            {
                var result = await _uow.PasajerosRepository.DeletePasajero(codcliente, codusuario);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar pasajero", error = ex.Message });
            }

        }

        [HttpGet("GetPasajerosCodigo")]
        public async Task<IActionResult> GetPasajerosCodigo([FromQuery] string codlan)
        {
            try
            {
                var pasajeros = await _readOnlyUow.PasajerosRepository.GetPasajerosCodigo(codlan);
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

        [HttpPost("NewDestino/{codusuario}")]
        public async Task<ActionResult> InsertDestino([FromBody] Pasajero pasajero, string codusuario)
        {
            try
            {
                var result = await _uow.PasajerosRepository.InsertDestino(pasajero, codusuario);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar destino", error = ex.Message });
            }

        }
    }
}