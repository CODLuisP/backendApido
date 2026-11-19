using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model;

namespace VelsatBackendApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeviceListController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET

        public DeviceListController(IReadOnlyUnitOfWork readOnlyUow)
        {
            _readOnlyUow = readOnlyUow;
        }

        // PASO 3: Envolver métodos en using
        [HttpGet("{username}")]
        public async Task<IActionResult> ObtenerDatosCargaInicial(string username)
        {
            var datosCargaInicial = await _readOnlyUow.DatosCargainicialService.ObtenerDatosCargaInicialAsync(username);
            datosCargaInicial.FechaActual = DateTime.Now;
            return Ok(datosCargaInicial);

        }

        [HttpGet("simplified/{username}")]
        public async Task<IActionResult> SimplifiedList(string username)
        {

            var datosCargaInicial = await _readOnlyUow.DatosCargainicialService.SimplifiedList(username);
            return Ok(datosCargaInicial);

        }

        // Servicio vigente por placa (programado = "1", en curso = "2"), leído de la base DO.
        // El front lo consulta aparte del polling de posiciones, con una frecuencia menor.
        [HttpGet("ServiciosActuales/{username}")]
        public async Task<IActionResult> ServiciosActuales(string username)
        {
            var servicios = await _readOnlyUow.ServicioActualRepository.ObtenerServiciosActuales(username);
            return Ok(servicios);
        }

        [HttpGet("Unidad/{username}/{placa}")]
        public async Task<IActionResult> ObtenerDeviceList(string username, string placa)
        {
            var datosDeviceList = await _readOnlyUow.DatosCargainicialService.ObtenerDatosVehiculoAsync(username, placa);
            datosDeviceList.FechaActual = DateTime.Now;
            return Ok(datosDeviceList);
        }

        [HttpGet("CantidadRegistros")]
        public async Task<IActionResult> ObtenerCantidadRegistros()
        {
            var cantidadRegistros = await _readOnlyUow.DatosCargainicialService.CantidadRegistros();
            return Ok(cantidadRegistros);
        }
    }
}
