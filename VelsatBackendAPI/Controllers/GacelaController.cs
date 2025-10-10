using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Cgcela;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GacelaController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public GacelaController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("DetalleServicios")]
        public async Task<IActionResult> GetDetalleServicios([FromQuery] string usuario, [FromQuery] string fechaIni, [FromQuery] string fechaFin)
        {
            try
            {
                var pedidos = await _unitOfWork.GacelaRepository.GetDetalleServicios(usuario, fechaIni, fechaFin);
                if (pedidos == null || !pedidos.Any())
                {
                    return NotFound("No se encontraron servicios de atención.");
                }
                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("DuracionServicios")]
        public async Task<IActionResult> GetDuracionServicios([FromQuery] string usuario, [FromQuery] string fechaIni, [FromQuery] string fechaFin)
        {
            try
            {
                var servicios = await _unitOfWork.GacelaRepository.GetDuracionServicios(usuario, fechaIni, fechaFin);
                if (servicios == null || !servicios.Any())
                {
                    return NotFound("No se encontraron servicios de atención por duración.");
                }
                return Ok(servicios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("UnidadesCercanas")]
        public async Task<IActionResult> GetUnidadesCercanas([FromQuery] double km, [FromQuery] string codunidad, [FromQuery] string usuario)
        {
            try
            {
                // Crear objeto GCarro base
                var carroBase = new GCarro
                {
                    Codunidad = codunidad
                };

                // Llamar al repositorio
                var unidadesCercanas = await _unitOfWork.GacelaRepository.GetUnidadesCercanas(km, carroBase, usuario);

                if (unidadesCercanas == null || !unidadesCercanas.Any())
                {
                    return NotFound("No se encontraron unidades cercanas dentro del radio especificado.");
                }

                // Respuesta enriquecida con metadatos
                var response = new
                {
                    RadioBusqueda = km,
                    UnidadBase = codunidad,
                    TotalEncontradas = unidadesCercanas.Count(),
                    Unidades = unidadesCercanas
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("Getservicios")]
        public async Task<IActionResult> GetServiciosRango([FromQuery] string fechaini, [FromQuery] string fechafin, [FromQuery] string usu)
        {
            try
            {
                // Validar parámetros requeridos
                if (string.IsNullOrEmpty(fechaini) || string.IsNullOrEmpty(fechafin) || string.IsNullOrEmpty(usu))
                {
                    return BadRequest("Los parámetros fechaini, fechafin y usuario son requeridos.");
                }

                var servicios = await _unitOfWork.GacelaRepository.GetServicios(fechaini, fechafin, usu);

                if (servicios == null || servicios.Count == 0)
                {
                    return NotFound("No se encontraron servicios en el rango de fechas especificado.");
                }

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
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("PasajeroList")]
        public async Task<IActionResult> ListaPasajeroServicio([FromQuery] string codservicio)
        {
            if (string.IsNullOrEmpty(codservicio))
            {
                return BadRequest(new { mensaje = "El parámetro 'codservicio' es obligatorio." });
            }

            var pasajeros = await _unitOfWork.GacelaRepository.ListaPasajeroServicio(codservicio);

            if (pasajeros == null || !pasajeros.Any())
            {
                return NotFound(new { mensaje = "No se encontraron pasajeros para el servicio." });
            }

            return Ok(pasajeros);
        }

        [HttpPut("UpdateEstado")]
        public async Task<IActionResult> UpdateEstado([FromBody] GPedido pedido)
        {
            if (pedido == null)
                return BadRequest("El pedido no puede ser nulo.");

            int resultado = await _unitOfWork.GacelaRepository.UpdateEstadoServicio(pedido);

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
                {
                    return BadRequest("No se enviaron pasajeros.");
                }

                int totalFilasAfectadas = 0;

                foreach (var pedido in pasajeros)
                {
                    int resultado = await _unitOfWork.GacelaRepository.NuevoSubServicioPasajero(pedido);
                    totalFilasAfectadas += resultado;
                }

                _unitOfWork.SaveChanges();

                if (totalFilasAfectadas > 0)
                    return Ok(new { mensaje = $"Se crearon {totalFilasAfectadas} subservicios correctamente", filasAfectadas = totalFilasAfectadas });

                return BadRequest("No se pudo crear ningún subservicio.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPut("ReiniciarServicio/{codservicio}")]
        public async Task<IActionResult> ReiniciarServicio(int codservicio)
        {
            if (codservicio <= 0)
                return BadRequest("El código de servicio debe ser mayor que cero.");

            int resultado = await _unitOfWork.GacelaRepository.ReiniciarServicio(codservicio);

            _unitOfWork.SaveChanges();

            if (resultado > 0)
                return Ok(new { mensaje = "Servicio reiniciado correctamente", filasAfectadas = resultado });

            return BadRequest("No se pudo reiniciar el servicio. Verifique que el código de servicio exista.");
        }

        [HttpPut("GuardarServicio")]
        public async Task<IActionResult> GuardarServicio([FromBody] List<GPedido> pedidos)
        {
            try
            {
                if (pedidos == null || !pedidos.Any())
                {
                    return BadRequest("No se enviaron pedidos.");
                }

                int totalFilasAfectadas = 0;

                foreach (var pedido in pedidos)
                {
                    int resultado = await _unitOfWork.GacelaRepository.GuardarServicio(pedido);
                    totalFilasAfectadas += resultado;
                }

                _unitOfWork.SaveChanges();

                if (totalFilasAfectadas > 0)
                    return Ok(new { mensaje = $"Se guardaron {totalFilasAfectadas} servicios correctamente", filasAfectadas = totalFilasAfectadas });

                return BadRequest("No se pudo guardar ningún servicio. Verifique que los códigos de pedido existan.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPost("ProcessExcel")]
        public async Task<IActionResult> ProcessExcel(IFormFile file, [FromQuery] string tipoGrupo, [FromQuery] string usuario)
        {
            try
            {
                Console.WriteLine("Inicio del método ProcessExcel");

                if (file == null || file.Length == 0)
                    return BadRequest("No se ha proporcionado un archivo válido.");

                if (string.IsNullOrEmpty(tipoGrupo) || string.IsNullOrEmpty(usuario))
                    return BadRequest("Los parámetros tipoGrupo y usuario son requeridos.");

                if (tipoGrupo != "T" && tipoGrupo != "A")
                    return BadRequest("El tipoGrupo debe ser 'T' (Tierra) o 'A' (Aire).");

                // Crear un archivo temporal
                var tempFilePath = Path.GetTempFileName();

                try
                {
                    // Guardar el archivo subido temporalmente
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Procesar el archivo Excel
                    var servicios = await _unitOfWork.GacelaRepository.ProcessExcel(tempFilePath, tipoGrupo, usuario);

                    _unitOfWork.SaveChanges();

                    return Ok(new
                    {
                        mensaje = "Archivo procesado correctamente",
                        serviciosProcesados = servicios.Count,
                        servicios = servicios
                    });
                }
                finally
                {
                    // Eliminar el archivo temporal - usando namespace completo
                    if (System.IO.File.Exists(tempFilePath))
                        System.IO.File.Delete(tempFilePath);
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { mensaje = $"Archivo no encontrado: {ex.Message}" });
            }
            catch (IOException ex)
            {
                return StatusCode(500, new { mensaje = $"Error de E/S: {ex.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completo: {ex}");

                return StatusCode(500, new { mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpPost("RegistrarServicioExterno/{usuario}")]
        public async Task<IActionResult> RegistrarServicioExterno([FromBody] List<GServicio> listaServicios, [FromRoute] string usuario)
        {
            try
            {
                if (listaServicios == null || !listaServicios.Any())
                {
                    return BadRequest("No se enviaron servicios para registrar.");
                }

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest("El usuario es requerido.");
                }

                var resultado = await _unitOfWork.GacelaRepository.RegistrarServicioExterno(listaServicios, usuario);

                _unitOfWork.SaveChanges();

                // Verificar si todos los servicios se registraron correctamente
                var serviciosExitosos = resultado.Where(s => s.Estado == "Servicio Registrado Correctamente").Count();
                var serviciosConError = resultado.Where(s => s.Estado != "Servicio Registrado Correctamente").ToList();

                if (serviciosExitosos > 0)
                {
                    var response = new
                    {
                        mensaje = $"Se procesaron {resultado.Count} servicios",
                        serviciosConError = serviciosConError.Count,
                        detalleServicios = resultado.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            estado = s.Estado,
                            empresa = s.Empresa
                        }),
                        errores = serviciosConError.Any() ? serviciosConError.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            error = s.Estado
                        }) : null
                    };

                    return Ok(response);
                }

                return BadRequest(new
                {
                    mensaje = "No se pudo registrar ningún servicio",
                    errores = serviciosConError.Select(s => new
                    {
                        numero = s.Numero,
                        codigoExterno = s.Codigoexterno,
                        error = s.Estado
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud de registro de servicios.");
            }
        }

        [HttpPut("CancelarServicioExterno/{usuario}")]
        public async Task<IActionResult> CancelarServicioExterno([FromBody] List<GServicio> listaServicios, [FromRoute] string usuario)
        {
            try
            {
                if (listaServicios == null || !listaServicios.Any())
                {
                    return BadRequest("No se enviaron servicios para cancelar.");
                }

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest("El usuario es requerido.");
                }

                var resultado = await _unitOfWork.GacelaRepository.CancelarServicioExterno(listaServicios, usuario);

                _unitOfWork.SaveChanges();

                // Verificar si todos los servicios se cancelaron correctamente
                var serviciosCancelados = resultado.Where(s => s.Estado == "Servicio Cancelado").Count();
                var serviciosConError = resultado.Where(s => s.Estado != "Servicio Cancelado").ToList();

                if (serviciosCancelados > 0)
                {
                    var response = new
                    {
                        mensaje = $"Se procesaron {resultado.Count} servicios",
                        serviciosCancelados = serviciosCancelados,
                        serviciosConError = serviciosConError.Count,
                        detalleServicios = resultado.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            estado = s.Estado,
                            empresa = s.Empresa
                        }),
                        errores = serviciosConError.Any() ? serviciosConError.Select(s => new
                        {
                            numero = s.Numero,
                            codigoExterno = s.Codigoexterno,
                            error = s.Estado
                        }) : null
                    };

                    return Ok(response);
                }

                return BadRequest(new
                {
                    mensaje = "No se pudo cancelar ningún servicio",
                    errores = serviciosConError.Select(s => new
                    {
                        numero = s.Numero,
                        codigoExterno = s.Codigoexterno,
                        error = s.Estado
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud de cancelación de servicios.");
            }
        }

        [HttpPut("CancelarPasajeroExterno/{usuario}")]
        public async Task<IActionResult> CancelarPasajeroExterno([FromBody] GPedido pedido, [FromRoute] string usuario)
        {
            try
            {
                if (pedido == null)
                {
                    return BadRequest("No se envió información del pedido para cancelar.");
                }

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest("El usuario es requerido.");
                }

                if (pedido.Servicio == null || string.IsNullOrWhiteSpace(pedido.Servicio.Codigoexterno))
                {
                    return BadRequest("El código externo del servicio es requerido.");
                }

                if (pedido.Pasajero == null || string.IsNullOrWhiteSpace(pedido.Pasajero.Codlan))
                {
                    return BadRequest("El código del pasajero (codlan) es requerido.");
                }

                var resultado = await _unitOfWork.GacelaRepository.CancelarPasajeroExterno(pedido, usuario);

                _unitOfWork.SaveChanges();

                if (resultado == "Cancelación de pasajero existosa")
                {
                    return Ok(new
                    {
                        mensaje = resultado,
                        codigoExterno = pedido.Servicio.Codigoexterno,
                        codlanPasajero = pedido.Pasajero.Codlan,
                        estado = "Cancelado"
                    });
                }

                return BadRequest(new
                {
                    mensaje = resultado,
                    codigoExterno = pedido.Servicio.Codigoexterno,
                    codlanPasajero = pedido.Pasajero.Codlan
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud de cancelación del pasajero.");
            }
        }

        [HttpPost("AgregarPasajeroExterno/{usuario}")]
        public async Task<IActionResult> AgregarPasajeroExterno([FromBody] GPedido pedido, [FromRoute] string usuario)
        {
            try
            {
                if (pedido == null)
                {
                    return BadRequest("No se envió información del pedido para agregar.");
                }

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest("El usuario es requerido.");
                }

                if (pedido.Servicio == null || string.IsNullOrWhiteSpace(pedido.Servicio.Codigoexterno))
                {
                    return BadRequest("El código externo del servicio es requerido.");
                }

                if (pedido.Pasajero == null || string.IsNullOrWhiteSpace(pedido.Pasajero.Codlan))
                {
                    return BadRequest("Los datos del pasajero son requeridos.");
                }

                if (pedido.Lugar == null || string.IsNullOrWhiteSpace(pedido.Lugar.Direccion))
                {
                    return BadRequest("Los datos de ubicación son requeridos.");
                }

                var resultado = await _unitOfWork.GacelaRepository.AgregarPasajeroExterno(pedido, usuario);

                _unitOfWork.SaveChanges();

                if (resultado == "Se agrego pasajero de forma existosa")
                {
                    return Ok(new
                    {
                        mensaje = resultado,
                        codigoExterno = pedido.Servicio.Codigoexterno,
                        codlanPasajero = pedido.Pasajero.Codlan,
                        nombrePasajero = pedido.Pasajero.Nombre,
                        orden = pedido.Orden,
                        estado = "Agregado"
                    });
                }

                return BadRequest(new
                {
                    mensaje = resultado,
                    codigoExterno = pedido.Servicio.Codigoexterno,
                    codlanPasajero = pedido.Pasajero?.Codlan
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud para agregar el pasajero.");
            }
        }

        
    }
}
