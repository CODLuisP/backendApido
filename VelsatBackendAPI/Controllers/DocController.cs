using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Documentacion;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocController : Controller
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        public DocController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        //DOCUMENTACIÓN
        //----------------------------------UNIDAD--------------------------------------------------//

        [HttpGet("GetByDeviceID")]
        public async Task<IActionResult> GetByDeviceID([FromQuery] string deviceID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceID))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El DeviceID es requerido"
                    });
                }

                var documentos = await _readOnlyUow.DocRepository.GetByDeviceID(deviceID);

                if (documentos == null || !documentos.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontraron documentos para el dispositivo {deviceID}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = documentos,
                    count = documentos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpPost("CreateDocUnidad")]
        public async Task<IActionResult> Create([FromBody] Docunidad docunidad)
        {
            try
            {
                if (docunidad == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Los datos del documento son requeridos"
                    });
                }

                var id = await _uow.DocRepository.Create(docunidad);
                _uow.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Documento creado exitosamente",
                    id = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("DeleteDocUnidad")]
        public async Task<IActionResult> DeleteUnidad([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "ID inválido"
                    });
                }

                var resultado = await _uow.DocRepository.DeleteUnidad(id);
                _uow.SaveChanges();

                if (!resultado)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró el documento con ID {id}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Documento eliminado exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetDocUnidadPorVencer")]
        public async Task<IActionResult> GetDocumentosUnidadProximosVencer([FromQuery] string usuario)
        {
            try
            {
                var documentos = await _readOnlyUow.DocRepository.GetDocumentosUnidadProximosVencer(usuario);

                if (documentos == null || !documentos.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No se encontraron documentos próximos a vencer"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = documentos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        //----------------------------------CONDUCTOR--------------------------------------------------//
        [HttpGet("GetByCodtaxi")]
        public async Task<IActionResult> GetByCodtaxi([FromQuery] int codtaxi)
        {
            try
            {
                if (codtaxi <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El Codtaxi debe ser mayor a 0"
                    });
                }

                var documentos = await _readOnlyUow.DocRepository.GetByCodtaxi(codtaxi);

                if (documentos == null || !documentos.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontraron documentos para el conductor {codtaxi}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = documentos,
                    count = documentos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] Docconductor docconductor)
        {
            try
            {
                if (docconductor == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Los datos del documento son requeridos"
                    });
                }

                var id = await _uow.DocRepository.Create(docconductor);
                _uow.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Documento creado exitosamente",
                    id = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("DeleteConductor")]
        public async Task<IActionResult> Delete([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "ID inválido"
                    });
                }

                var resultado = await _uow.DocRepository.DeleteConductor(id);
                _uow.SaveChanges();

                if (!resultado)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró el documento con ID {id}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Documento eliminado exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetDocCondcutorPorVencer")]
        public async Task<IActionResult> GetDocumentosProximosVencer(string usuario)
        {
            try
            {
                var documentos = await _readOnlyUow.DocRepository.GetDocumentosConductorProximosVencer(usuario);

                if (documentos == null || !documentos.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No se encontraron documentos próximos a vencer"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = documentos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpGet("detalleConductor/{codtaxi}")]
        public async Task<ActionResult<Usuario>> GetDetalleConductor(string codtaxi)
        {
            try
            {
                var conductor = await _readOnlyUow.DocRepository.GetDetalleConductor(codtaxi);

                if (conductor == null)
                {
                    return NotFound("No se encontró el conductor.");
                }

                return Ok(conductor);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
