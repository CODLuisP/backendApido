using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow; // ✅ Cambiar a ReadOnly

        public ServerController(IReadOnlyUnitOfWork readOnlyUow) // ✅ Cambiar
        {
            _readOnlyUow = readOnlyUow;
        }

        [HttpGet("{accountID}")]
        public async Task<IActionResult> GetServidor(string accountID)
        {
            if (string.IsNullOrWhiteSpace(accountID))
                return BadRequest("El parámetro 'accountID' es requerido.");

            try
            {
                var server = await _readOnlyUow.ServidorRepository.GetServidor(accountID);

                if (server == null)
                    return Ok(new { mensaje = "usuario incorrecto" });

                return Ok(new { Servidor = server.servidor });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al obtener el servidor",
                    error = ex.Message
                });
            }

        }
    }
}
