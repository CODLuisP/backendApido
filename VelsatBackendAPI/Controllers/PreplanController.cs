using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Globalization;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.Latam;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreplanController : Controller
    {

        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        // ✅ CAMBIO: Inyectar Factory en lugar de UnitOfWork
        public PreplanController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        // POST api/preplan/insert
        [HttpPost("insert")]
        public async Task<IActionResult> InsertPedido([FromBody] IEnumerable<ExcelAvianca> excel, [FromQuery] string fecact, [FromQuery] string tipo, [FromQuery] string usuario)
        {
            if (excel == null || !excel.Any())
            {
                return BadRequest("El arreglo Excel está vacío.");
            }

            try
            {
                var result = await _uow.PreplanRepository.InsertPedido(excel, fecact, tipo, usuario);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar pedido", error = ex.Message });
            }

        }


        [HttpGet("get")]
        public async Task<IActionResult> GetPedidos([FromQuery] string dato, [FromQuery] string empresa, [FromQuery] string usuario)
        {
            try
            {
                var pedidos = await _readOnlyUow.PreplanRepository.GetPedidos(dato, empresa, usuario);

                if (pedidos == null || pedidos.Count == 0)
                {
                    return Ok("No se encontraron pedidos.");
                }

                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }

        }

        [HttpPut("save")]
        public async Task<IActionResult> SavePedidos([FromBody] IEnumerable<Pedido> pedidos, [FromQuery] string usuario)
        {
            if (pedidos == null || !pedidos.Any())
            {
                return BadRequest(new { message = "Datos inválidos o lista vacía" });
            }

            try
            {
                int result = await _uow.PreplanRepository.SavePedidos(pedidos, usuario);
                _uow.SaveChanges();

                if (result == 1)
                {
                    return Ok(new { message = "Pedidos guardados correctamente" });
                }
                else
                {
                    return StatusCode(500, new { message = "Error al guardar los pedidos" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar los pedidos", error = ex.Message });
            }

        }

        [HttpPut("delete")]
        public async Task<IActionResult> BorrarPlan([FromQuery] string empresa, [FromQuery] string fecha, [FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(empresa) || string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { message = "Datos inválidos. Se requiere empresa, fecha y usuario." });
            }

            try
            {
                int result = await _uow.PreplanRepository.BorrarPlan(empresa, fecha, usuario);
                _uow.SaveChanges();

                if (result > 0)
                {
                    return Ok(new { message = "Plan borrado correctamente." });
                }
                else
                {
                    return NotFound(new { message = "No se encontró el registro para borrar." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al borrar el plan", error = ex.Message });
            }

        }

        [HttpGet("lugares/{codCliente}")]
        public async Task<IActionResult> GetLugares(string codCliente)
        {
            if (string.IsNullOrEmpty(codCliente))
            {
                return BadRequest(new { message = "Código de cliente es requerido." });
            }
            try
            {
                var lugares = await _readOnlyUow.PreplanRepository.GetLugares(codCliente);

                if (lugares == null || lugares.Count == 0)
                {
                    return NotFound(new { message = "No se encontraron lugares para el cliente." });
                }

                return Ok(lugares);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener lugares", error = ex.Message });
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
                int filasAfectadas = await _uow.PreplanRepository.UpdateDirec(coddire, codigo);
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

        [HttpGet("conductores")]
        public async Task<IActionResult> GetConductores([FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { message = "El usuario es requerido" });
            }
            try
            {
                var conductores = await _readOnlyUow.PreplanRepository.GetConductores(usuario);

                if (conductores == null || conductores.Count == 0)
                {
                    return NotFound(new { message = "No se encontraron conductores para este usuario" });
                }

                return Ok(conductores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener conductores", error = ex.Message });
            }

        }

        [HttpGet("unidades")]
        public async Task<IActionResult> GetUnidades([FromQuery] string usuario)
        {

            try
            {
                var unidades = await _readOnlyUow.PreplanRepository.GetUnidades(usuario);

                if (unidades == null || unidades.Count == 0)
                {
                    return NotFound(new { message = "No se encontraron unidades" });
                }

                return Ok(unidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }

        }

        [HttpPost("servicios")]
        public async Task<IActionResult> CreateServicios([FromQuery] string fecha, [FromQuery] string empresa, [FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(empresa) || string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { message = "Los parámetros fecha, empresa y usuario son requeridos" });
            }
            try
            {
                var servicios = await _uow.PreplanRepository.CreateServicios(fecha, empresa, usuario);
                _uow.SaveChanges();
                return Ok(new { message = "Servicios creados exitosamente", data = servicios });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear los servicios", error = ex.Message });
            }

        }

        [HttpGet("Getservicios")]
        public async Task<IActionResult> GetServicios([FromQuery] string fecha, [FromQuery] string usu)
        {
            if (string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(usu))
            {
                return BadRequest(new { message = "Los parámetros fecha y usuario son requeridos" });
            }
            try
            {
                var servicios = await _readOnlyUow.PreplanRepository.GetServicios(fecha, usu);

                if (servicios == null || servicios.Count == 0)
                {
                    return NotFound(new { message = "No se encontraron servicios." });
                }

                return Ok(servicios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener servicios", error = ex.Message });
            }

        }

        [HttpGet("GetPasajeros")]
        public async Task<IActionResult> GetPasajeros([FromQuery] string palabra, [FromQuery] string codusuario)
        {
            if (string.IsNullOrEmpty(palabra) || string.IsNullOrEmpty(codusuario))
            {
                return BadRequest(new { message = "Los parámetros palabra y codusuario son requeridos" });
            }

            try
            {
                var pasajeros = await _readOnlyUow.PreplanRepository.GetPasajeros(palabra, codusuario);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound(new { message = "No se encontraron pasajeros." });
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener pasajeros", error = ex.Message });
            }

        }
        [HttpGet("GetPasajerosEmpresa")]
        public async Task<IActionResult> GetPasajerosEmpresa([FromQuery] string palabra, [FromQuery] string codusuario, [FromQuery] string empresa)
        {
            if (string.IsNullOrEmpty(palabra) || string.IsNullOrEmpty(codusuario) || string.IsNullOrEmpty(empresa))
            {
                return BadRequest(new { message = "Los parámetros palabra y codusuario son requeridos" });
            }

            try
            {
                var pasajeros = await _readOnlyUow.PreplanRepository.GetPasajerosEmpresa(palabra, codusuario, empresa);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound(new { message = "No se encontraron pasajeros." });
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener pasajeros", error = ex.Message });
            }

        }

        [HttpGet("GetServicioPasajero")]
        public async Task<IActionResult> GetServicioPasajero([FromQuery] string usuario, [FromQuery] string fec, [FromQuery] string codcliente)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(fec) || string.IsNullOrEmpty(codcliente))
            {
                return BadRequest(new { message = "Los parámetros usuario, fec y codcliente son requeridos" });
            }
            try
            {
                var pasajeros = await _readOnlyUow.PreplanRepository.GetServicioPasajero(usuario, fec, codcliente);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound(new { message = "No se encontraron pasajeros." });
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener servicio de pasajeros", error = ex.Message });
            }

        }

        [HttpPost("AsignarServicio")]
        public async Task<IActionResult> AsignarServicio([FromBody] List<Servicio> listaServicios)
        {
            if (listaServicios == null || !listaServicios.Any())
            {
                return BadRequest(new { message = "La lista de servicios no puede estar vacía." });
            }
            try
            {
                string resultado = await _uow.PreplanRepository.AsignacionServicio(listaServicios);
                _uow.SaveChanges();

                if (resultado == "Servicio Asignado")
                {
                    return Ok(new { message = resultado });
                }
                else
                {
                    return BadRequest(new { message = resultado });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en la asignación del servicio", error = ex.Message });
            }

        }

        [HttpDelete("eliminacionmultiple")]
        public async Task<IActionResult> EliminacionMultiple([FromBody] List<Servicio> listaServicios)
        {
            if (listaServicios == null || listaServicios.Count == 0)
            {
                return BadRequest(new { message = "La lista de servicios está vacía o es nula" });
            }

            try
            {
                int resultado = await _uow.PreplanRepository.EliminacionMultiple(listaServicios);
                _uow.SaveChanges();

                if (resultado > 0)
                {
                    return Ok(new { message = "Servicios eliminados correctamente", serviciosEliminados = resultado });
                }

                return BadRequest(new { message = "No se pudieron eliminar los servicios" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar servicios", error = ex.Message });
            }

        }

        //[HttpGet("playback")]
        //public async Task<IActionResult> Playback([FromQuery] string placa, [FromQuery] string fechaini, [FromQuery] string fechafin)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(placa) || string.IsNullOrEmpty(fechaini) || string.IsNullOrEmpty(fechafin))
        //        {
        //            return BadRequest(new { mensaje = "Los parámetros placa, fechaini y fechafin son obligatorios." });
        //        }

        //        // Crear objeto Unidad con la placa
        //        var unidad = new Unidad { Gps = new Gps { Numequipo = placa } };

        //        var resultado = await _unitOfWork.PreplanRepository.PlaybackAsync(unidad, fechaini, fechafin);

        //        return Ok(resultado);
        //    }
        //    catch (FormatException ex)
        //    {
        //        return BadRequest(new { mensaje = "Formato de fecha incorrecto. Debe ser yyyy-MM-dd HH:mm", error = ex.Message });
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(new { mensaje = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        //    }
        //}

        [HttpGet("detalleConductor")]
        public async Task<IActionResult> GetConductorDetalle([FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { mensaje = "El parámetro 'usuario' es obligatorio." });
            }

            try
            {
                var conductores = await _readOnlyUow.PreplanRepository.GetConductorDetalle(usuario);

                if (conductores == null || !conductores.Any())
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }

                return Ok(conductores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener detalle del conductor", error = ex.Message });
            }

        }

        [HttpGet("PasajeroList")]
        public async Task<IActionResult> ListaPasajeroServicio([FromQuery] string codservicio)
        {
            if (string.IsNullOrEmpty(codservicio))
            {
                return BadRequest(new { mensaje = "El parámetro 'codservicio' es obligatorio." });
            }

            try
            {
                var pasajeros = await _readOnlyUow.PreplanRepository.ListaPasajeroServicio(codservicio);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound(new { mensaje = "No se encontraron pasajeros para el servicio." });
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener la lista de pasajeros.", error = ex.Message });
            }

        }


        [HttpPut("actualizarOrden")]
        public async Task<IActionResult> UpdateControlServicio([FromBody] Servicio servicio)
        {
            if (servicio == null)
            {
                return BadRequest("El servicio no puede ser nulo.");
            }
            try
            {
                int resultado = await _uow.PreplanRepository.UpdateControlServicio(servicio);

                // ✅ Solo en PUT (actualización)
                _uow.SaveChanges();

                if (resultado > 0)
                {
                    return Ok(new { mensaje = "Servicio actualizado correctamente.", filasAfectadas = resultado });
                }
                else
                {
                    return NotFound(new { mensaje = "No se encontró el servicio o no se realizaron cambios." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor.", error = ex.Message });
            }

        }


        [HttpPut("canasig/{codservicio}")]
        public async Task<IActionResult> CancelarAsignacion(string codservicio)
        {
            if (string.IsNullOrEmpty(codservicio))
            {
                return BadRequest("El código de servicio es requerido.");
            }

            try
            {
                int filasAfectadas = await _uow.PreplanRepository.CancelarAsignacion(codservicio);

                // ✅ Solo en PUT (actualización)
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Asignación cancelada correctamente." });
                }
                else
                {
                    return NotFound(new { mensaje = "No se encontró el servicio o ya está cancelado." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ocurrió un error al cancelar la asignación.", detalle = ex.Message });
            }

        }


        [HttpDelete("cancelar/{codservicio}")]
        public async Task<IActionResult> CancelarServicio(string codservicio)
        {
            if (string.IsNullOrEmpty(codservicio))
            {
                return BadRequest("El código del servicio es requerido.");
            }

            try
            {
                var servicio = new Servicio { Codservicio = codservicio };

                int resultado = await _uow.PreplanRepository.CancelarServicio(servicio);

                // ✅ Solo en DELETE
                _uow.SaveChanges();

                if (resultado > 0)
                {
                    return Ok(new { message = "Servicio cancelado exitosamente." });
                }
                else
                {
                    return NotFound(new { message = "No se encontró el servicio a cancelar." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cancelar el servicio.", error = ex.Message });
            }

        }


        [HttpPost("AgregarServicio")]
        public async Task<IActionResult> GrabarServicioMovil([FromBody] Servicio servicio, [FromQuery] string usuario)
        {
            if (servicio == null)
            {
                return BadRequest("El servicio no puede ser nulo.");
            }
            try
            {
                string resultado = await _uow.PreplanRepository.NewServicio(servicio, usuario);

                // ✅ Solo en POST (inserción)
                _uow.SaveChanges();

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor.", error = ex.Message });
            }

        }


        [HttpPut("UpdateEstado")]
        public async Task<IActionResult> UpdateEstado([FromBody] Pedido pedido)
        {
            if (pedido == null)
            {
                return BadRequest("El pedido no puede ser nulo.");
            }

            try
            {
                int resultado = await _uow.PreplanRepository.UpdateEstadoServicio(pedido);

                // ✅ Solo en PUT
                _uow.SaveChanges();

                if (resultado > 0)
                {
                    return Ok(new { mensaje = "Estado actualizado correctamente.", filasAfectadas = resultado });
                }

                return BadRequest("No se pudo actualizar el pedido.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor.", error = ex.Message });
            }

        }


        // REPORTE CARLOS GODOY
        [HttpGet("ExcelDiferencias")]
        public async Task<IActionResult> ReporteDiferencia(
            [FromQuery] string fecini,
            [FromQuery] string fecfin,
            [FromQuery] string aerolinea,
            [FromQuery] string usuario,
            [FromQuery] string tipo)
        {
            try
            {
                var feciniDT = DateTime.ParseExact(fecini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                var fecfinDT = DateTime.ParseExact(fecfin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

                // Formatear fechas al estilo requerido: dd/MM/yyyy HH:mm
                string feciniFormateada = feciniDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                string fecfinFormateada = fecfinDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

                var resultado = await _readOnlyUow.PreplanRepository.ReporteDiferencia(
                    feciniFormateada,
                    fecfinFormateada,
                    aerolinea,
                    usuario,
                    tipo
                );

                var excelBytes = await DataExcelSheet(resultado, fecini, fecfin, aerolinea, usuario, tipo);
                string fileName = $"Resumen_{aerolinea}_{fecini}_a_{fecfin}_{tipo}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar el reporte de diferencias.", error = ex.Message });
            }

        }

        private async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetByteArrayAsync(imageUrl);
            }
        }

        private async Task<byte[]> DataExcelSheet(List<Pedido> resultado, string fecini, string fecfin, string aerolinea, string usuario, string tipo)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Servicios");

                // Título principal
                var rangoTitulo = worksheet.Range("B4:I7");
                rangoTitulo.Merge();
                rangoTitulo.Value = "REPORTE DE SERVICIOS DE LA EMPRESA: " + aerolinea.ToUpper();
                rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoTitulo.Style.Font.FontColor = XLColor.White;
                rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoTitulo.Style.Font.FontName = "Calibri";
                rangoTitulo.Style.Font.FontSize = 16;
                rangoTitulo.Style.Font.SetBold();

                // Fecha de Inicio
                worksheet.Range("B9:C9").Merge();
                worksheet.Cell(9, 2).Value = "Fecha de Inicio:";
                worksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                worksheet.Cell(9, 2).Style.Font.FontName = "Calibri";
                worksheet.Cell(9, 2).Style.Font.FontSize = 10;
                worksheet.Cell(9, 2).Style.Font.SetBold();

                // CORREGIDO: Usar CultureInfo.InvariantCulture
                string feciniFormateada = DateTime.Parse(fecini, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                worksheet.Range("D9:E9").Merge();
                worksheet.Cell(9, 4).Value = feciniFormateada;
                worksheet.Cell(9, 4).Style = worksheet.Cell(9, 2).Style;

                // Fecha de Fin
                worksheet.Range("B10:C10").Merge();
                worksheet.Cell(10, 2).Value = "Fecha de Fin:";
                worksheet.Cell(10, 2).Style = worksheet.Cell(9, 2).Style;

                // CORREGIDO: Usar CultureInfo.InvariantCulture
                string fechafinFormateada = DateTime.Parse(fecfin, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                worksheet.Range("D10:E10").Merge();
                worksheet.Cell(10, 4).Value = fechafinFormateada;
                worksheet.Cell(10, 4).Style = worksheet.Cell(9, 4).Style;

                // Fecha y usuario - CORREGIDO: Usar CultureInfo.InvariantCulture
                worksheet.Cell("L9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                worksheet.Cell("L9").Style.Font.FontName = "Calibri";
                worksheet.Cell("L9").Style.Font.FontSize = 10;
                worksheet.Cell("L9").Style.Font.SetBold();
                worksheet.Cell("L9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("L10").Value = "USUARIO : " + usuario.ToUpper();
                worksheet.Cell("L10").Style.Font.FontName = "Calibri";
                worksheet.Cell("L10").Style.Font.FontSize = 10;
                worksheet.Cell("L10").Style.Font.SetBold();
                worksheet.Cell("L10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:L10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:L10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string imageUrl1 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public";
                byte[] imageBytes1 = await DownloadImageAsync(imageUrl1);
                using (var ms1 = new MemoryStream(imageBytes1))
                {
                    var image = worksheet.AddPicture(ms1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);
                }

                var mergedRange = worksheet.Range("J4:L7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string imageUrl2 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public";
                byte[] imageBytes2 = await DownloadImageAsync(imageUrl2);
                using (var ms2 = new MemoryStream(imageBytes2))
                {
                    var image2 = worksheet.AddPicture(ms2).MoveTo(worksheet.Cell("I4"), new System.Drawing.Point(100, 0)).WithSize(240, 80);
                }

                // Cabeceras
                var headers = new[]
                {"ITEM", "FECHA", "CLIENTE", "RECOJO/REPARTO", "N/SERV", "HORA TURNO", "HORA DE INICIO", "HORA LLEGADA ATO", "DIFERENCIA TIEMPO", "NOMBRES", "DIRECCIÓN", "DISTRITO", "PLACA", "CONDUCTOR"};

                worksheet.Row(12).Height = 40;

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(12, i + 2).Value = headers[i];
                    worksheet.Cell(12, i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                    worksheet.Cell(12, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(12, i + 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(12, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(12, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Ancho estimado para cada columna (puedes ajustar)
                worksheet.Column(2).Width = 7;
                worksheet.Column(3).Width = 12;
                worksheet.Column(4).Width = 18;
                worksheet.Column(5).Width = 18;
                worksheet.Column(6).Width = 10;
                worksheet.Column(7).Width = 10;
                worksheet.Column(8).Width = 13;
                worksheet.Column(9).Width = 14;
                worksheet.Column(10).Width = 14;
                worksheet.Column(11).Width = 14;
                worksheet.Column(12).Width = 45;
                worksheet.Column(13).Width = 75;
                worksheet.Column(14).Width = 18;
                worksheet.Column(15).Width = 14;
                worksheet.Column(16).Width = 40;

                worksheet.ShowGridLines = false;

                int fila = 13;
                int item = 1;

                // Variables para controlar el color por grupo de N/SERV
                string numeroServicioAnterior = "";
                bool colorAlternativo = false; // false = gris claro, true = blanco

                // Colores de fondo para alternar por grupo
                var colorGrupo1 = XLColor.FromHtml("#EBF1DE"); // Verde claro
                var colorGrupo2 = XLColor.White; // Blanco

                foreach (var pedido in resultado)
                {
                    var servicio = pedido.Servicio;
                    var pasajero = pedido.Pasajero;
                    var lugar = pedido.Lugar;
                    var unidad = servicio?.Unidad;
                    var conductor = servicio?.Conductor;

                    string numeroServicioActual = servicio?.Numeromovil ?? "";

                    // Verificar si cambió el número de servicio para alternar color
                    if (numeroServicioActual != numeroServicioAnterior)
                    {
                        if (numeroServicioAnterior != "") // No alternar en el primer registro
                        {
                            colorAlternativo = !colorAlternativo;
                        }
                        numeroServicioAnterior = numeroServicioActual;
                    }

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = servicio?.Fecha?.Split(' ')?.FirstOrDefault() ?? "";
                    worksheet.Cell(fila, 4).Value = servicio?.Empresa ?? "";
                    worksheet.Cell(fila, 5).Value = servicio?.Tipo ?? "";
                    worksheet.Cell(fila, 6).Value = numeroServicioActual;

                    // CORREGIDO: Conversión robusta para HORA ATO que maneja múltiples formatos de fecha
                    string horaAto = "";
                    if (!string.IsNullOrEmpty(servicio?.Fecha))
                    {
                        // Lista de formatos posibles para la fecha
                        string[] formatosPosibles = {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm",
                    "dd/MM/yyyy HH:mm:ss",
                    "dd/MM/yyyy HH:mm",
                    "MM/dd/yyyy HH:mm:ss",
                    "MM/dd/yyyy HH:mm",
                    "yyyy/MM/dd HH:mm:ss",
                    "yyyy/MM/dd HH:mm",
                    "dd-MM-yyyy HH:mm:ss",
                    "dd-MM-yyyy HH:mm"
                };

                        DateTime fechaParseada;
                        bool fechaValida = false;

                        // Intentar parsear con cada formato
                        foreach (string formato in formatosPosibles)
                        {
                            if (DateTime.TryParseExact(servicio.Fecha, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaParseada))
                            {
                                horaAto = fechaParseada.ToString("HH:mm", CultureInfo.InvariantCulture);
                                fechaValida = true;
                                break;
                            }
                        }

                        // Si ningún formato específico funciona, intentar parseo general
                        if (!fechaValida && DateTime.TryParse(servicio.Fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaParseada))
                        {
                            horaAto = fechaParseada.ToString("HH:mm", CultureInfo.InvariantCulture);
                            fechaValida = true;
                        }

                        // Log para debug (puedes remover en producción)
                        if (!fechaValida)
                        {
                            Console.WriteLine($"[WARNING] No se pudo parsear la fecha: '{servicio.Fecha}' para el servicio");
                        }
                    }
                    string horaInicio = pedido.Formathorarec ?? "";
                    string horaLlegadaAto = servicio?.Formathorarec ?? "";

                    worksheet.Cell(fila, 7).Value = horaAto;
                    worksheet.Cell(fila, 8).Value = horaInicio;
                    worksheet.Cell(fila, 9).Value = horaLlegadaAto;

                    // NUEVO: Calcular diferencias de tiempo
                    // Columna 10: Diferencia entre columna 9 (Hora Llegada ATO) y columna 7 (Hora ATO)
                    string diferenciaTiempo = CalcularDiferenciaTiempo(horaLlegadaAto, horaAto);
                    worksheet.Cell(fila, 10).Value = diferenciaTiempo;

                    // Columna 11: Diferencia entre columna 8 (Hora de Inicio) y columna 7 (Hora ATO)
                    string tiempoProgramado = CalcularDiferenciaTiempo(horaInicio, horaAto);
                    worksheet.Cell(fila, 11).Value = tiempoProgramado;

                    worksheet.Cell(fila, 12).Value = pasajero?.Nombre ?? "";
                    worksheet.Cell(fila, 13).Value = lugar?.Direccion ?? "";
                    worksheet.Cell(fila, 14).Value = lugar?.Distrito ?? "";
                    worksheet.Cell(fila, 15).Value = unidad?.Codunidad ?? "";
                    worksheet.Cell(fila, 16).Value = conductor?.Apepate ?? "";

                    // Aplicar estilos de centrado y color por grupo de N/SERV
                    int colInicio = 2; // Columna B
                    int colFin = 16;   // Última columna según tu estructura

                    var rango = worksheet.Range(fila, colInicio, fila, colFin);

                    // Centrado horizontal y vertical
                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Color de fondo según el grupo de N/SERV
                    var colorFondo = colorAlternativo ? colorGrupo2 : colorGrupo1;
                    rango.Style.Fill.BackgroundColor = colorFondo;

                    // Color especial para la columna 9 (HORA LLEGADA ATO)
                    worksheet.Cell(fila, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe246");

                    // NUEVO: Aplicar color rojo a valores negativos en las columnas calculadas
                    // Columna 10 - DIFERENCIA TIEMPO
                    if (!string.IsNullOrEmpty(diferenciaTiempo) && diferenciaTiempo.StartsWith("-"))
                    {
                        worksheet.Cell(fila, 10).Style.Font.FontColor = XLColor.Red;
                    }

                    // Columna 11 - TIEMPO PROGRAMADO  
                    if (!string.IsNullOrEmpty(tiempoProgramado) && tiempoProgramado.StartsWith("-"))
                    {
                        worksheet.Cell(fila, 11).Style.Font.FontColor = XLColor.Red;
                    }

                    fila++;
                }

                // Aplicar wrap text desde la fila 12 en adelante (columnas 7-16)
                // Calcular la última fila con datos
                int ultimaFila = fila - 1;
                if (ultimaFila >= 12)
                {
                    var rangoWrapText = worksheet.Range(12, 7, ultimaFila, 16);
                    rangoWrapText.Style.Alignment.WrapText = true;
                }

                // NUEVO: Aplicar bordes (líneas de cuadrícula) solo al área de datos
                if (ultimaFila >= 13) // Solo si hay datos
                {
                    // Definir el rango completo de datos (desde B13 hasta la última columna y fila con datos)
                    var rangoConBordes = worksheet.Range(13, 2, ultimaFila, 16); // B13 hasta P(última fila)

                    // Aplicar bordes a todas las celdas del rango
                    rangoConBordes.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    // Color de los bordes (gris claro)
                    rangoConBordes.Style.Border.TopBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.BottomBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.LeftBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.RightBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.InsideBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.OutsideBorderColor = XLColor.Gray;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private string CalcularDiferenciaTiempo(string horaFinal, string horaInicial)
        {
            if (string.IsNullOrEmpty(horaFinal) || string.IsNullOrEmpty(horaInicial))
            {
                return "";
            }

            try
            {
                // Parsear las horas (formato esperado HH:mm en 24 horas)
                if (!TimeSpan.TryParseExact(horaFinal, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan tsFinal))
                {
                    return "";
                }

                if (!TimeSpan.TryParseExact(horaInicial, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan tsInicial))
                {
                    return "";
                }

                // Calcular la diferencia considerando el cruce de medianoche
                TimeSpan diferencia = tsFinal - tsInicial;

                // Si la diferencia es muy negativa (más de 12 horas), probablemente cruzó medianoche
                if (diferencia.TotalHours < -12)
                {
                    diferencia = (tsFinal + TimeSpan.FromDays(1)) - tsInicial;
                }
                // Si la diferencia es muy positiva (más de 12 horas), el orden puede estar invertido
                else if (diferencia.TotalHours > 12)
                {
                    diferencia = tsFinal - (tsInicial + TimeSpan.FromDays(1));
                }

                // Si es 00:00, devolver sin signos
                if (diferencia == TimeSpan.Zero)
                {
                    return "00:00";
                }

                // Lógica invertida: si es negativo no lleva signo, si es positivo lleva signo menos
                if (diferencia < TimeSpan.Zero)
                {
                    return diferencia.Negate().ToString(@"hh\:mm");
                }

                return "-" + diferencia.ToString(@"hh\:mm");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al calcular diferencia de tiempo entre '{horaFinal}' y '{horaInicial}': {ex.Message}");
                return "";
            }
        }


        //[HttpGet("ReporteDiferencia")]
        //public async Task<IActionResult> ReporteDiferencia([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea, [FromQuery] string usuario, [FromQuery] string tipo)
        //{
        //    try
        //    {
        //        fecini = DateTime.Parse(fecini).ToString("dd/MM/yyyy") + " 00:00";
        //        fecfin = DateTime.Parse(fecfin).ToString("dd/MM/yyyy") + " 23:59";

        //        var resultado = await _unitOfWork.PreplanRepository.ReporteDiferencia(fecini, fecfin, aerolinea, usuario, tipo);

        //        if (resultado == null || resultado.Count == 0)
        //        {
        //            return NotFound("No se encontraron pedidos.");
        //        }

        //        return Ok(resultado);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Manejo de errores
        //        return StatusCode(500, $"Error interno: {ex.Message}");
        //    }
        //}


        // REPORTE EXCEL - Control Servicios
        [HttpGet("ServiciosExcel")]
        public async Task<IActionResult> ResumenServiciosExcel([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea, [FromQuery] string usuario)
        {
            try
            {
                var feciniDT = DateTime.ParseExact(fecini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                var fecfinDT = DateTime.ParseExact(fecfin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

                // Formatear fechas al estilo requerido: dd/MM/yyyy HH:mm
                string feciniFormateada = feciniDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                string fecfinFormateada = fecfinDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

                // Llamada al repositorio
                var resultado = await _readOnlyUow.PreplanRepository.ReporteFormatoAvianca(
                    feciniFormateada,
                    fecfinFormateada,
                    aerolinea,
                    usuario
                );

                // Generar archivo Excel
                var excelBytes = await ConvertDataExcel(resultado, fecini, fecfin, aerolinea, usuario);
                string fileName = $"Resumen_{aerolinea}_{fecini}_a_{fecfin}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar el reporte de servicios.", error = ex.Message });
            }

        }

        private async Task<byte[]> ConvertDataExcel(List<Pedido> resultado, string fecini, string fecfin, string aerolinea, string usuario)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Servicios");

                // Título principal
                var rangoTitulo = worksheet.Range("B4:I7");
                rangoTitulo.Merge();
                rangoTitulo.Value = "REPORTE DE SERVICIOS DE LA EMPRESA: " + aerolinea.ToUpper();
                rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoTitulo.Style.Font.FontColor = XLColor.White;
                rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoTitulo.Style.Font.FontName = "Calibri";
                rangoTitulo.Style.Font.FontSize = 16;
                rangoTitulo.Style.Font.SetBold();

                // Fecha de Inicio
                worksheet.Range("B9:C9").Merge();
                worksheet.Cell(9, 2).Value = "Fecha de Inicio:";
                worksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                worksheet.Cell(9, 2).Style.Font.FontName = "Calibri";
                worksheet.Cell(9, 2).Style.Font.FontSize = 10;
                worksheet.Cell(9, 2).Style.Font.SetBold();

                // CORREGIDO: Usar CultureInfo.InvariantCulture
                string feciniFormateada = DateTime.Parse(fecini, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                worksheet.Range("D9:E9").Merge();
                worksheet.Cell(9, 4).Value = feciniFormateada;
                worksheet.Cell(9, 4).Style = worksheet.Cell(9, 2).Style;

                // Fecha de Fin
                worksheet.Range("B10:C10").Merge();
                worksheet.Cell(10, 2).Value = "Fecha de Fin:";
                worksheet.Cell(10, 2).Style = worksheet.Cell(9, 2).Style;

                // CORREGIDO: Usar CultureInfo.InvariantCulture
                string fechafinFormateada = DateTime.Parse(fecfin, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                worksheet.Range("D10:E10").Merge();
                worksheet.Cell(10, 4).Value = fechafinFormateada;
                worksheet.Cell(10, 4).Style = worksheet.Cell(9, 4).Style;

                // Fecha y usuario - CORREGIDO: Usar CultureInfo.InvariantCulture
                worksheet.Cell("J9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                worksheet.Cell("J9").Style.Font.FontName = "Calibri";
                worksheet.Cell("J9").Style.Font.FontSize = 10;
                worksheet.Cell("J9").Style.Font.SetBold();
                worksheet.Cell("J9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("J10").Value = "USUARIO : " + usuario.ToUpper();
                worksheet.Cell("J10").Style.Font.FontName = "Calibri";
                worksheet.Cell("J10").Style.Font.FontSize = 10;
                worksheet.Cell("J10").Style.Font.SetBold();
                worksheet.Cell("J10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:J10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:J10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string imageUrl1 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public";
                byte[] imageBytes1 = await DownloadImageAsync(imageUrl1);
                using (var ms1 = new MemoryStream(imageBytes1))
                {
                    var image = worksheet.AddPicture(ms1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);
                }

                var mergedRange = worksheet.Range("J4:K7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string imageUrl2 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public";
                byte[] imageBytes2 = await DownloadImageAsync(imageUrl2);
                using (var ms2 = new MemoryStream(imageBytes2))
                {
                    var image2 = worksheet.AddPicture(ms2).MoveTo(worksheet.Cell("I4"), new System.Drawing.Point(100, 0)).WithSize(240, 80);
                }

                // Cabeceras
                var headers = new[]
                {"ITEM", "FECHA", "CLIENTE", "TIPO SERVICIO", "RECOJO/REPARTO", "N/CODIGO", "VUELO", "HORA ATO", "HORA CONDUCTOR", "HORA GEOCERCA", "HORA PROG. ATEN.", "HORA REAL ATEN.", "ORDEN", "CÓDIGO", "NOMBRES", "TELÉFONO", "DIRECCIÓN", "DISTRITO", "PLACA", "CONDUCTOR", "ESTADO SERVICIO", "PRECIO SERVICIO", "OBSERVACIONES"};

                worksheet.Row(12).Height = 40;

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(12, i + 2).Value = headers[i];
                    worksheet.Cell(12, i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                    worksheet.Cell(12, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(12, i + 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(12, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(12, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Ancho estimado para cada columna (puedes ajustar)

                worksheet.Column(2).Width = 8;
                worksheet.Column(3).Width = 15;
                worksheet.Column(4).Width = 10;
                worksheet.Column(5).Width = 16;
                worksheet.Column(6).Width = 18;
                worksheet.Column(7).Width = 13;
                worksheet.Column(8).Width = 7;
                worksheet.Column(9).Width = 12;
                worksheet.Column(10).Width = 19;
                worksheet.Column(11).Width = 19;
                worksheet.Column(12).Width = 19;
                worksheet.Column(13).Width = 16;
                worksheet.Column(14).Width = 9;
                worksheet.Column(15).Width = 14;
                worksheet.Column(16).Width = 40;
                worksheet.Column(17).Width = 13;
                worksheet.Column(18).Width = 90;
                worksheet.Column(19).Width = 24;
                worksheet.Column(20).Width = 16;
                worksheet.Column(21).Width = 35;
                worksheet.Column(22).Width = 17;
                worksheet.Column(23).Width = 17;
                worksheet.Column(24).Width = 20;

                worksheet.Column(18).Style.Alignment.WrapText = true;
                worksheet.ShowGridLines = false;

                int fila = 13;
                int item = 1;

                foreach (var pedido in resultado)
                {
                    var servicio = pedido.Servicio;
                    var pasajero = pedido.Pasajero;
                    var lugar = pedido.Lugar;
                    var unidad = servicio?.Unidad;
                    var conductor = servicio?.Conductor;
                    var zona = servicio?.Zona;

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = servicio?.Fecha?.Split(' ')?.FirstOrDefault() ?? "";
                    worksheet.Cell(fila, 4).Value = servicio?.Empresa ?? "";
                    worksheet.Cell(fila, 5).Value = "Tierra";
                    worksheet.Cell(fila, 6).Value = servicio?.Tipo ?? "";
                    worksheet.Cell(fila, 7).Value = servicio?.Numeromovil ?? "";
                    worksheet.Cell(fila, 8).Value = pedido.Vuelo ?? "";

                    // CORREGIDO: Conversión robusta para HORA ATO que maneja múltiples formatos de fecha
                    string horaAto = "";
                    if (!string.IsNullOrEmpty(servicio?.Fecha))
                    {
                        // Lista de formatos posibles para la fecha
                        string[] formatosPosibles = {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm",
                    "dd/MM/yyyy HH:mm:ss",
                    "dd/MM/yyyy HH:mm",
                    "MM/dd/yyyy HH:mm:ss",
                    "MM/dd/yyyy HH:mm",
                    "yyyy/MM/dd HH:mm:ss",
                    "yyyy/MM/dd HH:mm",
                    "dd-MM-yyyy HH:mm:ss",
                    "dd-MM-yyyy HH:mm"
                };

                        DateTime fechaParseada;
                        bool fechaValida = false;

                        // Intentar parsear con cada formato
                        foreach (string formato in formatosPosibles)
                        {
                            if (DateTime.TryParseExact(servicio.Fecha, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaParseada))
                            {
                                horaAto = fechaParseada.ToString("HH:mm", CultureInfo.InvariantCulture);
                                fechaValida = true;
                                break;
                            }
                        }

                        // Si ningún formato específico funciona, intentar parseo general
                        if (!fechaValida && DateTime.TryParse(servicio.Fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaParseada))
                        {
                            horaAto = fechaParseada.ToString("HH:mm", CultureInfo.InvariantCulture);
                            fechaValida = true;
                        }

                        // Log para debug (puedes remover en producción)
                        if (!fechaValida)
                        {
                            Console.WriteLine($"[WARNING] No se pudo parsear la fecha: '{servicio.Fecha}' para el servicio");
                        }
                    }
                    worksheet.Cell(fila, 9).Value = horaAto;

                    worksheet.Cell(fila, 10).Value = servicio?.Formathorarec ?? "";
                    worksheet.Cell(fila, 11).Value = servicio?.Gps?.Fecha ?? "";
                    worksheet.Cell(fila, 12).Value = pedido.Formathorarec ?? "";
                    worksheet.Cell(fila, 13).Value = "";
                    worksheet.Cell(fila, 14).Value = pedido.Orden ?? "";
                    worksheet.Cell(fila, 15).Value = pasajero?.Codlan ?? "";
                    worksheet.Cell(fila, 16).Value = pasajero?.Nombre ?? "";
                    worksheet.Cell(fila, 17).Value = pasajero?.Telefono ?? "";
                    worksheet.Cell(fila, 18).Value = lugar?.Direccion ?? "";
                    worksheet.Cell(fila, 19).Value = lugar?.Distrito ?? "";
                    worksheet.Cell(fila, 20).Value = unidad?.Codunidad ?? "";
                    worksheet.Cell(fila, 21).Value = conductor?.Apepate ?? "";
                    worksheet.Cell(fila, 22).Value = servicio?.Estado ?? "";
                    worksheet.Cell(fila, 23).Value = zona?.Precio?.ToString() ?? "";
                    worksheet.Cell(fila, 24).Value = "";

                    fila++;
                }

                // Aplicar estilos de centrado y color intercalado desde fila 13 hacia abajo
                int colInicio = 2; // Columna B
                int colFin = 24;   // Última columna según tu estructura

                for (int i = 13; i < fila; i++)
                {
                    var rango = worksheet.Range(i, colInicio, i, colFin);

                    // Centrado horizontal y vertical
                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Color intercalado
                    if ((i - 13) % 2 == 0)
                    {
                        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#f2f2f2");
                    }
                    else
                    {
                        rango.Style.Fill.BackgroundColor = XLColor.White; // Blanco
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        //1ER REPORTE PARA CLIENTE AREMYS

        [HttpGet("Aremys")]
        public async Task<IActionResult> ReporteFormatoAremys([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea)
        {
            try
            {
                // Convertir las fechas al formato requerido: dd/MM/yyyy HH:mm
                fecini = DateTime.Parse(fecini).ToString("dd/MM/yyyy") + " 00:00";
                fecfin = DateTime.Parse(fecfin).ToString("dd/MM/yyyy") + " 23:59";

                // Llamada al repositorio
                var resultado = await _readOnlyUow.PreplanRepository.ReporteFormatoAremys(fecini, fecfin, aerolinea);

                if (resultado == null || resultado.Count == 0)
                {
                    return NotFound("No se encontraron pedidos.");
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { mensaje = "Error interno al generar el reporte Aremys.", error = ex.Message });
            }

        }


        [HttpGet("ResumenExcelAremys")]
        public async Task<IActionResult> ResumenExcelAremys([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea)
        {
            try
            {
                // Parsear las fechas recibidas (formato ISO)
                var feciniDT = DateTime.ParseExact(fecini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                var fecfinDT = DateTime.ParseExact(fecfin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

                // Formatear al estilo requerido: dd/MM/yyyy HH:mm
                string feciniFormateada = feciniDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                string fecfinFormateada = fecfinDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

                // Obtener los datos desde el repositorio
                var resultado = await _readOnlyUow.PreplanRepository.ReporteFormatoAremys(feciniFormateada, fecfinFormateada, aerolinea);

                if (resultado == null || resultado.Count == 0)
                {
                    return NotFound("No se encontraron registros para exportar.");
                }

                // Generar el archivo Excel
                var excelBytes = await ConvertDataExcelAremys(resultado, fecini, fecfin, aerolinea);
                string fileName = $"Resumen_{aerolinea}_{fecini}_a_{fecfin}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al generar el reporte Excel de Aremys.",
                    error = ex.Message
                });
            }

        }

        private async Task<byte[]> ConvertDataExcelAremys(List<Pedido> resultado, string fecini, string fecfin, string aerolinea)
        {
            using (var workbook = new XLWorkbook())
            {
                var usuario = "AREMYS";
                var worksheet = workbook.Worksheets.Add("Servicios");

                // Título principal
                var rangoTitulo = worksheet.Range("B4:K7");
                rangoTitulo.Merge();
                rangoTitulo.Value = "REPORTE DE SERVICIOS DE LA EMPRESA: " + aerolinea.ToUpper();
                rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoTitulo.Style.Font.FontColor = XLColor.White;
                rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoTitulo.Style.Font.FontName = "Calibri";
                rangoTitulo.Style.Font.FontSize = 16;
                rangoTitulo.Style.Font.SetBold();

                // Fecha de Inicio
                worksheet.Range("B9:C9").Merge();
                worksheet.Cell(9, 2).Value = "Fecha de Inicio:";
                worksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                worksheet.Cell(9, 2).Style.Font.FontName = "Calibri";
                worksheet.Cell(9, 2).Style.Font.FontSize = 10;
                worksheet.Cell(9, 2).Style.Font.SetBold();

                string feciniFormateada = DateTime.Parse(fecini).ToString("dd/MM/yyyy HH:mm");
                worksheet.Range("D9:F9").Merge();
                worksheet.Cell(9, 4).Value = feciniFormateada;
                worksheet.Cell(9, 4).Style = worksheet.Cell(9, 2).Style;

                // Fecha de Fin
                worksheet.Range("B10:C10").Merge();
                worksheet.Cell(10, 2).Value = "Fecha de Fin:";
                worksheet.Cell(10, 2).Style = worksheet.Cell(9, 2).Style;

                string fechafinFormateada = DateTime.Parse(fecfin).ToString("dd/MM/yyyy HH:mm");
                worksheet.Range("D10:F10").Merge();
                worksheet.Cell(10, 4).Value = fechafinFormateada;
                worksheet.Cell(10, 4).Style = worksheet.Cell(9, 4).Style;

                // Fecha y usuario
                worksheet.Cell("K9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cell("K9").Style.Font.FontName = "Calibri";
                worksheet.Cell("K9").Style.Font.FontSize = 10;
                worksheet.Cell("K9").Style.Font.SetBold();
                worksheet.Cell("K9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("K10").Value = "USUARIO : " + usuario;
                worksheet.Cell("K10").Style.Font.FontName = "Calibri";
                worksheet.Cell("K10").Style.Font.FontSize = 10;
                worksheet.Cell("K10").Style.Font.SetBold();
                worksheet.Cell("K10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:K10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:K10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string imageUrl1 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public";
                byte[] imageBytes1 = await DownloadImageAsync(imageUrl1);
                using (var ms1 = new MemoryStream(imageBytes1))
                {
                    var image = worksheet.AddPicture(ms1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);
                }

                var mergedRange = worksheet.Range("L4:M7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string imageUrl2 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public";
                byte[] imageBytes2 = await DownloadImageAsync(imageUrl2);
                using (var ms2 = new MemoryStream(imageBytes2))
                {
                    var image2 = worksheet.AddPicture(ms2).MoveTo(worksheet.Cell("I4"), new System.Drawing.Point(100, 0)).WithSize(240, 80);
                }

                // Cabeceras
                var headers = new[]
                {"ITEM", "CLIENTE", "FECHA", "", "DETALLE", "", "", "HORA PROGRAMADA", "HORA LLEGADA", "HORA INICIO", "HORA PV7 O LIMA HUB", "HORA SKY", "ORDEN", "CÓDIGO", "NOMBRES", "TELÉFONO", "DIRECCIÓN", "DISTRITO", "PLACA", "CONDUCTOR"};

                worksheet.Row(12).Height = 40;

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(12, i + 2).Value = headers[i];
                    worksheet.Cell(12, i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                    worksheet.Cell(12, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(12, i + 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(12, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(12, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Ancho estimado para cada columna (puedes ajustar)

                worksheet.Column(2).Width = 8;
                worksheet.Column(3).Width = 15;
                worksheet.Column(4).Width = 10;
                worksheet.Column(5).Hide(); // Oculta columna E (índice 5)
                worksheet.Column(6).Width = 10;
                worksheet.Column(7).Hide(); // Oculta columna G (índice 7)
                worksheet.Column(8).Hide(); // Oculta columna H (índice 8)
                worksheet.Column(9).Width = 20;
                worksheet.Column(10).Width = 18;
                worksheet.Column(11).Width = 19;
                worksheet.Column(12).Width = 22;
                worksheet.Column(13).Width = 16;
                worksheet.Column(14).Width = 9;
                worksheet.Column(15).Width = 14;
                worksheet.Column(16).Width = 40;
                worksheet.Column(17).Width = 13;
                worksheet.Column(18).Width = 90;
                worksheet.Column(19).Width = 24;
                worksheet.Column(20).Width = 16;
                worksheet.Column(21).Width = 40;
                worksheet.Column(22).Hide(); // Oculta columna V (índice 7)
                worksheet.Column(23).Hide(); // Oculta columna W (índice 8)
                worksheet.Column(24).Hide(); // Oculta columna X (índice 7)

                worksheet.Column(18).Style.Alignment.WrapText = true;
                worksheet.ShowGridLines = false;

                int fila = 13;
                int item = 1;

                foreach (var pedido in resultado)
                {
                    var servicio = pedido.Servicio;
                    var pasajero = pedido.Pasajero;
                    var lugar = pedido.Lugar;
                    var unidad = servicio?.Unidad;
                    var conductor = servicio?.Conductor;

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = servicio?.Empresa ?? "";
                    worksheet.Cell(fila, 4).Value = servicio?.Fecha?.Split(' ')?.FirstOrDefault() ?? "";
                    worksheet.Cell(fila, 6).Value = servicio?.Tipo ?? "";

                    worksheet.Cell(fila, 9).Value = pedido.Fecplan;
                    worksheet.Cell(fila, 10).Value = servicio?.Fecha ?? "";
                    worksheet.Cell(fila, 11).Value = pedido.Fecaten ?? "";
                    worksheet.Cell(fila, 12).Value = servicio?.Fecfin ?? "";
                    worksheet.Cell(fila, 13).Value = pedido.Fecha ?? "";
                    worksheet.Cell(fila, 14).Value = pedido.Orden ?? "";
                    worksheet.Cell(fila, 15).Value = pasajero?.Codlan ?? "";
                    worksheet.Cell(fila, 16).Value = pasajero?.Nombre ?? "";
                    worksheet.Cell(fila, 17).Value = pasajero?.Telefono ?? "";
                    worksheet.Cell(fila, 18).Value = lugar?.Direccion ?? "";
                    worksheet.Cell(fila, 19).Value = lugar?.Distrito ?? "";
                    worksheet.Cell(fila, 20).Value = unidad?.Codunidad ?? "";
                    worksheet.Cell(fila, 21).Value = conductor?.Apepate ?? "";

                    fila++;
                }

                // Aplicar estilos de centrado y color intercalado desde fila 13 hacia abajo
                int colInicio = 2; // Columna B
                int colFin = 24;   // Última columna según tu estructura

                for (int i = 13; i < fila; i++)
                {
                    var rango = worksheet.Range(i, colInicio, i, colFin);

                    // Centrado horizontal y vertical
                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Color intercalado
                    if ((i - 13) % 2 == 0)
                    {
                        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#f2f2f2");
                    }
                    else
                    {
                        rango.Style.Fill.BackgroundColor = XLColor.White; // Blanco
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        [HttpPost("AgregarPasajero")]
        public async Task<IActionResult> RegistrarPasajeroGrupo([FromBody] Pedido pedido, [FromQuery] string usuario)
        {
            if (pedido == null)
                return BadRequest(new { mensaje = "El pedido no puede ser nulo." });

            try
            {
                int result = await _uow.PreplanRepository.RegistrarPasajeroGrupo(pedido, usuario);

                _uow.SaveChanges(); // ✅ Solo en POST, PUT, DELETE

                if (result > 0)
                    return Ok(new { mensaje = "Pasajero registrado correctamente", filasAfectadas = result });

                return BadRequest(new { mensaje = "No se pudo registrar el pasajero." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno al registrar el pasajero.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPut("UpdateHoras")]
        public async Task<IActionResult> UpdateHorasServicio([FromQuery] string codservicio, [FromQuery] string fecha, [FromQuery] string fecplan)
        {
            if (string.IsNullOrEmpty(codservicio) || string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(fecplan))
                return BadRequest("Todos los parámetros son requeridos.");

            try
            {
                int result = await _uow.PreplanRepository.UpdateHorasServicio(codservicio, fecha, fecplan);

                _uow.SaveChanges(); // ✅ Solo porque es un PUT (operación de escritura)

                if (result > 0)
                    return Ok(new { mensaje = "Servicio actualizado correctamente.", filasAfectadas = result });

                return NotFound(new { mensaje = "No se encontró el servicio con el código proporcionado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al actualizar el servicio.",
                    detalle = ex.Message
                });
            }

        }

        [HttpPut("UpdateDestino")]
        public async Task<IActionResult> UpdateDestinoServicio([FromQuery] string codservicio, [FromQuery] string newcoddestino, [FromQuery] string newcodubicli)
        {
            if (string.IsNullOrEmpty(codservicio) ||
                string.IsNullOrEmpty(newcoddestino) ||
                string.IsNullOrEmpty(newcodubicli))
            {
                return BadRequest(new { mensaje = "Todos los parámetros son requeridos." });
            }

            try
            {
                int result = await _uow.PreplanRepository.UpdateDestinoServicio(codservicio, newcoddestino, newcodubicli);

                _uow.SaveChanges(); // ✅ Solo porque es un PUT (operación de escritura)

                if (result > 0)
                    return Ok(new { mensaje = "Servicio actualizado correctamente.", filasAfectadas = result });

                return NotFound(new { mensaje = "No se encontró el servicio con el código proporcionado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al actualizar el destino del servicio.",
                    detalle = ex.Message
                });
            }

        }

        [HttpGet("GetDestinos")]
        public async Task<IActionResult> GetDestinos([FromQuery] string palabra)
        {
            try
            {
                var destinos = await _readOnlyUow.PreplanRepository.GetDestinos(palabra);

                if (destinos == null || !destinos.Any())
                {
                    return NotFound(new { mensaje = "No se encontraron destinos." });
                }

                return Ok(destinos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al obtener los destinos.",
                    detalle = ex.Message
                });
            }

        }

        [HttpPut("GrupoCero")]
        public async Task<IActionResult> EliminarGrupoCero([FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(usuario))
                return BadRequest(new { mensaje = "El parámetro 'usuario' es requerido." });

            try
            {
                int result = await _uow.PreplanRepository.EliminarGrupoCero(usuario);

                _uow.SaveChanges(); // ✅ Solo porque es un PUT

                if (result > 0)
                    return Ok(new { mensaje = "Grupo eliminado correctamente.", filasAfectadas = result });

                return NotFound(new { mensaje = "No se encontraron registros para eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al eliminar el grupo.",
                    detalle = ex.Message
                });
            }

        }

        [HttpGet("conductores/{usuario}")]
        public async Task<ActionResult<List<Conductor>>> GetConductoresByUsuario(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return BadRequest(new { mensaje = "El parámetro 'usuario' es requerido." });
            }

            try
            {
                var conductores = await _readOnlyUow.PreplanRepository.GetConductoresxUsuario(usuario);

                if (conductores == null || !conductores.Any())
                {
                    return NotFound(new { mensaje = "No se encontraron conductores para el usuario especificado." });
                }

                return Ok(conductores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }
        }

        [HttpGet("carros/{usuario}")]
        public async Task<ActionResult<List<Carro>>> GetUnidadesxUsuario(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return BadRequest(new { mensaje = "El parámetro 'usuario' es requerido." });
            }

            try
            {
                var carros = await _readOnlyUow.PreplanRepository.GetUnidadesxUsuario(usuario);

                if (carros == null || !carros.Any())
                {
                    return NotFound(new { mensaje = "No se encontraron carros para el usuario especificado." });
                }

                return Ok(carros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }

        [HttpPost("NuevoConductor/{usuario}")]
        public async Task<IActionResult> GuardarConductor([FromBody] Conductor conductor, [FromRoute] string usuario)
        {
            if (conductor == null)
            {
                return BadRequest(new { message = "El conductor no puede ser nulo" });
            }

            if (string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { message = "El usuario es requerido" });
            }

            try
            {
                var resultado = await _uow.PreplanRepository.GuardarConductorAsync(conductor, usuario);
                _uow.SaveChanges();

                return resultado switch
                {
                    0 => BadRequest(new { message = "Error al guardar conductor" }),
                    1 => Ok(new { message = "Conductor guardado exitosamente" }),
                    2 => Conflict(new { message = "El conductor ya existe" }),
                    _ => StatusCode(500, new { message = "Error interno" })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar conductor", error = ex.Message });
            }

        }

        [HttpPut("ModificarConductor/{id}")]
        public async Task<IActionResult> ModificarConductor(int id, [FromBody] Conductor conductor)
        {
            if (conductor == null)
                return BadRequest(new { mensaje = "El objeto 'conductor' no puede ser nulo." });

            try
            {
                var resultado = await _uow.PreplanRepository.ModificarConductorAsync(conductor);

                _uow.SaveChanges(); // ✅ Solo porque es un PUT (operación de escritura)

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Conductor no encontrado o no se pudo modificar.", code = 0 }),
                    1 => Ok(new { mensaje = "Conductor modificado exitosamente.", code = 1 }),
                    _ => Ok(new { mensaje = $"{resultado} registros modificados", code = resultado })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPost("HabilitarCond/{id}")]
        public async Task<IActionResult> HabilitarConductor(int id)
        {

            if (id <= 0)
                return BadRequest(new { mensaje = "Código de conductor inválido." });

            try
            {
                var resultado = await _uow.PreplanRepository.HabilitarConductorAsync(id);

                _uow.SaveChanges(); // ✅ Solo porque es un POST (operación de escritura)

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Conductor no encontrado.", code = 0 }),
                    1 => Ok(new { mensaje = "Conductor habilitado exitosamente.", code = 1 }),
                    _ => StatusCode(500, new { mensaje = "Error interno al habilitar el conductor.", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPost("DeshabilitarCond/{id}")]
        public async Task<IActionResult> DeshabilitarConductor(int id)
        {
            if (id <= 0)
                return BadRequest(new { mensaje = "Código de conductor inválido." });

            try
            {
                var resultado = await _uow.PreplanRepository.DeshabilitarConductorAsync(id);

                _uow.SaveChanges(); // ✅ Solo en operaciones POST/PUT/DELETE

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Conductor no encontrado.", code = 0 }),
                    1 => Ok(new { mensaje = "Conductor deshabilitado exitosamente.", code = 1 }),
                    _ => StatusCode(500, new { mensaje = "Error interno al deshabilitar el conductor.", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPost("Liberar/{id}")]
        public async Task<IActionResult> LiberarConductor(int id)
        {
            if (id <= 0)
                return BadRequest(new { mensaje = "Código de conductor inválido." });

            try
            {
                var resultado = await _uow.PreplanRepository.LiberarConductorAsync(id);

                _uow.SaveChanges(); // ✅ Solo se ejecuta en operaciones POST, PUT o DELETE

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Conductor no encontrado.", code = 0 }),
                    1 => Ok(new { mensaje = "Conductor liberado exitosamente.", code = 1 }),
                    _ => StatusCode(500, new { mensaje = "Error interno al liberar el conductor.", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> EliminarConductorDelete(int id)
        {
            if (id <= 0)
                return BadRequest(new { mensaje = "Código de conductor inválido." });

            try
            {
                var resultado = await _uow.PreplanRepository.EliminarConductorAsync(id);

                _uow.SaveChanges(); // ✅ Solo en operaciones DELETE, POST o PUT

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Conductor no encontrado.", code = 0 }),
                    1 => Ok(new { mensaje = "Conductor eliminado exitosamente.", code = 1 }),
                    _ => StatusCode(500, new { mensaje = "Error interno al eliminar el conductor.", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPost("HabilitarUnidad/{placa}")]
        public async Task<IActionResult> HabilitarUnidad(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
                return BadRequest(new { mensaje = "La placa de la unidad es requerida." });

            try
            {
                var resultado = await _uow.PreplanRepository.HabilitarUnidadAsync(placa);

                _uow.SaveChanges(); // ✅ Solo en POST, PUT o DELETE

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Unidad no encontrada.", code = 0 }),
                    1 => Ok(new { mensaje = "Unidad habilitada exitosamente.", code = 1 }),
                    _ => StatusCode(500, new { mensaje = "Error interno al habilitar la unidad.", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPost("DeshabilitarUnidad/{placa}")]
        public async Task<IActionResult> DeshabilitarUnidad(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
                return BadRequest(new { mensaje = "La placa de la unidad es requerida." });

            try
            {
                var resultado = await _uow.PreplanRepository.DeshabilitarUnidadAsync(placa);

                _uow.SaveChanges(); // ✅ Confirmar transacción

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Unidad no encontrada.", code = 0 }),
                    1 => Ok(new { mensaje = "Unidad deshabilitada exitosamente.", code = 1 }),
                    _ => StatusCode(500, new { mensaje = "Error interno al deshabilitar la unidad.", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPut("LiberarUnidad/{placa}")]
        public async Task<IActionResult> LiberarUnidad(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
                return BadRequest(new { mensaje = "La placa de la unidad es requerida." });

            try
            {
                var resultado = await _uow.PreplanRepository.LiberarUnidadAsync(placa);

                _uow.SaveChanges(); // ✅ Se guarda solo si la operación fue exitosa

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Unidad no encontrada o no se pudo liberar.", code = 0 }),
                    1 => Ok(new { mensaje = "Unidad liberada exitosamente.", code = 1 }),
                    _ => Ok(new { mensaje = $"{resultado} unidades liberadas exitosamente.", code = 1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPut("ActualizarDireccionPasajero/{codpedido}/{codubicli}")]
        public async Task<IActionResult> UpdDirPasServicio(int codpedido, string codubicli)
        {
            if (codpedido <= 0)
                return BadRequest(new { mensaje = "El código del pedido es inválido." });

            if (string.IsNullOrWhiteSpace(codubicli))
                return BadRequest(new { mensaje = "El código de ubicación del cliente es requerido." });

            try
            {
                var resultado = await _uow.PreplanRepository.UpdDirPasServicio(codpedido, codubicli);

                _uow.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { mensaje = "Pedido no encontrado o no se pudo actualizar la dirección.", code = 0 }),
                    1 => Ok(new { mensaje = "Dirección del pasajero actualizada exitosamente.", code = 1 }),
                    _ => Ok(new { mensaje = $"{resultado} servicios actualizados exitosamente.", code = 1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor.",
                    detalle = ex.Message
                });
            }

        }


        [HttpPost("DireccionAdicional")]
        public async Task<IActionResult> CrearLugarCliente([FromBody] LugarCliente lugarCliente)
        {
            if (lugarCliente == null)
                return BadRequest(new { mensaje = "Los datos del lugar cliente son requeridos." });

            try
            {
                int filasAfectadas = await _uow.PreplanRepository.NuevoLugarCliente(lugarCliente);

                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new
                    {
                        mensaje = "Dirección adicional creada exitosamente.",
                        filasAfectadas
                    });
                }

                return BadRequest(new { mensaje = "No se pudo crear la dirección." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al crear la dirección.",
                    detalle = ex.Message
                });
            }

        }


        [HttpDelete("EliminarDireccion")]
        public async Task<IActionResult> EliminarLugarCliente(int codlugar)
        {
            if (codlugar <= 0)
                return BadRequest(new { mensaje = "Código de lugar inválido." });

            try
            {
                int filasAfectadas = await _uow.PreplanRepository.EliminarLugarCliente(codlugar);

                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new
                    {
                        mensaje = "Dirección eliminada exitosamente.",
                        filasAfectadas
                    });
                }

                return NotFound(new { mensaje = "No se encontró la dirección a eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al eliminar la dirección.",
                    detalle = ex.Message
                });
            }

        }

        // POST api/latam/insert
        [HttpPost("insertLatam")]
        public async Task<IActionResult> InsertPedidoLatam([FromBody] List<List<RegistroExcelLatam>> gruposRegistros, [FromQuery] string usuario)
        {
            if (gruposRegistros == null || !gruposRegistros.Any())
            {
                return BadRequest("El arreglo de grupos está vacío.");
            }

            if (string.IsNullOrWhiteSpace(usuario))
            {
                return BadRequest("El usuario es requerido.");
            }

            try
            {
                var result = await _uow.PreplanRepository.InsertPedidoLatam(gruposRegistros, usuario);
                _uow.SaveChanges();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar servicios Latam", error = ex.Message });
            }
        }

        [HttpPost("Generarlink")]
        public async Task<IActionResult> GenerarLink([FromBody] ControlTrack registro)
        {
            if (registro == null)
                return BadRequest(new { mensaje = "Los datos enviados son inválidos." });

            try
            {
                int filasAfectadas = await _uow.PreplanRepository.Generarlink(registro);

                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new
                    {
                        mensaje = "Link generado exitosamente.",
                        filasAfectadas
                    });
                }

                return BadRequest(new { mensaje = "No se pudo generar el link." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al generar el link.",
                    detalle = ex.Message
                });
            }
        }

        [HttpGet("ObtenerPorToken/{token}")]
        public async Task<IActionResult> ObtenerPorToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest(new { mensaje = "El token es obligatorio." });

            try
            {
                var resultado = await _uow.PreplanRepository.ObtenerPorToken(token);

                if (resultado == null)
                {
                    return NotFound(new { mensaje = "No se encontró información para el token proporcionado." });
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor al obtener el registro.",
                    detalle = ex.Message
                });
            }
        }

        [HttpGet("ExcelServiciosConductor")]
        public async Task<IActionResult> ExcelServiciosConductor([FromQuery] string codConductor, [FromQuery] string fecha, [FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(codConductor))
                return BadRequest(new { mensaje = "El código de conductor es obligatorio." });
            if (string.IsNullOrEmpty(fecha))
                return BadRequest(new { mensaje = "La fecha es obligatoria." });

            try
            {
                var resultado = await _readOnlyUow.PreplanRepository.ReporteConductorServicio(
                    codConductor,
                    fecha
                );

                if (resultado == null || !resultado.Any())
                {
                    return NotFound(new { mensaje = "No se encontraron servicios para los parámetros proporcionados." });
                }

                var excelBytes = await GenerarExcelServiciosConductor(resultado, codConductor, fecha, usuario);
                string fileName = $"Servicios_Conductor_{codConductor}_{fecha.Replace("/", "-")}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (InvalidOperationException ex)
            {
                // Excepción específica para conductor sin turno
                return BadRequest(new { mensaje = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al generar el reporte de servicios del conductor.",
                    detalle = ex.Message
                });
            }
        }

        private async Task<byte[]> GenerarExcelServiciosConductor(List<ServicioDetalle> resultado, string codConductor, string fecha, string usuario)

        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Servicios Conductor");

                // Título principal
                var rangoTitulo = worksheet.Range("C2:H5");
                rangoTitulo.Merge();
                rangoTitulo.Value = "REPORTE DE SERVICIOS POR CONDUCTOR";
                rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoTitulo.Style.Font.FontColor = XLColor.White;
                rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoTitulo.Style.Font.FontName = "Calibri";
                rangoTitulo.Style.Font.FontSize = 16;
                rangoTitulo.Style.Font.SetBold();

                // Fecha de generación
                worksheet.Cell("J7").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                worksheet.Cell("J7").Style.Font.FontName = "Calibri";
                worksheet.Cell("J7").Style.Font.FontSize = 10;
                worksheet.Cell("J7").Style.Font.SetBold();
                worksheet.Cell("J7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("J8").Value = "USUARIO : " + (usuario?.ToUpper() ?? "N/A");
                worksheet.Cell("J8").Style.Font.FontName = "Calibri";
                worksheet.Cell("J8").Style.Font.FontSize = 10;
                worksheet.Cell("J8").Style.Font.SetBold();
                worksheet.Cell("J8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Línea azul de separación
                worksheet.Range("B8:J8").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B8:J8").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string imageUrl1 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public";
                byte[] imageBytes1 = await DownloadImageAsync(imageUrl1);
                using (var ms1 = new MemoryStream(imageBytes1))
                {
                    var image = worksheet.AddPicture(ms1).MoveTo(worksheet.Cell("B2")).WithSize(81, 81);
                }

                string imageUrl2 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public";
                byte[] imageBytes2 = await DownloadImageAsync(imageUrl2);
                using (var ms2 = new MemoryStream(imageBytes2))
                {
                    var image2 = worksheet.AddPicture(ms2).MoveTo(worksheet.Cell("I2")).WithSize(240, 80);
                }

                // CALCULAR PERIODO Y TURNOS TRABAJADOS
                // Obtener turno y hora de inicio del primer resultado
                string turnoRaw = resultado.FirstOrDefault()?.Turno ?? "D";
                string turno = turnoRaw.ToUpper() == "D" ? "Día" :
                               turnoRaw.ToUpper() == "N" ? "Noche" :
                               turnoRaw;

                string hora = resultado.FirstOrDefault()?.HoraInicioTurno ?? "00:00";

                DateTime fechaInicio = DateTime.ParseExact($"{fecha} {hora}", "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                DateTime fechaFin = fechaInicio.AddHours(12);

                int turnosTrabajados = 1;
                string periodo;


                if (fechaInicio.Date == fechaFin.Date)
                {
                    periodo = fechaInicio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                    turnosTrabajados = 1;
                }
                else
                {
                    periodo = $"{fechaInicio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} - {fechaFin.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
                    turnosTrabajados = 2;
                }

                // CALCULAR MÉTRICAS
                int cantidadServicios = resultado
                    .Where(s => !string.IsNullOrEmpty(s.Numero))
                    .Select(s => s.Numero)
                    .Distinct()
                    .Count();

                decimal promedioServicios = turnosTrabajados > 0 ? (decimal)cantidadServicios / turnosTrabajados : 0;

                string nombreConductor = resultado.FirstOrDefault()?.ApellidosConductor ?? "";
                string unidadAsignada = resultado.FirstOrDefault()?.Unidadasig ?? "";

                // Calcular horas de manejo (agrupado por servicio)
                var serviciosUnicos = resultado
                    .Where(s => !string.IsNullOrEmpty(s.Numero))
                    .GroupBy(s => s.Numero)
                    .Select(g => g.First())
                    .ToList();

                TimeSpan totalHorasManejo = SumarDiferenciasTiempo(serviciosUnicos);
                string horasmanejo = FormatearHoras(totalHorasManejo);

                // Calcular horas de manejo promedio
                TimeSpan horasManejoPromTs = turnosTrabajados > 0
                    ? TimeSpan.FromMinutes(totalHorasManejo.TotalMinutes / turnosTrabajados)
                    : TimeSpan.Zero;
                string horasmanejoprom = FormatearHoras(horasManejoPromTs);

                // Calcular puntualidad (% de servicios únicos con diferencia tiempo verde para Recojo)
                int totalRecojoServicios = 0;
                int recojosPuntuales = 0;

                var serviciosRecojoUnicos = resultado
                    .Where(s => !string.IsNullOrEmpty(s.Numero) && s.Tipo?.ToUpper() == "I")
                    .GroupBy(s => s.Numero)
                    .Select(g => g.First())
                    .ToList();

                foreach (var servicio in serviciosRecojoUnicos)
                {
                    totalRecojoServicios++;
                    string diferenciaTiempo = CalcularDiferenciaTiempo(servicio.HoraAto ?? "", servicio.HoraTurno ?? "");

                    if (!string.IsNullOrEmpty(diferenciaTiempo) && !diferenciaTiempo.StartsWith("-"))
                    {
                        recojosPuntuales++;
                    }
                }

                int puntualidad = totalRecojoServicios > 0 ? (int)Math.Round((decimal)recojosPuntuales / totalRecojoServicios * 100) : 0;

                // DETALLES DEBAJO DE LA LÍNEA AZUL - Fila 10
                int filaDetalles = 10;

                // FILA 10 - CONDUCTOR y TURNOS.TRAB y HORAS MANEJO
                var rangoCondutor = worksheet.Range("B10:C10");
                rangoCondutor.Merge();
                rangoCondutor.Value = "CONDUCTOR";
                rangoCondutor.Style.Font.FontName = "Calibri";
                rangoCondutor.Style.Font.FontSize = 10;
                rangoCondutor.Style.Font.SetBold();
                rangoCondutor.Style.Font.FontColor = XLColor.White;
                rangoCondutor.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoCondutor.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoCondutor.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 4).Value = nombreConductor + " - " + unidadAsignada;
                worksheet.Cell(filaDetalles, 4).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 4).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 4).Style.Font.SetBold();
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangoturnos = worksheet.Range("F10:G10");
                rangoturnos.Merge();
                rangoturnos.Value = "TURNOS TRABAJADOS";
                rangoturnos.Style.Font.FontName = "Calibri";
                rangoturnos.Style.Font.FontSize = 10;
                rangoturnos.Style.Font.SetBold();
                rangoturnos.Style.Font.FontColor = XLColor.White;
                rangoturnos.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoturnos.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoturnos.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 8).Value = turnosTrabajados.ToString("D2");
                worksheet.Cell(filaDetalles, 8).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 8).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangohorasman = worksheet.Range("J10:K10");
                rangohorasman.Merge();
                rangohorasman.Value = "HORAS MANEJO";
                rangohorasman.Style.Font.FontName = "Calibri";
                rangohorasman.Style.Font.FontSize = 10;
                rangohorasman.Style.Font.SetBold();
                rangohorasman.Style.Font.FontColor = XLColor.White;
                rangohorasman.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangohorasman.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangohorasman.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 12).Value = horasmanejo;
                worksheet.Cell(filaDetalles, 12).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                filaDetalles++; // Fila 11

                // FILA 11 - PERIODO y CANT. SER y HORMANPROM
                var rangoperiodo = worksheet.Range("B11:C11");
                rangoperiodo.Merge();
                rangoperiodo.Value = "PERIODO";
                rangoperiodo.Style.Font.FontName = "Calibri";
                rangoperiodo.Style.Font.FontSize = 10;
                rangoperiodo.Style.Font.SetBold();
                rangoperiodo.Style.Font.FontColor = XLColor.White;
                rangoperiodo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoperiodo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoperiodo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 4).Value = periodo;
                worksheet.Cell(filaDetalles, 4).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 4).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 4).Style.Font.SetBold();
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangocantser = worksheet.Range("F11:G11");
                rangocantser.Merge();
                rangocantser.Value = "CANTIDAD SERVICIOS";
                rangocantser.Style.Font.FontName = "Calibri";
                rangocantser.Style.Font.FontSize = 10;
                rangocantser.Style.Font.SetBold();
                rangocantser.Style.Font.FontColor = XLColor.White;
                rangocantser.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangocantser.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangocantser.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 8).Value = cantidadServicios.ToString("D2");
                worksheet.Cell(filaDetalles, 8).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 8).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangohorasprom = worksheet.Range("J11:K11");
                rangohorasprom.Merge();
                rangohorasprom.Value = "HORAS MANEJO PROMEDIO";
                rangohorasprom.Style.Font.FontName = "Calibri";
                rangohorasprom.Style.Font.FontSize = 10;
                rangohorasprom.Style.Font.SetBold();
                rangohorasprom.Style.Font.FontColor = XLColor.White;
                rangohorasprom.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangohorasprom.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangohorasprom.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 12).Value = horasmanejoprom;
                worksheet.Cell(filaDetalles, 12).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                filaDetalles++; // Fila 12

                // FILA 12 - TURNO y PROM. SER y PUNTUALIDAD
                var rangoturno = worksheet.Range("B12:C12");
                rangoturno.Merge();
                rangoturno.Value = "TURNO";
                rangoturno.Style.Font.FontName = "Calibri";
                rangoturno.Style.Font.FontSize = 10;
                rangoturno.Style.Font.SetBold();
                rangoturno.Style.Font.FontColor = XLColor.White;
                rangoturno.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoturno.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoturno.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 4).Value = turno;
                worksheet.Cell(filaDetalles, 4).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 4).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 4).Style.Font.SetBold();
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangoprom = worksheet.Range("F12:G12");
                rangoprom.Merge();
                rangoprom.Value = "PROMEDIO SERVICIOS";
                rangoprom.Style.Font.FontName = "Calibri";
                rangoprom.Style.Font.FontSize = 10;
                rangoprom.Style.Font.SetBold();
                rangoprom.Style.Font.FontColor = XLColor.White;
                rangoprom.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoprom.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoprom.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 8).Value = Math.Round(promedioServicios).ToString("00");
                worksheet.Cell(filaDetalles, 8).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 8).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangopuntual = worksheet.Range("J12:K12");
                rangopuntual.Merge();
                rangopuntual.Value = "PUNTUALIDAD";
                rangopuntual.Style.Font.FontName = "Calibri";
                rangopuntual.Style.Font.FontSize = 10;
                rangopuntual.Style.Font.SetBold();
                rangopuntual.Style.Font.FontColor = XLColor.White;
                rangopuntual.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangopuntual.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangopuntual.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 12).Value = puntualidad.ToString() + "%";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                filaDetalles++; // Fila 13
                filaDetalles++; // Fila 14 - Espacio

                // Cabeceras de la tabla - Fila 15
                var headers = new[]{"ITEM", "FECHA", "CLIENTE", "RECOJO/REPARTO", "N/SERV", "HORA TURNO", "HORA DE INICIO", "HORA LLEGADA ATO", "DIFERENCIA TIEMPO", "TIEMPO PROGRAMADO", "NOMBRES", "DIRECCIÓN", "DISTRITO", "PLACA", "CONDUCTOR"};

                int filaHeaders = filaDetalles;
                worksheet.Row(filaHeaders).Height = 40;

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(filaHeaders, i + 2).Value = headers[i];
                    worksheet.Cell(filaHeaders, i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.FontName = "Calibri";
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.FontSize = 10;
                    worksheet.Cell(filaHeaders, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(filaHeaders, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    worksheet.Cell(filaHeaders, i + 2).Style.Alignment.WrapText = true;
                }

                // Ancho de columnas
                worksheet.Column(2).Width = 7;   // ITEM
                worksheet.Column(3).Width = 12;  // FECHA
                worksheet.Column(4).Width = 25;  // CLIENTE
                worksheet.Column(5).Width = 18;  // RECOJO/REPARTO
                worksheet.Column(6).Width = 10;  // N/SERV
                worksheet.Column(7).Width = 14;  // HORA ACTIVO TURNO
                worksheet.Column(8).Width = 12;  // HORA DE INICIO
                worksheet.Column(9).Width = 15;  // HORA LLEGADA ATO
                worksheet.Column(10).Width = 14; // DIFERENCIA TIEMPO
                worksheet.Column(11).Width = 14; // TIEMPO PROGRAMADO
                worksheet.Column(12).Width = 35; // NOMBRES
                worksheet.Column(13).Width = 60; // DIRECCIÓN
                worksheet.Column(14).Width = 20; // DISTRITO
                worksheet.Column(15).Width = 12; // PLACA
                worksheet.Column(16).Width = 35; // CONDUCTOR

                worksheet.ShowGridLines = false;

                int fila = filaHeaders + 1;
                int item = 1;

                string numeroServicioAnterior = "";
                bool colorAlternativo = false;

                var colorGrupo1 = XLColor.FromHtml("#EBF1DE");
                var colorGrupo2 = XLColor.White;

                foreach (var servicio in resultado)
                {
                    string numeroServicioActual = servicio.Numero ?? "";

                    if (numeroServicioActual != numeroServicioAnterior)
                    {
                        if (numeroServicioAnterior != "")
                        {
                            colorAlternativo = !colorAlternativo;
                        }
                        numeroServicioAnterior = numeroServicioActual;
                    }

                    string tipoTexto = servicio.Tipo?.ToUpper() == "I" ? "Recojo" :
                                       servicio.Tipo?.ToUpper() == "S" ? "Reparto" :
                                       servicio.Tipo ?? "";

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = servicio.Fecha ?? "";
                    worksheet.Cell(fila, 4).Value = servicio.Empresa ?? ""; // CLIENTE es EMPRESA
                    worksheet.Cell(fila, 5).Value = tipoTexto;
                    worksheet.Cell(fila, 6).Value = numeroServicioActual;
                    worksheet.Cell(fila, 7).Value = servicio.HoraTurno ?? "";
                    worksheet.Cell(fila, 8).Value = servicio.HoraInicio ?? "";
                    worksheet.Cell(fila, 9).Value = servicio.HoraAto ?? "";

                    // DIFERENCIA TIEMPO = HORA LLEGADA ATO - HORA ACTIVO TURNO
                    string diferenciaTiempo = CalcularDiffTiempo(servicio.HoraAto ?? "", servicio.HoraTurno ?? "");
                    worksheet.Cell(fila, 10).Value = diferenciaTiempo;

                    // TIEMPO PROGRAMADO = HORA ACTIVO TURNO - HORA DE INICIO
                    string tiempoProgramado = CalcularDiferenciaTiempo(servicio.HoraTurno ?? "", servicio.HoraInicio ?? "");
                    tiempoProgramado = tiempoProgramado.Replace("-", ""); // Quitar signo negativo
                    worksheet.Cell(fila, 11).Value = tiempoProgramado;

                    worksheet.Cell(fila, 12).Value = servicio.Apellidos ?? ""; // NOMBRES son APELLIDOS
                    worksheet.Cell(fila, 13).Value = servicio.Direccion ?? "";
                    worksheet.Cell(fila, 14).Value = servicio.Distrito ?? "";
                    worksheet.Cell(fila, 15).Value = servicio.Unidad ?? "";
                    worksheet.Cell(fila, 16).Value = servicio.ApellidosConductor ?? "";

                    int colInicio = 2;
                    int colFin = 16;

                    var rango = worksheet.Range(fila, colInicio, fila, colFin);

                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    rango.Style.Font.FontName = "Calibri";
                    rango.Style.Font.FontSize = 10;

                    var colorFondo = colorAlternativo ? colorGrupo2 : colorGrupo1;
                    rango.Style.Fill.BackgroundColor = colorFondo;

                    // COLOR AMARILLO para HORA LLEGADA ATO
                    worksheet.Cell(fila, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe246");

                    // Aplicar color a DIFERENCIA TIEMPO solo para Recojo
                    if (tipoTexto == "Recojo" && !string.IsNullOrEmpty(diferenciaTiempo))
                    {
                        if (diferenciaTiempo.StartsWith("-"))
                        {
                            worksheet.Cell(fila, 10).Style.Font.FontColor = XLColor.Red;
                        }
                        else
                        {
                            worksheet.Cell(fila, 10).Style.Font.FontColor = XLColor.FromHtml("#228b22");
                        }
                    }

                    fila++;
                }

                int ultimaFila = fila - 1;
                if (ultimaFila >= filaHeaders)
                {
                    var rangoWrapText = worksheet.Range(filaHeaders, 7, ultimaFila, 16);
                    rangoWrapText.Style.Alignment.WrapText = true;
                }

                if (ultimaFila >= filaHeaders + 1)
                {
                    var rangoConBordes = worksheet.Range(filaHeaders + 1, 2, ultimaFila, 16);

                    rangoConBordes.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    rangoConBordes.Style.Border.TopBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.BottomBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.LeftBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.RightBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.InsideBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.OutsideBorderColor = XLColor.Gray;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private string CalcularDiffTiempo(string horaFin, string horaInicio)
        {
            if (string.IsNullOrEmpty(horaFin) || string.IsNullOrEmpty(horaInicio))
                return "";

            try
            {
                var timeInicio = TimeSpan.Parse(horaInicio);
                var timeFin = TimeSpan.Parse(horaFin);
                var diferencia = timeFin - timeInicio;

                int horas = (int)diferencia.TotalHours;
                int minutos = Math.Abs(diferencia.Minutes);

                if (diferencia.TotalMinutes < 0)
                {
                    return $"-{Math.Abs(horas):D2}:{minutos:D2}";
                }
                else
                {
                    return $"{horas:D2}:{minutos:D2}";
                }
            }
            catch
            {
                return "";
            }
        }

        private TimeSpan SumarDiferenciasTiempo(List<ServicioDetalle> servicios)
        {
            TimeSpan totalTiempo = TimeSpan.Zero;

            foreach (var servicio in servicios)
            {
                if (string.IsNullOrEmpty(servicio.HoraInicio) || string.IsNullOrEmpty(servicio.HoraAto))
                    continue;

                try
                {
                    var timeInicio = TimeSpan.Parse(servicio.HoraInicio);
                    var timeFin = TimeSpan.Parse(servicio.HoraAto);
                    var diferencia = timeFin - timeInicio;

                    if (diferencia.TotalMinutes > 0) // Solo sumar si es positivo
                    {
                        totalTiempo = totalTiempo.Add(diferencia);
                    }
                }
                catch { }
            }

            return totalTiempo;
        }

        private string FormatearHoras(TimeSpan tiempo)
        {
            int horas = (int)tiempo.TotalHours;
            int minutos = tiempo.Minutes;
            return $"{horas:D2}:{minutos:D2}";
        }

        [HttpGet("ServiciosConductorRangos")]
        public async Task<IActionResult> ExcelServiciosConductor([FromQuery] string codConductor, [FromQuery] string fechaini, [FromQuery] string fechafin, [FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(codConductor))
                return BadRequest(new { mensaje = "El código de conductor es obligatorio." });
            if (string.IsNullOrEmpty(fechaini))
                return BadRequest(new { mensaje = "La fecha inicial es obligatoria." });
            if (string.IsNullOrEmpty(fechafin))
                return BadRequest(new { mensaje = "La fecha final es obligatoria." });

            try
            {
                var resultado = await _readOnlyUow.PreplanRepository.ReporteConductorServicioRango(
                    codConductor,
                    fechaini,
                    fechafin
                );

                if (resultado == null || !resultado.Any())
                {
                    return NotFound(new { mensaje = "No se encontraron servicios para los parámetros proporcionados." });
                }

                var excelBytes = await GenerarExcelServiciosConductorRango(resultado, codConductor, fechaini, fechafin, usuario);
                string fileName = $"Servicios_Conductor_{codConductor}_{fechaini.Replace("/", "-")}_al_{fechafin.Replace("/", "-")}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al generar el reporte de servicios del conductor.",
                    detalle = ex.Message
                });
            }
        }

        //Reporte de alertas de velocidad
        private async Task<byte[]> GenerarExcelServiciosConductorRango(List<ServicioDetalle> resultado, string codConductor, string fechaini, string fechafin, string usuario)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Servicios Conductor");

                // Título principal
                var rangoTitulo = worksheet.Range("C2:H5");
                rangoTitulo.Merge();
                rangoTitulo.Value = "REPORTE DE SERVICIOS POR CONDUCTOR";
                rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoTitulo.Style.Font.FontColor = XLColor.White;
                rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoTitulo.Style.Font.FontName = "Calibri";
                rangoTitulo.Style.Font.FontSize = 16;
                rangoTitulo.Style.Font.SetBold();

                // Fecha de generación
                worksheet.Cell("J7").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                worksheet.Cell("J7").Style.Font.FontName = "Calibri";
                worksheet.Cell("J7").Style.Font.FontSize = 10;
                worksheet.Cell("J7").Style.Font.SetBold();
                worksheet.Cell("J7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("J8").Value = "USUARIO : " + (usuario?.ToUpper() ?? "N/A");
                worksheet.Cell("J8").Style.Font.FontName = "Calibri";
                worksheet.Cell("J8").Style.Font.FontSize = 10;
                worksheet.Cell("J8").Style.Font.SetBold();
                worksheet.Cell("J8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Línea azul de separación
                worksheet.Range("B8:J8").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B8:J8").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string imageUrl1 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public";
                byte[] imageBytes1 = await DownloadImageAsync(imageUrl1);
                using (var ms1 = new MemoryStream(imageBytes1))
                {
                    var image = worksheet.AddPicture(ms1).MoveTo(worksheet.Cell("B2")).WithSize(81, 81);
                }

                string imageUrl2 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/5fb05ad0-957b-4de1-ca5a-3eb24882fa00/public";
                byte[] imageBytes2 = await DownloadImageAsync(imageUrl2);
                using (var ms2 = new MemoryStream(imageBytes2))
                {
                    var image2 = worksheet.AddPicture(ms2).MoveTo(worksheet.Cell("I2")).WithSize(240, 80);
                }

                // CALCULAR PERIODO Y TURNOS TRABAJADOS
                // SIEMPRE completar las fechas con horas
                string fechaIniCompleta = $"{fechaini} 00:00";
                string fechaFinCompleta = $"{fechafin} 23:59";

                DateTime fechaInicio = DateTime.ParseExact(fechaIniCompleta, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                DateTime fechaFin = DateTime.ParseExact(fechaFinCompleta, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);


                // Calcular turnos trabajados (cada 12 horas = 1 turno)
                TimeSpan diferenciaPeriodo = fechaFin - fechaInicio;
                int turnosTrabajados = (int)Math.Ceiling(diferenciaPeriodo.TotalHours / 12);
                if (turnosTrabajados < 1) turnosTrabajados = 1;

                string periodo;
                if (fechaInicio.Date == fechaFin.Date)
                {
                    periodo = fechaInicio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                else
                {
                    periodo = $"{fechaInicio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} - {fechaFin.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
                }

                // Obtener turno del primer resultado (solo para mostrar)
                string turnoRaw = resultado.FirstOrDefault()?.Turno ?? "D";
                string turno = turnoRaw.ToUpper() == "D" ? "Día" :
                               turnoRaw.ToUpper() == "N" ? "Noche" :
                               turnoRaw;

                // CALCULAR MÉTRICAS
                int cantidadServicios = resultado
                    .Where(s => !string.IsNullOrEmpty(s.Numero))
                    .Select(s => s.Numero)
                    .Distinct()
                    .Count();

                decimal promedioServicios = turnosTrabajados > 0 ? (decimal)cantidadServicios / turnosTrabajados : 0;

                string nombreConductor = resultado.FirstOrDefault()?.ApellidosConductor ?? "";
                string unidadAsignada = resultado.FirstOrDefault()?.Unidadasig ?? "";

                // Calcular horas de manejo (agrupado por servicio)
                var serviciosUnicos = resultado
                    .Where(s => !string.IsNullOrEmpty(s.Numero))
                    .GroupBy(s => s.Numero)
                    .Select(g => g.First())
                    .ToList();

                TimeSpan totalHorasManejo = SumarDiferenciasTiempo(serviciosUnicos);
                string horasmanejo = FormatearHoras(totalHorasManejo);

                // Calcular horas de manejo promedio
                TimeSpan horasManejoPromTs = turnosTrabajados > 0
                    ? TimeSpan.FromMinutes(totalHorasManejo.TotalMinutes / turnosTrabajados)
                    : TimeSpan.Zero;
                string horasmanejoprom = FormatearHoras(horasManejoPromTs);

                // Calcular puntualidad (% de servicios únicos con diferencia tiempo verde para Recojo)
                int totalRecojoServicios = 0;
                int recojosPuntuales = 0;

                var serviciosRecojoUnicos = resultado
                    .Where(s => !string.IsNullOrEmpty(s.Numero) && s.Tipo?.ToUpper() == "I")
                    .GroupBy(s => s.Numero)
                    .Select(g => g.First())
                    .ToList();

                foreach (var servicio in serviciosRecojoUnicos)
                {
                    totalRecojoServicios++;
                    string diferenciaTiempo = CalcularDiferenciaTiempo(servicio.HoraAto ?? "", servicio.HoraTurno ?? "");

                    if (!string.IsNullOrEmpty(diferenciaTiempo) && !diferenciaTiempo.StartsWith("-"))
                    {
                        recojosPuntuales++;
                    }
                }

                int puntualidad = totalRecojoServicios > 0 ? (int)Math.Round((decimal)recojosPuntuales / totalRecojoServicios * 100) : 0;

                // DETALLES DEBAJO DE LA LÍNEA AZUL - Fila 10
                int filaDetalles = 10;

                // FILA 10 - CONDUCTOR y TURNOS.TRAB y HORAS MANEJO
                var rangoCondutor = worksheet.Range("B10:C10");
                rangoCondutor.Merge();
                rangoCondutor.Value = "CONDUCTOR";
                rangoCondutor.Style.Font.FontName = "Calibri";
                rangoCondutor.Style.Font.FontSize = 10;
                rangoCondutor.Style.Font.SetBold();
                rangoCondutor.Style.Font.FontColor = XLColor.White;
                rangoCondutor.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoCondutor.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoCondutor.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 4).Value = nombreConductor + " - " + unidadAsignada;
                worksheet.Cell(filaDetalles, 4).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 4).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 4).Style.Font.SetBold();
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangoturnos = worksheet.Range("F10:G10");
                rangoturnos.Merge();
                rangoturnos.Value = "TURNOS TRABAJADOS";
                rangoturnos.Style.Font.FontName = "Calibri";
                rangoturnos.Style.Font.FontSize = 10;
                rangoturnos.Style.Font.SetBold();
                rangoturnos.Style.Font.FontColor = XLColor.White;
                rangoturnos.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoturnos.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoturnos.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 8).Value = turnosTrabajados.ToString("D2");
                worksheet.Cell(filaDetalles, 8).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 8).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangohorasman = worksheet.Range("J10:K10");
                rangohorasman.Merge();
                rangohorasman.Value = "HORAS MANEJO";
                rangohorasman.Style.Font.FontName = "Calibri";
                rangohorasman.Style.Font.FontSize = 10;
                rangohorasman.Style.Font.SetBold();
                rangohorasman.Style.Font.FontColor = XLColor.White;
                rangohorasman.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangohorasman.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangohorasman.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 12).Value = horasmanejo;
                worksheet.Cell(filaDetalles, 12).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                filaDetalles++; // Fila 11

                // FILA 11 - PERIODO y CANT. SER y HORMANPROM
                var rangoperiodo = worksheet.Range("B11:C11");
                rangoperiodo.Merge();
                rangoperiodo.Value = "PERIODO";
                rangoperiodo.Style.Font.FontName = "Calibri";
                rangoperiodo.Style.Font.FontSize = 10;
                rangoperiodo.Style.Font.SetBold();
                rangoperiodo.Style.Font.FontColor = XLColor.White;
                rangoperiodo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoperiodo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoperiodo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 4).Value = periodo;
                worksheet.Cell(filaDetalles, 4).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 4).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 4).Style.Font.SetBold();
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangocantser = worksheet.Range("F11:G11");
                rangocantser.Merge();
                rangocantser.Value = "CANTIDAD SERVICIOS";
                rangocantser.Style.Font.FontName = "Calibri";
                rangocantser.Style.Font.FontSize = 10;
                rangocantser.Style.Font.SetBold();
                rangocantser.Style.Font.FontColor = XLColor.White;
                rangocantser.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangocantser.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangocantser.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 8).Value = cantidadServicios.ToString("D2");
                worksheet.Cell(filaDetalles, 8).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 8).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangohorasprom = worksheet.Range("J11:K11");
                rangohorasprom.Merge();
                rangohorasprom.Value = "HORAS MANEJO PROMEDIO";
                rangohorasprom.Style.Font.FontName = "Calibri";
                rangohorasprom.Style.Font.FontSize = 10;
                rangohorasprom.Style.Font.SetBold();
                rangohorasprom.Style.Font.FontColor = XLColor.White;
                rangohorasprom.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangohorasprom.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangohorasprom.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 12).Value = horasmanejoprom;
                worksheet.Cell(filaDetalles, 12).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                filaDetalles++; // Fila 12

                // FILA 12 - TURNO y PROM. SER y PUNTUALIDAD
                var rangoturno = worksheet.Range("B12:C12");
                rangoturno.Merge();
                rangoturno.Value = "TURNO";
                rangoturno.Style.Font.FontName = "Calibri";
                rangoturno.Style.Font.FontSize = 10;
                rangoturno.Style.Font.SetBold();
                rangoturno.Style.Font.FontColor = XLColor.White;
                rangoturno.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoturno.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoturno.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 4).Value = turno;
                worksheet.Cell(filaDetalles, 4).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 4).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 4).Style.Font.SetBold();
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(filaDetalles, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangoprom = worksheet.Range("F12:G12");
                rangoprom.Merge();
                rangoprom.Value = "PROMEDIO SERVICIOS";
                rangoprom.Style.Font.FontName = "Calibri";
                rangoprom.Style.Font.FontSize = 10;
                rangoprom.Style.Font.SetBold();
                rangoprom.Style.Font.FontColor = XLColor.White;
                rangoprom.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoprom.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoprom.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 8).Value = Math.Round(promedioServicios).ToString("00");
                worksheet.Cell(filaDetalles, 8).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 8).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var rangopuntual = worksheet.Range("J12:K12");
                rangopuntual.Merge();
                rangopuntual.Value = "PUNTUALIDAD";
                rangopuntual.Style.Font.FontName = "Calibri";
                rangopuntual.Style.Font.FontSize = 10;
                rangopuntual.Style.Font.SetBold();
                rangopuntual.Style.Font.FontColor = XLColor.White;
                rangopuntual.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangopuntual.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangopuntual.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(filaDetalles, 12).Value = puntualidad.ToString() + "%";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontName = "Calibri";
                worksheet.Cell(filaDetalles, 12).Style.Font.FontSize = 10;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(filaDetalles, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                filaDetalles++; // Fila 13
                filaDetalles++; // Fila 14 - Espacio

                // Cabeceras de la tabla - Fila 15
                var headers = new[] { "ITEM", "FECHA", "CLIENTE", "RECOJO/REPARTO", "N/SERV", "HORA TURNO", "HORA DE INICIO", "HORA LLEGADA ATO", "DIFERENCIA TIEMPO", "TIEMPO PROGRAMADO", "NOMBRES", "DIRECCIÓN", "DISTRITO", "PLACA", "CONDUCTOR" };

                int filaHeaders = filaDetalles;
                worksheet.Row(filaHeaders).Height = 40;

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(filaHeaders, i + 2).Value = headers[i];
                    worksheet.Cell(filaHeaders, i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.FontName = "Calibri";
                    worksheet.Cell(filaHeaders, i + 2).Style.Font.FontSize = 10;
                    worksheet.Cell(filaHeaders, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(filaHeaders, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    worksheet.Cell(filaHeaders, i + 2).Style.Alignment.WrapText = true;
                }

                // Ancho de columnas
                worksheet.Column(2).Width = 7;   // ITEM
                worksheet.Column(3).Width = 12;  // FECHA
                worksheet.Column(4).Width = 25;  // CLIENTE
                worksheet.Column(5).Width = 18;  // RECOJO/REPARTO
                worksheet.Column(6).Width = 10;  // N/SERV
                worksheet.Column(7).Width = 14;  // HORA ACTIVO TURNO
                worksheet.Column(8).Width = 12;  // HORA DE INICIO
                worksheet.Column(9).Width = 15;  // HORA LLEGADA ATO
                worksheet.Column(10).Width = 14; // DIFERENCIA TIEMPO
                worksheet.Column(11).Width = 14; // TIEMPO PROGRAMADO
                worksheet.Column(12).Width = 35; // NOMBRES
                worksheet.Column(13).Width = 60; // DIRECCIÓN
                worksheet.Column(14).Width = 20; // DISTRITO
                worksheet.Column(15).Width = 12; // PLACA
                worksheet.Column(16).Width = 35; // CONDUCTOR

                worksheet.ShowGridLines = false;

                int fila = filaHeaders + 1;
                int item = 1;

                string numeroServicioAnterior = "";
                bool colorAlternativo = false;

                var colorGrupo1 = XLColor.FromHtml("#EBF1DE");
                var colorGrupo2 = XLColor.White;

                foreach (var servicio in resultado)
                {
                    string numeroServicioActual = servicio.Numero ?? "";

                    if (numeroServicioActual != numeroServicioAnterior)
                    {
                        if (numeroServicioAnterior != "")
                        {
                            colorAlternativo = !colorAlternativo;
                        }
                        numeroServicioAnterior = numeroServicioActual;
                    }

                    string tipoTexto = servicio.Tipo?.ToUpper() == "I" ? "Recojo" :
                                       servicio.Tipo?.ToUpper() == "S" ? "Reparto" :
                                       servicio.Tipo ?? "";

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = servicio.Fecha ?? "";
                    worksheet.Cell(fila, 4).Value = servicio.Empresa ?? ""; // CLIENTE es EMPRESA
                    worksheet.Cell(fila, 5).Value = tipoTexto;
                    worksheet.Cell(fila, 6).Value = numeroServicioActual;
                    worksheet.Cell(fila, 7).Value = servicio.HoraTurno ?? "";
                    worksheet.Cell(fila, 8).Value = servicio.HoraInicio ?? "";
                    worksheet.Cell(fila, 9).Value = servicio.HoraAto ?? "";

                    // DIFERENCIA TIEMPO = HORA LLEGADA ATO - HORA ACTIVO TURNO
                    string diferenciaTiempo = CalcularDiffTiempo(servicio.HoraAto ?? "", servicio.HoraTurno ?? "");
                    worksheet.Cell(fila, 10).Value = diferenciaTiempo;

                    // TIEMPO PROGRAMADO = HORA ACTIVO TURNO - HORA DE INICIO
                    string tiempoProgramado = CalcularDiferenciaTiempo(servicio.HoraTurno ?? "", servicio.HoraInicio ?? "");
                    tiempoProgramado = tiempoProgramado.Replace("-", ""); // Quitar signo negativo
                    worksheet.Cell(fila, 11).Value = tiempoProgramado;

                    worksheet.Cell(fila, 12).Value = servicio.Apellidos ?? ""; // NOMBRES son APELLIDOS
                    worksheet.Cell(fila, 13).Value = servicio.Direccion ?? "";
                    worksheet.Cell(fila, 14).Value = servicio.Distrito ?? "";
                    worksheet.Cell(fila, 15).Value = servicio.Unidad ?? "";
                    worksheet.Cell(fila, 16).Value = servicio.ApellidosConductor ?? "";

                    int colInicio = 2;
                    int colFin = 16;

                    var rango = worksheet.Range(fila, colInicio, fila, colFin);

                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    rango.Style.Font.FontName = "Calibri";
                    rango.Style.Font.FontSize = 10;

                    var colorFondo = colorAlternativo ? colorGrupo2 : colorGrupo1;
                    rango.Style.Fill.BackgroundColor = colorFondo;

                    // COLOR AMARILLO para HORA LLEGADA ATO
                    worksheet.Cell(fila, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe246");

                    // Aplicar color a DIFERENCIA TIEMPO solo para Recojo
                    if (tipoTexto == "Recojo" && !string.IsNullOrEmpty(diferenciaTiempo))
                    {
                        if (diferenciaTiempo.StartsWith("-"))
                        {
                            worksheet.Cell(fila, 10).Style.Font.FontColor = XLColor.Red;
                        }
                        else
                        {
                            worksheet.Cell(fila, 10).Style.Font.FontColor = XLColor.FromHtml("#228b22");
                        }
                    }

                    fila++;
                }

                int ultimaFila = fila - 1;
                if (ultimaFila >= filaHeaders)
                {
                    var rangoWrapText = worksheet.Range(filaHeaders, 7, ultimaFila, 16);
                    rangoWrapText.Style.Alignment.WrapText = true;
                }

                if (ultimaFila >= filaHeaders + 1)
                {
                    var rangoConBordes = worksheet.Range(filaHeaders + 1, 2, ultimaFila, 16);

                    rangoConBordes.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rangoConBordes.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    rangoConBordes.Style.Border.TopBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.BottomBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.LeftBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.RightBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.InsideBorderColor = XLColor.Gray;
                    rangoConBordes.Style.Border.OutsideBorderColor = XLColor.Gray;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        [HttpPost("InsertarAlertaVelocidad")]
        public async Task<IActionResult> InsertarAlertaVelocidad([FromBody] SpeedAlert alerta)
        {
            try
            {
                var resultado = await _uow.PreplanRepository.InsertarAlertaVelocidad(alerta);
                _uow.SaveChanges();

                if (resultado > 0)
                {
                    return Ok(new { message = "Alerta insertada correctamente", id = resultado });
                }
                return BadRequest("No se pudo insertar la alerta.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("AlertasVelocidad")]
        public async Task<IActionResult> ReporteAlertasVelocidad([FromQuery] string usuario, [FromQuery] string fechaini, [FromQuery] string fechafin)
        {
            try
            {
                var alertas = await _readOnlyUow.PreplanRepository.ReporteAlertasVelocidad(usuario, fechaini, fechafin);
                if (alertas == null || alertas.Count == 0)
                {
                    return Ok("No se encontraron alertas de velocidad.");
                }
                return Ok(alertas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("AlertasVelocidadExcel")]
        public async Task<IActionResult> ResumenExcelAlertasVelocidad([FromQuery] string usuario, [FromQuery] string fechaini, [FromQuery] string fechafin)
        {
            try
            {
                var alertas = await _readOnlyUow.PreplanRepository.ReporteAlertasVelocidad(usuario, fechaini, fechafin);

                if (alertas == null || alertas.Count == 0)
                {
                    return NotFound("No se encontraron alertas de velocidad para exportar.");
                }

                // Generar el archivo Excel
                var excelBytes = await ConvertDataExcelAlertasVelocidad(alertas, usuario, fechaini, fechafin);
                string fileName = $"Alertas_Velocidad_{usuario}_{fechaini}_a_{fechafin}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al generar el reporte Excel de Alertas de Velocidad.",
                    error = ex.Message
                });
            }
        }

        private async Task<byte[]> ConvertDataExcelAlertasVelocidad(List<SpeedAlert> alertas, string usuario, string fechaini, string fechafin)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Alertas de Velocidad");

                // Título principal
                var rangoTitulo = worksheet.Range("B4:H7");
                rangoTitulo.Merge();
                rangoTitulo.Value = "REPORTE DE ALERTAS DE VELOCIDAD: " + usuario.ToUpper();
                rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoTitulo.Style.Font.FontColor = XLColor.White;
                rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                rangoTitulo.Style.Font.FontName = "Calibri";
                rangoTitulo.Style.Font.FontSize = 16;
                rangoTitulo.Style.Font.SetBold();

                // Fecha de Inicio
                worksheet.Range("B9:C9").Merge();
                worksheet.Cell(9, 2).Value = "Fecha de Inicio:";
                worksheet.Cell(9, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(9, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(9, 2).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(9, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                worksheet.Cell(9, 2).Style.Font.FontName = "Calibri";
                worksheet.Cell(9, 2).Style.Font.FontSize = 10;
                worksheet.Cell(9, 2).Style.Font.SetBold();

                worksheet.Range("D9:E9").Merge();
                worksheet.Cell(9, 4).Value = fechaini;
                worksheet.Cell(9, 4).Style = worksheet.Cell(9, 2).Style;

                // Fecha de Fin
                worksheet.Range("B10:C10").Merge();
                worksheet.Cell(10, 2).Value = "Fecha de Fin:";
                worksheet.Cell(10, 2).Style = worksheet.Cell(9, 2).Style;

                worksheet.Range("D10:E10").Merge();
                worksheet.Cell(10, 4).Value = fechafin;
                worksheet.Cell(10, 4).Style = worksheet.Cell(9, 4).Style;

                // Fecha y usuario de generación
                worksheet.Cell("H9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cell("H9").Style.Font.FontName = "Calibri";
                worksheet.Cell("H9").Style.Font.FontSize = 10;
                worksheet.Cell("H9").Style.Font.SetBold();
                worksheet.Cell("H9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("H10").Value = "USUARIO: " + usuario.ToUpper();
                worksheet.Cell("H10").Style.Font.FontName = "Calibri";
                worksheet.Cell("H10").Style.Font.FontSize = 10;
                worksheet.Cell("H10").Style.Font.SetBold();
                worksheet.Cell("H10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:H10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:H10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes (opcional - usa las mismas URLs de tu método original)
                string imageUrl1 = "https://imagedelivery.net/o0E1jB_kGKnYacpYCBFmZA/e880b9a3-e8f9-4278-9d06-6c2f661b8800/public";
                byte[] imageBytes1 = await DownloadImageAsync(imageUrl1);
                using (var ms1 = new MemoryStream(imageBytes1))
                {
                    var image = worksheet.AddPicture(ms1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);
                }

                // Cabeceras
                var headers = new[] { "ITEM", "UNIDAD", "FECHA", "HORA", "VELOCIDAD", "LATITUD", "LONGITUD" };

                worksheet.Row(12).Height = 40;

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(12, i + 2).Value = headers[i];
                    worksheet.Cell(12, i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3446");
                    worksheet.Cell(12, i + 2).Style.Font.Bold = true;
                    worksheet.Cell(12, i + 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(12, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(12, i + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Ancho de columnas
                worksheet.Column(2).Width = 8;   // ITEM
                worksheet.Column(3).Width = 20;  // UNIDAD
                worksheet.Column(4).Width = 15;  // FECHA
                worksheet.Column(5).Width = 12;  // HORA
                worksheet.Column(6).Width = 15;  // VELOCIDAD
                worksheet.Column(7).Width = 18;  // LATITUD
                worksheet.Column(8).Width = 18;  // LONGITUD

                worksheet.ShowGridLines = false;

                // Llenar datos
                int fila = 13;
                int item = 1;

                foreach (var alerta in alertas)
                {
                    // Separar fecha y hora del campo datetime
                    string[] fechaHora = alerta.Datetime?.ToString().Split(' ') ?? new string[] { "", "" };
                    string fecha = fechaHora.Length > 0 ? fechaHora[0] : "";
                    string hora = fechaHora.Length > 1 ? fechaHora[1] : "";

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = alerta.DeviceID ?? "";
                    worksheet.Cell(fila, 4).Value = fecha;
                    worksheet.Cell(fila, 5).Value = hora;
                    worksheet.Cell(fila, 6).Value = alerta.Speed != null ? $"{alerta.Speed} Km/h" : "";
                    worksheet.Cell(fila, 7).Value = alerta.Latitude != null ? double.Parse(alerta.Latitude.ToString()).ToString("F5") : "";
                    worksheet.Cell(fila, 8).Value = alerta.Longitude != null ? double.Parse(alerta.Longitude.ToString()).ToString("F5") : "";
                    fila++;
                }

                // Aplicar estilos de centrado y color intercalado
                int colInicio = 2; // Columna B
                int colFin = 8;    // Columna H

                for (int i = 13; i < fila; i++)
                {
                    var rango = worksheet.Range(i, colInicio, i, colFin);

                    // Centrado horizontal y vertical
                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Color intercalado
                    if ((i - 13) % 2 == 0)
                    {
                        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#f2f2f2");
                    }
                    else
                    {
                        rango.Style.Fill.BackgroundColor = XLColor.White;
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
