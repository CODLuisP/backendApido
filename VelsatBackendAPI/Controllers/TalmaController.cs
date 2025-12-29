using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Talma;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TalmaController : Controller
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        public TalmaController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        [HttpPost("InsertPedidoTalma")]
        public async Task<IActionResult> InsertPedidoTalma([FromBody] List<RegistroExcel> registros)
        {
            try
            {
                // Validaciones iniciales
                if (registros == null || !registros.Any())
                    return BadRequest("No se han proporcionado registros para procesar.");

                // Validar que todos los registros tengan los campos básicos requeridos
                var registrosInvalidos = registros.Where(r =>
                    string.IsNullOrWhiteSpace(r.Codlan) ||
                    r.Tipo == '\0' ||
                    string.IsNullOrWhiteSpace(r.Fecha) ||
                    string.IsNullOrWhiteSpace(r.Hora)
                ).ToList();

                if (registrosInvalidos.Any())
                {
                    return BadRequest(new
                    {
                        mensaje = "Algunos registros no tienen los campos requeridos",
                        registrosConError = registrosInvalidos.Select(r => new
                        {
                            id = r.Id,
                            codlan = r.Codlan,
                            motivo = "Faltan campos requeridos (Codlan, Tipo, Fecha u Hora)"
                        })
                    });
                }

                // Procesar los pedidos
                var response = await _uow.TalmaRepository.InsertPedido(registros);

                // Guardar cambios si hay transacción pendiente
                _uow.SaveChanges();

                // Determinar el tipo de respuesta según el resultado
                if (response.Success)
                {
                    return Ok(new
                    {
                        mensaje = "Todos los pedidos se procesaron correctamente",
                        registrosProcesados = registros.Count,
                        exitoso = true
                    });
                }
                else if (response.Errores.Any() && response.Errores.Count < registros.Count)
                {
                    // Algunos registros se procesaron, otros tuvieron errores
                    var registrosExitosos = registros.Count - response.Errores.Count;
                    return Ok(new
                    {
                        mensaje = $"Proceso completado con errores. {registrosExitosos} de {registros.Count} registros procesados correctamente",
                        registrosProcesados = registrosExitosos,
                        registrosConError = response.Errores.Count,
                        exitoso = false,
                        errores = response.Errores
                    });
                }
                else
                {
                    // Todos los registros fallaron
                    return BadRequest(new
                    {
                        mensaje = "No se pudo procesar ningún registro",
                        registrosConError = response.Errores.Count,
                        exitoso = false,
                        errores = response.Errores
                    });
                }
            }
            catch (Exception ex)
            {
                // Log del error si tienes logger configurado
                // _logger.LogError(ex, "Error al insertar pedidos Talma");

                return StatusCode(500, new
                {
                    mensaje = $"Error interno del servidor: {ex.Message}",
                    exitoso = false
                });
            }
        }

        [HttpGet("PreplanTalma")]
        public async Task<IActionResult> GetPreplanTalma([FromQuery] string tipo, [FromQuery] string fecha, [FromQuery] string hora)
        {
            try
            {
                var pedidos = await _readOnlyUow.TalmaRepository.GetPreplanTalma(tipo, fecha, hora);
                if (pedidos == null || !pedidos.Any())
                    return NotFound("No se encontraron registros en preplan_talma.");
                return Ok(pedidos);
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }
    }
}
