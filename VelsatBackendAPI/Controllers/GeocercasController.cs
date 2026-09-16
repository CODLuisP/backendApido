using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/geocercas")]
    [ApiController]
    public class GeocercasController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;
        private readonly IUnitOfWork _uow;

        public GeocercasController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        [HttpPost]
        public async Task<ActionResult> CrearGeocerca([FromBody] Geocercas geocerca)
        {
            try
            {
                var id = await _uow.GeocercasRepository.Insert(geocerca);
                _uow.SaveChanges();
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear la geocerca", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Geocercas>>> GetGeocercas([FromQuery] string accountID)
        {
            try
            {
                var result = await _readOnlyUow.GeocercasRepository.GetByAccount(accountID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las geocercas", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Geocercas>> GetGeocerca(int id)
        {
            try
            {
                var result = await _readOnlyUow.GeocercasRepository.GetById(id);
                if (result == null)
                    return NotFound(new { message = "Geocerca no encontrada" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la geocerca", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> ActualizarGeocerca(int id, [FromBody] Geocercas geocerca)
        {
            try
            {
                geocerca.Id = id;
                var result = await _uow.GeocercasRepository.Update(geocerca);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la geocerca", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> EliminarGeocerca(int id)
        {
            try
            {
                var result = await _uow.GeocercasRepository.Delete(id);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar la geocerca", error = ex.Message });
            }
        }
    }
}
