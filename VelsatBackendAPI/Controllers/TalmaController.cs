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

        [HttpDelete("DeletePreplanTalma/{codigo}")]
        public async Task<IActionResult> DeletePreplanTalma(int codigo)
        {
            try
            {
                var resultado = await _uow.TalmaRepository.DeletePreplanTalma(codigo);

                if (resultado)
                {
                    _uow.SaveChanges();
                    return Ok(new
                    {
                        mensaje = $"Registro {codigo} eliminado correctamente",
                        exitoso = true
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        mensaje = $"No se encontró el registro {codigo}",
                        exitoso = false
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = $"Error interno del servidor: {ex.Message}",
                    exitoso = false
                });
            }
        }

        [HttpGet("PreplanTalmaEliminados")]
        public async Task<IActionResult> GetPreplanTalmaEliminados([FromQuery] string tipo, [FromQuery] string fecha, [FromQuery] string hora)
        {
            try
            {
                var pedidos = await _readOnlyUow.TalmaRepository.GetPreplanTalmaEliminados(tipo, fecha, hora);
                if (pedidos == null || !pedidos.Any())
                    return NotFound("No se encontraron registros en preplan_talma.");
                return Ok(pedidos);
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPost("SavePreplanTalma")]
        public async Task<IActionResult> SavePreplanTalma([FromBody] List<UpdatePreplanTalma> pedidos)
        {
            try
            {
                // Validación inicial
                if (pedidos == null || !pedidos.Any())
                    return BadRequest("No se han proporcionado registros para procesar.");

                // Procesar todos los registros
                var registrosActualizados = await _uow.TalmaRepository.SavePreplanTalma(pedidos);

                // Guardar cambios si hay transacción pendiente
                _uow.SaveChanges();

                // Determinar el tipo de respuesta según el resultado
                if (registrosActualizados == pedidos.Count)
                {
                    return Ok(new
                    {
                        mensaje = "Todos los registros se actualizaron correctamente",
                        registrosActualizados = registrosActualizados,
                        exitoso = true
                    });
                }
                else if (registrosActualizados > 0)
                {
                    return Ok(new
                    {
                        mensaje = $"Proceso completado con errores. {registrosActualizados} de {pedidos.Count} registros actualizados correctamente",
                        registrosActualizados = registrosActualizados,
                        registrosConError = pedidos.Count - registrosActualizados,
                        exitoso = false
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        mensaje = "No se pudo actualizar ningún registro",
                        registrosConError = pedidos.Count,
                        exitoso = false
                    });
                }
            }
            catch (Exception ex)
            {
                // Log del error si tienes logger configurado
                // _logger.LogError(ex, "Error al actualizar pedidos Talma");
                return StatusCode(500, new
                {
                    mensaje = $"Error interno del servidor: {ex.Message}",
                    exitoso = false
                });
            }
        }

        [HttpGet("GetHoras")]
        public async Task<IActionResult> GetHoras([FromQuery] string fecha)
        {
            try
            {
                // Validación del parámetro
                if (string.IsNullOrWhiteSpace(fecha))
                    return BadRequest("La fecha es requerida.");

                var horas = await _readOnlyUow.TalmaRepository.GetHoras(fecha);

                if (horas == null || !horas.Any())
                    return NotFound("No se encontraron horas para la fecha especificada.");

                return Ok(horas);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error al obtener horas");
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPost("CreateServicios")]
        public async Task<IActionResult> CreateServicios([FromBody] List<ServicioRequest> servicios)
        {
            try
            {
                // Validación inicial
                if (servicios == null || !servicios.Any())
                    return BadRequest("No se han proporcionado servicios para crear.");

                // Crear los servicios
                var serviciosCreados = await _uow.TalmaRepository.CreateServicios(servicios);

                // Guardar cambios
                _uow.SaveChanges();

                if (serviciosCreados == servicios.Count)
                {
                    return Ok(new
                    {
                        mensaje = "Todos los servicios se crearon correctamente",
                        serviciosCreados = serviciosCreados,
                        exitoso = true
                    });
                }
                else if (serviciosCreados > 0)
                {
                    return Ok(new
                    {
                        mensaje = $"Se crearon {serviciosCreados} de {servicios.Count} servicios",
                        serviciosCreados = serviciosCreados,
                        serviciosConError = servicios.Count - serviciosCreados,
                        exitoso = false
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        mensaje = "No se pudo crear ningún servicio",
                        exitoso = false
                    });
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error al crear servicios");
                return StatusCode(500, new
                {
                    mensaje = $"Error interno del servidor: {ex.Message}",
                    exitoso = false
                });
            }
        }

        [HttpPost("AgregarPasajero")]
        public async Task<IActionResult> RegistrarPasajeroGrupo([FromBody] List<PedidoTalma> pedidos, [FromQuery] string usuario)
        {
            if (pedidos == null || !pedidos.Any())
                return BadRequest(new { mensaje = "La lista de pedidos no puede ser nula o vacía." });

            try
            {
                int result = await _uow.TalmaRepository.RegistrarPasajeroGrupo(pedidos, usuario);
                _uow.SaveChanges(); // ✅ Solo en POST, PUT, DELETE

                if (result > 0)
                    return Ok(new { mensaje = "Pasajeros registrados correctamente", filasAfectadas = result });

                return BadRequest(new { mensaje = "No se pudo registrar ningún pasajero." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno al registrar los pasajeros.",
                    detalle = ex.Message
                });
            }
        }

        [HttpPut("direccion/{coddire}/{codigo}")]
        public async Task<IActionResult> UpdateDirec([FromRoute] string coddire, [FromRoute] string codigo)
        {
            if (string.IsNullOrEmpty(coddire) || string.IsNullOrEmpty(codigo))
            {
                return BadRequest(new { message = "Datos inválidos. Se requiere código de cliente y dirección." });
            }

            try
            {
                int filasAfectadas = await _uow.TalmaRepository.UpdateDirec(coddire, codigo);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { message = "Dirección actualizada correctamente" });
                }
                else
                {
                    return NotFound(new { message = "No se encontró el registro para actualizar" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar dirección", error = ex.Message });
            }

        }

        [HttpDelete("eliminarCarga")]
        public async Task<IActionResult> DeleteLoad([FromQuery] string fecha, [FromQuery] string usuario, [FromQuery] string empresa)
        {
            if (string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(empresa))
            {
                return BadRequest(new { message = "Datos inválidos. Se requiere fecha, usuario y empresa." });
            }

            try
            {
                int filasAfectadas = await _uow.TalmaRepository.DeleteLoad(fecha, usuario, empresa);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { message = "Registro eliminado correctamente", filasAfectadas = filasAfectadas });
                }
                else
                {
                    return NotFound(new { message = "No se encontró el registro para eliminar" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar registro", error = ex.Message });
            }
        }
    }
}
