using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Controllers
{
    [ApiController]
    public class GeocercasVehiculosController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;
        private readonly IUnitOfWork _uow;

        public GeocercasVehiculosController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        [HttpPost("api/geocercas/{id}/vehiculos")]
        public async Task<ActionResult> VincularVehiculos(int id, [FromBody] IEnumerable<string> deviceIds)
        {
            try
            {
                var result = await _uow.GeocercasVehiculosRepository.Vincular(id, deviceIds);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al vincular vehículos a la geocerca", error = ex.Message });
            }
        }

        [HttpDelete("api/geocercas/{id}/vehiculos/{deviceID}")]
        public async Task<ActionResult> DesvincularVehiculo(int id, string deviceID)
        {
            try
            {
                var result = await _uow.GeocercasVehiculosRepository.Desvincular(id, deviceID);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al desvincular el vehículo de la geocerca", error = ex.Message });
            }
        }

        [HttpGet("api/geocercas/{id}/vehiculos")]
        public async Task<ActionResult<IEnumerable<GeocercasVehiculos>>> GetVehiculosDeGeocerca(int id)
        {
            try
            {
                var result = await _readOnlyUow.GeocercasVehiculosRepository.GetByGeocerca(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los vehículos de la geocerca", error = ex.Message });
            }
        }

        [HttpGet("api/vehiculos/{deviceID}/geocercas")]
        public async Task<ActionResult<IEnumerable<Geocercas>>> GetGeocercasDeVehiculo(string deviceID)
        {
            try
            {
                var result = await _readOnlyUow.GeocercasVehiculosRepository.GetGeocercasByDevice(deviceID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las geocercas del vehículo", error = ex.Message });
            }
        }
    }
}
