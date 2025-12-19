using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Cgcela;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GacelaController : Controller
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        public GacelaController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        [HttpGet("DetalleServicios")]
        public async Task<IActionResult> GetDetalleServicios([FromQuery] string usuario, [FromQuery] string fechaIni, [FromQuery] string fechaFin)
        {
            try
            {
                var pedidos = await _readOnlyUow.GacelaRepository.GetDetalleServicios(usuario, fechaIni, fechaFin);
                if (pedidos == null || !pedidos.Any())
                    return NotFound("No se encontraron servicios de atención.");

                return Ok(pedidos);
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("DuracionServicios")]
        public async Task<IActionResult> GetDuracionServicios([FromQuery] string usuario, [FromQuery] string fechaIni, [FromQuery] string fechaFin)
        {
            try
            {
                var servicios = await _readOnlyUow.GacelaRepository.GetDuracionServicios(usuario, fechaIni, fechaFin);
                if (servicios == null || !servicios.Any())
                    return NotFound("No se encontraron servicios de atención por duración.");

                return Ok(servicios);
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpGet("UnidadesCercanas")]
        public async Task<IActionResult> GetUnidadesCercanas([FromQuery] double km, [FromQuery] string codunidad, [FromQuery] string usuario)
        {
            try
            {
                var carroBase = new GCarro { Codunidad = codunidad };
                var unidadesCercanas = await _readOnlyUow.GacelaRepository.GetUnidadesCercanas(km, carroBase, usuario);

                if (unidadesCercanas == null || !unidadesCercanas.Any())
                    return NotFound("No se encontraron unidades cercanas dentro del radio especificado.");

                return Ok(new
                {
                    RadioBusqueda = km,
                    UnidadBase = codunidad,
                    TotalEncontradas = unidadesCercanas.Count(),
                    Unidades = unidadesCercanas
                });
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpGet("Getservicios")]
        public async Task<IActionResult> GetServiciosRango([FromQuery] string fechaini, [FromQuery] string fechafin, [FromQuery] string usu)
        {
            try
            {
                if (string.IsNullOrEmpty(fechaini) || string.IsNullOrEmpty(fechafin) || string.IsNullOrEmpty(usu))
                    return BadRequest("Los parámetros fechaini, fechafin y usuario son requeridos.");

                var servicios = await _readOnlyUow.GacelaRepository.GetServicios(fechaini, fechafin, usu);
                if (servicios == null || servicios.Count == 0)
                    return NotFound("No se encontraron servicios en el rango de fechas especificado.");

                return Ok(servicios);
            }
            catch (FormatException ex)
            {
                return BadRequest($"Error en el formato de fecha: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Error en los parámetros: {ex.Message}");
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpGet("PasajeroList")]
        public async Task<IActionResult> ListaPasajeroServicio([FromQuery] string codservicio)
        {
            if (string.IsNullOrEmpty(codservicio))
                return BadRequest(new { mensaje = "El parámetro 'codservicio' es obligatorio." });

            var pasajeros = await _readOnlyUow.GacelaRepository.ListaPasajeroServicio(codservicio);

            if (pasajeros == null || !pasajeros.Any())
                return NotFound(new { mensaje = "No se encontraron pasajeros para el servicio." });

            return Ok(pasajeros);

        }

        [HttpPut("UpdateEstado")]
        public async Task<IActionResult> UpdateEstado([FromBody] GPedido pedido)
        {
            if (pedido == null)
                return BadRequest("El pedido no puede ser nulo.");

            int resultado = await _uow.GacelaRepository.UpdateEstadoServicio(pedido);
            _uow.SaveChanges();

            if (resultado > 0)
                return Ok(new { mensaje = "Estado actualizado correctamente", filasAfectadas = resultado });

            return BadRequest("No se pudo actualizar el pedido.");

        }

        [HttpPut("NuevoSubServicioPasajero")]
        public async Task<IActionResult> NuevoSubServicioPasajero([FromBody] List<GPedido> pasajeros)
        {
            try
            {
                if (pasajeros == null || !pasajeros.Any())
                    return BadRequest("No se enviaron pasajeros.");

                int totalFilasAfectadas = 0;
                foreach (var pedido in pasajeros)
                    totalFilasAfectadas += await _uow.GacelaRepository.NuevoSubServicioPasajero(pedido);

                _uow.SaveChanges();

                if (totalFilasAfectadas > 0)
                    return Ok(new { mensaje = $"Se crearon {totalFilasAfectadas} subservicios correctamente", filasAfectadas = totalFilasAfectadas });

                return BadRequest("No se pudo crear ningún subservicio.");
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpPut("ReiniciarServicio/{codservicio}")]
        public async Task<IActionResult> ReiniciarServicio(int codservicio)
        {
            if (codservicio <= 0)
                return BadRequest("El código de servicio debe ser mayor que cero.");

            int resultado = await _uow.GacelaRepository.ReiniciarServicio(codservicio);
            _uow.SaveChanges();

            if (resultado > 0)
                return Ok(new { mensaje = "Servicio reiniciado correctamente", filasAfectadas = resultado });

            return BadRequest("No se pudo reiniciar el servicio.");

        }

        [HttpPut("GuardarServicio")]
        public async Task<IActionResult> GuardarServicio([FromBody] List<GPedido> pedidos)
        {
            try
            {
                if (pedidos == null || !pedidos.Any())
                    return BadRequest("No se enviaron pedidos.");

                int totalFilasAfectadas = 0;
                foreach (var pedido in pedidos)
                    totalFilasAfectadas += await _uow.GacelaRepository.GuardarServicio(pedido);

                _uow.SaveChanges();

                if (totalFilasAfectadas > 0)
                    return Ok(new { mensaje = $"Se guardaron {totalFilasAfectadas} servicios correctamente", filasAfectadas = totalFilasAfectadas });

                return BadRequest("No se pudo guardar ningún servicio.");
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }


        [HttpPost("ProcessExcel")]
        public async Task<IActionResult> ProcessExcel(IFormFile file, [FromQuery] string tipoGrupo, [FromQuery] string usuario)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No se ha proporcionado un archivo válido.");

                if (string.IsNullOrEmpty(tipoGrupo) || string.IsNullOrEmpty(usuario))
                    return BadRequest("Los parámetros tipoGrupo y usuario son requeridos.");

                if (tipoGrupo != "T" && tipoGrupo != "A")
                    return BadRequest("El tipoGrupo debe ser 'T' o 'A'.");

                var tempFilePath = Path.GetTempFileName();
                try
                {
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    var servicios = await _uow.GacelaRepository.ProcessExcel(tempFilePath, tipoGrupo, usuario);
                    _uow.SaveChanges();

                    return Ok(new
                    {
                        mensaje = "Archivo procesado correctamente",
                        serviciosProcesados = servicios.Count,
                        servicios = servicios
                    });
                }
                finally
                {
                    if (System.IO.File.Exists(tempFilePath))
                        System.IO.File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno del servidor: {ex.Message}" });
            }

        }

        [HttpPost("RegistrarServicioExterno/{usuario}")]
        public async Task<IActionResult> RegistrarServicioExterno([FromBody] List<GServicio> listaServicios, [FromRoute] string usuario)
        {
            try
            {
                if (listaServicios == null || !listaServicios.Any())
                    return BadRequest("No se enviaron servicios para registrar.");

                if (string.IsNullOrWhiteSpace(usuario))
                    return BadRequest("El usuario es requerido.");

                var resultado = await _uow.GacelaRepository.RegistrarServicioExterno(listaServicios, usuario);
                _uow.SaveChanges();

                var exitosos = resultado.Where(s => s.Estado == "Servicio Registrado Correctamente").Count();
                var errores = resultado.Where(s => s.Estado != "Servicio Registrado Correctamente").ToList();

                if (exitosos > 0)
                    return Ok(new
                    {
                        mensaje = $"Se procesaron {resultado.Count} servicios",
                        serviciosConError = errores.Count,
                        detalleServicios = resultado.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            estado = s.Estado,
                            empresa = s.Empresa
                        }),
                        errores = errores.Any() ? errores.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            error = s.Estado
                        }) : null
                    });

                return BadRequest(new
                {
                    mensaje = "No se pudo registrar ningún servicio",
                    errores = errores.Select(s => new
                    {
                        numero = s.Numero,
                        codigoExterno = s.Codigoexterno,
                        error = s.Estado
                    })
                });
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpPut("CancelarServicioExterno/{usuario}")]
        public async Task<IActionResult> CancelarServicioExterno([FromBody] List<GServicio> listaServicios, [FromRoute] string usuario)
        {
            try
            {
                if (listaServicios == null || !listaServicios.Any())
                    return BadRequest("No se enviaron servicios para cancelar.");

                if (string.IsNullOrWhiteSpace(usuario))
                    return BadRequest("El usuario es requerido.");

                var resultado = await _uow.GacelaRepository.CancelarServicioExterno(listaServicios, usuario);
                _uow.SaveChanges();

                var cancelados = resultado.Where(s => s.Estado == "Servicio Cancelado").Count();
                var errores = resultado.Where(s => s.Estado != "Servicio Cancelado").ToList();

                if (cancelados > 0)
                    return Ok(new
                    {
                        mensaje = $"Se procesaron {resultado.Count} servicios",
                        serviciosCancelados = cancelados,
                        serviciosConError = errores.Count,
                        detalleServicios = resultado.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            estado = s.Estado,
                            empresa = s.Empresa
                        }),
                        errores = errores.Any() ? errores.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            error = s.Estado
                        }) : null
                    });

                return BadRequest(new
                {
                    mensaje = "No se pudo cancelar ningún servicio",
                    errores = errores.Select(s => new
                    {
                        numero = s.Numero,
                        codigoExterno = s.Codigoexterno,
                        error = s.Estado
                    })
                });
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpPut("CancelarPasajeroExterno/{usuario}")]
        public async Task<IActionResult> CancelarPasajeroExterno([FromBody] GPedido pedido, [FromRoute] string usuario)
        {
            try
            {
                if (pedido == null)
                    return BadRequest("No se envió información del pedido para cancelar.");

                if (string.IsNullOrWhiteSpace(usuario))
                    return BadRequest("El usuario es requerido.");

                var resultado = await _uow.GacelaRepository.CancelarPasajeroExterno(pedido, usuario);
                _uow.SaveChanges();

                if (resultado == "Cancelación de pasajero existosa")
                    return Ok(new
                    {
                        mensaje = resultado,
                        codigoExterno = pedido.Servicio.Codigoexterno,
                        codlanPasajero = pedido.Pasajero.Codlan,
                        estado = "Cancelado"
                    });

                return BadRequest(new
                {
                    mensaje = resultado,
                    codigoExterno = pedido.Servicio.Codigoexterno,
                    codlanPasajero = pedido.Pasajero.Codlan
                });
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpPost("AgregarPasajeroExterno/{usuario}")]
        public async Task<IActionResult> AgregarPasajeroExterno([FromBody] GPedido pedido, [FromRoute] string usuario)
        {
            try
            {
                if (pedido == null)
                    return BadRequest("No se envió información del pedido para agregar.");

                if (string.IsNullOrWhiteSpace(usuario))
                    return BadRequest("El usuario es requerido.");

                var resultado = await _uow.GacelaRepository.AgregarPasajeroExterno(pedido, usuario);
                _uow.SaveChanges();

                if (resultado == "Se agrego pasajero de forma existosa")
                    return Ok(new
                    {
                        mensaje = resultado,
                        codigoExterno = pedido.Servicio.Codigoexterno,
                        codlanPasajero = pedido.Pasajero.Codlan,
                        nombrePasajero = pedido.Pasajero.Nombre,
                        orden = pedido.Orden,
                        estado = "Agregado"
                    });

                return BadRequest(new
                {
                    mensaje = resultado,
                    codigoExterno = pedido.Servicio.Codigoexterno,
                    codlanPasajero = pedido.Pasajero?.Codlan
                });
            }
            catch
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpPost("ActualizarPasajeroExterno/{usuario}")]
        public async Task<IActionResult> ActualizarPasajeroExterno([FromBody] List<GUsuario> listaClientes, [FromRoute] string usuario)
        {
            try
            {
                if (listaClientes == null || !listaClientes.Any())
                    return BadRequest("No se envió información de clientes para actualizar.");

                if (string.IsNullOrWhiteSpace(usuario))
                    return BadRequest("El usuario es requerido.");

                var resultado = await _uow.GacelaRepository.ActualizarPasajeroExterno(listaClientes, usuario);
                _uow.SaveChanges();

                // Verificar si todos fueron exitosos
                var todosExitosos = resultado.All(c => c.Observacion == "Cliente registrado");
                var algunosExitosos = resultado.Any(c => c.Observacion == "Cliente registrado");

                if (todosExitosos)
                {
                    return Ok(new
                    {
                        mensaje = "Todos los clientes fueron registrados exitosamente",
                        totalProcesados = resultado.Count,
                        clientes = resultado.Select(c => new
                        {
                            codlan = c.Codlan,
                            nombre = c.Nombre,
                            empresa = c.Empresa,
                            estado = c.Observacion
                        })
                    });
                }
                else if (algunosExitosos)
                {
                    return Ok(new
                    {
                        mensaje = "Algunos clientes fueron registrados con errores",
                        totalProcesados = resultado.Count,
                        exitosos = resultado.Count(c => c.Observacion == "Cliente registrado"),
                        fallidos = resultado.Count(c => c.Observacion != "Cliente registrado"),
                        clientes = resultado.Select(c => new
                        {
                            codlan = c.Codlan,
                            nombre = c.Nombre,
                            empresa = c.Empresa,
                            estado = c.Observacion
                        })
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        mensaje = "Ningún cliente pudo ser registrado",
                        totalProcesados = resultado.Count,
                        clientes = resultado.Select(c => new
                        {
                            codlan = c.Codlan,
                            nombre = c.Nombre,
                            empresa = c.Empresa,
                            error = c.Observacion
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Hubo un error al procesar la solicitud.",
                    error = ex.Message
                });
            }
        }
    }
}
