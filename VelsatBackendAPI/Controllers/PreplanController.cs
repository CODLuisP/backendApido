using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreplanController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public PreplanController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // POST api/preplan/insert
        [HttpPost("insert")]
        public async Task<IActionResult> InsertPedido([FromBody] IEnumerable<ExcelAvianca> excel, [FromQuery] string fecact, [FromQuery] string tipo, [FromQuery] string usuario)
        {
            if (excel == null || !excel.Any())
            {
                return BadRequest("El arreglo Excel está vacío.");
            }

            var result = await _unitOfWork.PreplanRepository.InsertPedido(excel, fecact, tipo, usuario);

            _unitOfWork.SaveChanges();

            return Ok(result);
        }


        [HttpGet("get")]
        public async Task<IActionResult> GetPedidos([FromQuery] string dato, [FromQuery] string empresa, [FromQuery] string usuario)
        {
            try
            {
                var pedidos = await _unitOfWork.PreplanRepository.GetPedidos(dato, empresa, usuario);

                if (pedidos == null || pedidos.Count == 0)
                {
                    return NotFound("No se encontraron pedidos.");
                }

                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPut("save")]
        public async Task<IActionResult> SavePedidos([FromBody] IEnumerable<Pedido> pedidos, string usuario)
        {
            if (pedidos == null)
            {
                return BadRequest(new { message = "Datos inválidos" });
            }

            int result = await _unitOfWork.PreplanRepository.SavePedidos(pedidos, usuario);

            _unitOfWork.SaveChanges();

            if (result == 1)
            {
                return Ok(new { message = "Pedidos guardados correctamente" });
            }
            else
            {
                return StatusCode(500, new { message = "Error al guardar los pedidos" });
            }
        }

        [HttpPut("delete")]
        public async Task<IActionResult> BorrarPlan(string empresa, string fecha, string usuario)
        {
            if (string.IsNullOrEmpty(empresa) || string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { message = "Datos inválidos. Se requiere empresa, fecha y usuario." });
            }

            int result = await _unitOfWork.PreplanRepository.BorrarPlan(empresa, fecha, usuario);

            _unitOfWork.SaveChanges();

            if (result > 0)
            {
                return Ok(new { message = "Plan borrado correctamente." });
            }
            else
            {
                return StatusCode(500, new { message = "No se encontró el registro o hubo un error al borrar el plan." });
            }
        }

        [HttpGet("lugares/{codCliente}")]
        public async Task<IActionResult> GetLugares(string codCliente)
        {
            if (string.IsNullOrEmpty(codCliente))
            {
                return BadRequest(new { message = "Código de cliente es requerido." });
            }

            var lugares = await _unitOfWork.PreplanRepository.GetLugares(codCliente);

            if (lugares == null || lugares.Count == 0)
            {
                return NotFound(new { message = "No se encontraron lugares para el cliente." });
            }

            return Ok(lugares);
        }

        [HttpPut("direccion/{coddire}/{codigo}")]
        public async Task<IActionResult> UpdateDirec([FromRoute] string coddire, [FromRoute] string codigo)
        {

            if (string.IsNullOrEmpty(coddire) || string.IsNullOrEmpty(codigo))
            {
                return BadRequest(new { message = "Datos inválidos. Se requiere código de cliente y dirección." });
            }

            int filasAfectadas = await _unitOfWork.PreplanRepository.UpdateDirec(coddire, codigo);

            _unitOfWork.SaveChanges();

            if (filasAfectadas > 0)
            {
                return Ok(new { message = "Dirección actualizada correctamente" });
            }
            else
            {
                return NotFound(new { message = "No se encontró el registro para actualizar" });
            }
        }

        [HttpGet("conductores")]
        public async Task<IActionResult> GetConductores([FromQuery] string usuario)
        {
            if (string.IsNullOrEmpty(usuario))
            {
                return BadRequest(new { message = "El usuario es requerido" });
            }

            var conductores = await _unitOfWork.PreplanRepository.GetConductores(usuario);

            if (conductores == null || conductores.Count == 0)
            {
                return NotFound(new { message = "No se encontraron conductores para este usuario" });
            }

            return Ok(conductores);
        }

        [HttpGet("unidades")]
        public async Task<IActionResult> GetUnidades([FromQuery] string usuario)
        {
            try
            {
                var unidades = await _unitOfWork.PreplanRepository.GetUnidades(usuario);

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
            try
            {
                var servicios = await _unitOfWork.PreplanRepository.CreateServicios(fecha, empresa, usuario);

                _unitOfWork.SaveChanges();

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
            try
            {
                var servicios = await _unitOfWork.PreplanRepository.GetServicios(fecha, usu);

                if (servicios == null || servicios.Count == 0)
                {
                    return NotFound("No se encontraron servicios.");
                }

                return Ok(servicios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("GetPasajeros")]
        public async Task<IActionResult> GetPasajeros([FromQuery] string palabra, string codusuario)
        {
            try
            {
                var pasajeros = await _unitOfWork.PreplanRepository.GetPasajeros(palabra, codusuario);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound("No se encontraron pasajeros.");
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpGet("GetServicioPasajero")]
        public async Task<IActionResult> GetServicioPasajero([FromQuery] string usuario, [FromQuery] string fec, [FromQuery] string codcliente)
        {
            try
            {
                var pasajeros = await _unitOfWork.PreplanRepository.GetServicioPasajero(usuario, fec, codcliente);

                if (pasajeros == null || !pasajeros.Any())
                {
                    return NotFound("No se encontraron pasajeros.");
                }

                return Ok(pasajeros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPost("AsignarServicio")]
        public async Task<IActionResult> AsignarServicio([FromBody] List<Servicio> listaServicios)
        {
            if (listaServicios == null || !listaServicios.Any())
            {
                return BadRequest("La lista de servicios no puede estar vacía.");
            }

            try
            {
                string resultado = await _unitOfWork.PreplanRepository.AsignacionServicio(listaServicios);

                _unitOfWork.SaveChanges();

                if (resultado == "Servicio Asignado")
                {
                    return Ok(new { message = resultado });
                }
                else
                {
                    return NotFound(new { message = resultado });
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

            int resultado = await _unitOfWork.PreplanRepository.EliminacionMultiple(listaServicios);

            _unitOfWork.SaveChanges();

            if (resultado > 0)
            {
                return Ok(new { message = "Servicios eliminados correctamente" });
            }

            return BadRequest(new { message = "No se pudieron eliminar los servicios" });
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

            var conductores = await _unitOfWork.PreplanRepository.GetConductorDetalle(usuario);

            if (conductores == null || !conductores.Any())
            {
                return NotFound(new { mensaje = "No se encontró el conductor." });
            }

            return Ok(conductores);
        }

        [HttpGet("PasajeroList")]
        public async Task<IActionResult> ListaPasajeroServicio([FromQuery] string codservicio)
        {
            if (string.IsNullOrEmpty(codservicio))
            {
                return BadRequest(new { mensaje = "El parámetro 'codservicio' es obligatorio." });
            }

            var pasajeros = await _unitOfWork.PreplanRepository.ListaPasajeroServicio(codservicio);

            if (pasajeros == null || !pasajeros.Any())
            {
                return NotFound(new { mensaje = "No se encontraron pasajeros para el servicio." });
            }

            return Ok(pasajeros);
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
                int resultado = await _unitOfWork.PreplanRepository.UpdateControlServicio(servicio);

                _unitOfWork.SaveChanges();

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
                int filasAfectadas = await _unitOfWork.PreplanRepository.CancelarAsignacion(codservicio);

                _unitOfWork.SaveChanges();

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

            var servicio = new Servicio { Codservicio = codservicio };

            int resultado = await _unitOfWork.PreplanRepository.CancelarServicio(servicio);

            _unitOfWork.SaveChanges();

            if (resultado > 0)
            {
                return Ok(new { message = "Servicio cancelado exitosamente." });
            }
            else
            {
                return NotFound(new { message = "No se encontró el servicio a cancelar." });
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
                string resultado = await _unitOfWork.PreplanRepository.NewServicio(servicio, usuario);

                _unitOfWork.SaveChanges();

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpPut("UpdateEstado")]
        public async Task<IActionResult> UpdateEstado([FromBody] Pedido pedido)
        {
            if (pedido == null)
                return BadRequest("El pedido no puede ser nulo.");

            int resultado = await _unitOfWork.PreplanRepository.UpdateEstadoServicio(pedido);

            _unitOfWork.SaveChanges();

            if (resultado > 0)
                return Ok(new { mensaje = "Estado actualizado correctamente", filasAfectadas = resultado });

            return BadRequest("No se pudo actualizar el pedido.");
        }

        //REPORTE CARLOS GODOY
        [HttpGet("ExcelDiferencias")]
        public async Task<IActionResult> ReporteDiferencia([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea, [FromQuery] string usuario, [FromQuery] string tipo)
        {
            var feciniDT = DateTime.ParseExact(fecini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var fecfinDT = DateTime.ParseExact(fecfin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            // Formatear al estilo que necesitas: dd/MM/yyyy HH:mm
            string feciniFormateada = feciniDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            string fecfinFormateada = fecfinDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            var resultado = await _unitOfWork.PreplanRepository.ReporteDiferencia(feciniFormateada, fecfinFormateada, aerolinea, usuario, tipo);

            var excelBytes = DataExcelSheet(resultado, fecini, fecfin, aerolinea, usuario, tipo);
            string fileName = $"Resumen_{aerolinea}_{fecini}_a_{fecfin}_{tipo}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private byte[] DataExcelSheet(List<Pedido> resultado, string fecini, string fecfin, string aerolinea, string usuario, string tipo)
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
                worksheet.Cell(9, 2).Style.Font.FontName = "Cambria";
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
                worksheet.Cell("L9").Style.Font.FontName = "Cambria";
                worksheet.Cell("L9").Style.Font.FontSize = 10;
                worksheet.Cell("L9").Style.Font.SetBold();
                worksheet.Cell("L9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("L10").Value = "USUARIO : " + usuario.ToUpper();
                worksheet.Cell("L10").Style.Font.FontName = "Cambria";
                worksheet.Cell("L10").Style.Font.FontSize = 10;
                worksheet.Cell("L10").Style.Font.SetBold();
                worksheet.Cell("L10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:L10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:L10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string logo1 = "C:\\inetpub\\wwwroot\\CarLogo.jpg";
                worksheet.AddPicture(logo1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);

                var mergedRange = worksheet.Range("J4:L7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string logo2 = "C:\\inetpub\\wwwroot\\VelsatLogo.png";
                worksheet.AddPicture(logo2).MoveTo(worksheet.Cell("K4")).WithSize(240, 80).MoveTo(970, 60); //comprobar pos

                // Cabeceras
                var headers = new[]
                {"ITEM", "FECHA", "CLIENTE", "RECOJO/REPARTO", "N/SERV", "HORA TURNO", "HORA DE INICIO", "HORA LLEGADA ATO", "DIFERENCIA TIEMPO", "TIEMPO PROGRAMADO", "NOMBRES", "DIRECCIÓN", "DISTRITO", "PLACA", "CONDUCTOR"};

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


        //REPORTE EXCEL Control servicios
        [HttpGet("ServiciosExcel")]
        public async Task<IActionResult> ResumenServiciosExcel([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea, [FromQuery] string usuario)
        {
            var feciniDT = DateTime.ParseExact(fecini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var fecfinDT = DateTime.ParseExact(fecfin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            // Formatear al estilo que necesitas: dd/MM/yyyy HH:mm
            string feciniFormateada = feciniDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            string fecfinFormateada = fecfinDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            var resultado = await _unitOfWork.PreplanRepository.ReporteFormatoAvianca(feciniFormateada, fecfinFormateada, aerolinea, usuario);

            var excelBytes = ConvertDataExcel(resultado, fecini, fecfin, aerolinea, usuario);
            string fileName = $"Resumen_{aerolinea}_{fecini}_a_{fecfin}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private byte[] ConvertDataExcel(List<Pedido> resultado, string fecini, string fecfin, string aerolinea, string usuario)
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
                worksheet.Cell(9, 2).Style.Font.FontName = "Cambria";
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
                worksheet.Cell("J9").Style.Font.FontName = "Cambria";
                worksheet.Cell("J9").Style.Font.FontSize = 10;
                worksheet.Cell("J9").Style.Font.SetBold();
                worksheet.Cell("J9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("J10").Value = "USUARIO : " + usuario.ToUpper();
                worksheet.Cell("J10").Style.Font.FontName = "Cambria";
                worksheet.Cell("J10").Style.Font.FontSize = 10;
                worksheet.Cell("J10").Style.Font.SetBold();
                worksheet.Cell("J10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:J10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:J10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string logo1 = "C:\\inetpub\\wwwroot\\CarLogo.jpg";
                worksheet.AddPicture(logo1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);

                var mergedRange = worksheet.Range("J4:K7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string logo2 = "C:\\inetpub\\wwwroot\\VelsatLogo.png";
                worksheet.AddPicture(logo2).MoveTo(worksheet.Cell("J4")).WithSize(240, 80).MoveTo(820, 60); //comprobar pos

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
                fecini = DateTime.Parse(fecini).ToString("dd/MM/yyyy") + " 00:00";
                fecfin = DateTime.Parse(fecfin).ToString("dd/MM/yyyy") + " 23:59";

                var resultado = await _unitOfWork.PreplanRepository.ReporteFormatoAremys(fecini, fecfin, aerolinea);

                if (resultado == null || resultado.Count == 0)
                {
                    return NotFound("No se encontraron pedidos.");
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("ResumenExcelAremys")]
        public async Task<IActionResult> ResumenExcelAremys([FromQuery] string fecini, [FromQuery] string fecfin, [FromQuery] string aerolinea)
        {
            var feciniDT = DateTime.ParseExact(fecini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var fecfinDT = DateTime.ParseExact(fecfin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            // Formatear al estilo que necesitas: dd/MM/yyyy HH:mm
            string feciniFormateada = feciniDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            string fecfinFormateada = fecfinDT.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            var resultado = await _unitOfWork.PreplanRepository.ReporteFormatoAremys(feciniFormateada, fecfinFormateada, aerolinea);

            var excelBytes = ConvertDataExcelAremys(resultado, fecini, fecfin, aerolinea);
            string fileName = $"Resumen_{aerolinea}_{fecini}_a_{fecfin}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private byte[] ConvertDataExcelAremys(List<Pedido> resultado, string fecini, string fecfin, string aerolinea)
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
                worksheet.Cell(9, 2).Style.Font.FontName = "Cambria";
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
                worksheet.Cell("K9").Style.Font.FontName = "Cambria";
                worksheet.Cell("K9").Style.Font.FontSize = 10;
                worksheet.Cell("K9").Style.Font.SetBold();
                worksheet.Cell("K9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("K10").Value = "USUARIO : " + usuario;
                worksheet.Cell("K10").Style.Font.FontName = "Cambria";
                worksheet.Cell("K10").Style.Font.FontSize = 10;
                worksheet.Cell("K10").Style.Font.SetBold();
                worksheet.Cell("K10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:K10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:K10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string logo1 = "C:\\inetpub\\wwwroot\\CarLogo.jpg";
                worksheet.AddPicture(logo1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);

                var mergedRange = worksheet.Range("L4:M7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string logo2 = "C:\\inetpub\\wwwroot\\VelsatLogo.png";
                worksheet.AddPicture(logo2).MoveTo(worksheet.Cell("L4")).WithSize(240, 80).MoveTo(800, 60);

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
            try
            {
                int result = await _unitOfWork.PreplanRepository.RegistrarPasajeroGrupo(pedido, usuario);

                _unitOfWork.SaveChanges();

                if (result > 0)
                    return Ok(new { mensaje = "Pasajero registrado correctamente", filasAfectadas = result });

                return BadRequest(new { mensaje = "No se pudo registrar el pasajero" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno", detalle = ex.Message });
            }
        }

        [HttpPut("UpdateHoras")]
        public async Task<IActionResult> UpdateHorasServicio([FromQuery] string codservicio, [FromQuery] string fecha, [FromQuery] string fecplan)
        {
            try
            {
                int result = await _unitOfWork.PreplanRepository.UpdateHorasServicio(codservicio, fecha, fecplan);

                _unitOfWork.SaveChanges();

                if (result > 0)
                    return Ok("Servicio actualizado correctamente.");
                else
                    return NotFound("No se encontró el servicio con el código proporcionado.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar el servicio: {ex.Message}");
            }
        }

        [HttpPut("UpdateDestino")]
        public async Task<IActionResult> UpdateDestinoServicio([FromQuery] string codservicio, [FromQuery] string newcoddestino, [FromQuery] string newcodubicli)
        {
            try
            {
                int result = await _unitOfWork.PreplanRepository.UpdateDestinoServicio(codservicio, newcoddestino, newcodubicli);

                _unitOfWork.SaveChanges();

                if (result > 0)
                    return Ok("Servicio actualizado correctamente.");
                else
                    return NotFound("No se encontró el servicio con el código proporcionado.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar el servicio: {ex.Message}");
            }
        }

        [HttpGet("GetDestinos")]
        public async Task<IActionResult> GetDestinos([FromQuery] string palabra)
        {
            try
            {
                var destinos = await _unitOfWork.PreplanRepository.GetDestinos(palabra);

                if (destinos == null || !destinos.Any())
                {
                    return NotFound("No se encontraron destinos.");
                }

                return Ok(destinos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hubo un error al procesar la solicitud.");
            }
        }

        [HttpPut("GrupoCero")]
        public async Task<IActionResult> EliminarGrupoCero([FromQuery] string usuario)
        {
            try
            {
                int result = await _unitOfWork.PreplanRepository.EliminarGrupoCero(usuario);

                _unitOfWork.SaveChanges();

                if (result > 0)
                    return Ok("Grupo eliminado correctamente.");
                else
                    return NotFound("No se encontraron registros para eliminar.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el grupo: {ex.Message}");
            }
        }

        [HttpGet("conductores/{usuario}")]
        public async Task<ActionResult<List<Conductor>>> GetConductoresByUsuario(string usuario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest("El parámetro usuario es requerido");
                }

                var conductores = await _unitOfWork.PreplanRepository.GetConductoresxUsuario(usuario);

                if (conductores == null || !conductores.Any())
                {
                    return NotFound("No se encontraron conductores para el usuario especificado");
                }

                return Ok(conductores);
            }
            catch (Exception ex)
            {
                // Log del error aquí si tienes un logger configurado
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("carros/{usuario}")]
        public async Task<ActionResult<List<Carro>>> GetUnidadesxUsuario(string usuario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest("El parámetro usuario es requerido");
                }

                var carros = await _unitOfWork.PreplanRepository.GetUnidadesxUsuario(usuario);

                if (carros == null || !carros.Any())
                {
                    return NotFound("No se encontraron carros para el usuario especificado");
                }

                return Ok(carros);
            }
            catch (Exception ex)
            {
                // Log del error aquí si tienes un logger configurado
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("NuevoConductor/{usuario}")]
        public async Task<IActionResult> GuardarConductor([FromBody] Conductor conductor, string usuario)
        {
            var resultado = await _unitOfWork.PreplanRepository.GuardarConductorAsync(conductor, usuario);

            _unitOfWork.SaveChanges();

            return resultado switch
            {
                0 => BadRequest("Error al guardar conductor"),
                1 => Ok("Conductor guardado exitosamente"),
                2 => Conflict("El conductor ya existe"),
                _ => StatusCode(500, "Error interno")
            };
        }

        [HttpPut("ModificarConductor/{id}")]
        public async Task<IActionResult> ModificarConductor(int id, [FromBody] Conductor conductor)
        {
            try
            {
                // Ejecutar la modificación
                var resultado = await _unitOfWork.PreplanRepository.ModificarConductorAsync(conductor);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Conductor no encontrado o no se pudo modificar", code = 0 }),
                    1 => Ok(new { message = "Conductor modificado exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                // Log del error si tienes logger
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("HabilitarCond/{id}")]
        public async Task<IActionResult> HabilitarConductor(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Código de conductor inválido" });

                var resultado = await _unitOfWork.PreplanRepository.HabilitarConductorAsync(id);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Conductor no encontrado", code = 0 }),
                    1 => Ok(new { message = "Conductor habilitado exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("DeshabilitarCond/{id}")]
        public async Task<IActionResult> DeshabilitarConductor(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Código de conductor inválido" });

                var resultado = await _unitOfWork.PreplanRepository.DeshabilitarConductorAsync(id);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Conductor no encontrado", code = 0 }),
                    1 => Ok(new { message = "Conductor deshabilitado exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("Liberar/{id}")]
        public async Task<IActionResult> LiberarConductor(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Código de conductor inválido" });

                var resultado = await _unitOfWork.PreplanRepository.LiberarConductorAsync(id);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Conductor no encontrado", code = 0 }),
                    1 => Ok(new { message = "Conductor liberado exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> EliminarConductorDelete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Código de conductor inválido" });

                var resultado = await _unitOfWork.PreplanRepository.EliminarConductorAsync(id);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Conductor no encontrado", code = 0 }),
                    1 => Ok(new { message = "Conductor eliminado exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("HabilitarUnidad/{placa}")]
        public async Task<IActionResult> HabilitarUnidad(string placa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placa))
                    return BadRequest(new { message = "La placa de la unidad es requerida" });

                var resultado = await _unitOfWork.PreplanRepository.HabilitarUnidadAsync(placa);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Unidad no encontrada", code = 0 }),
                    1 => Ok(new { message = "Unidad habilitada exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("DeshabilitarUnidad/{placa}")]
        public async Task<IActionResult> DeshabilitarUnidad(string placa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placa))
                    return BadRequest(new { message = "La placa de la unidad es requerida" });

                var resultado = await _unitOfWork.PreplanRepository.DeshabilitarUnidadAsync(placa);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Unidad no encontrada", code = 0 }),
                    1 => Ok(new { message = "Unidad deshabilitada exitosamente", code = 1 }),
                    _ => StatusCode(500, new { message = "Error interno", code = -1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("LiberarUnidad/{placa}")]
        public async Task<IActionResult> LiberarUnidad(string placa)
        {
            try
            {
                var resultado = await _unitOfWork.PreplanRepository.LiberarUnidadAsync(placa);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Unidad no encontrada o no se pudo liberar", code = 0 }),
                    1 => Ok(new { message = "Unidad liberada exitosamente", code = 1 }),
                    _ => Ok(new { message = $"{resultado} unidades liberadas exitosamente", code = 1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("ActualizarDireccionPasajero/{codpedido}/{codubicli}")]
        public async Task<IActionResult> UpdDirPasServicio(int codpedido, string codubicli)
        {
            try
            {
                var resultado = await _unitOfWork.PreplanRepository.UpdDirPasServicio(codpedido, codubicli);

                _unitOfWork.SaveChanges();

                return resultado switch
                {
                    0 => NotFound(new { message = "Pedido no encontrado o no se pudo actualizar la dirección", code = 0 }),
                    1 => Ok(new { message = "Dirección del servicio actualizada exitosamente", code = 1 }),
                    _ => Ok(new { message = $"{resultado} servicios actualizados exitosamente", code = 1 })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("DireccionAdicional")]
        public async Task<IActionResult> CrearLugarCliente([FromBody] LugarCliente lugarCliente)
        {
            if (lugarCliente == null)
            {
                return BadRequest(new { message = "Los datos del lugar cliente son requeridos." });
            }

            try
            {
                int filasAfectadas = await _unitOfWork.PreplanRepository.NuevoLugarCliente(lugarCliente);

                _unitOfWork.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new
                    {
                        message = "Dirección adiocional creada exitosamente.",
                        filasAfectadas = filasAfectadas
                    });
                }
                else
                {
                    return BadRequest(new { message = "No se pudo crear la dirección." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno del servidor al crear la dirección.",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("EliminarDireccion")]
        public async Task<IActionResult> EliminarLugarCliente(int codlugar)
        {
            if (codlugar <= 1)
            {
                return BadRequest(new { message = "Código de lugar inválido." });
            }

            try
            {
                int filasAfectadas = await _unitOfWork.PreplanRepository.EliminarLugarCliente(codlugar);

                _unitOfWork.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new
                    {
                        message = "Dirección eliminada exitosamente.",
                        filasAfectadas = filasAfectadas
                    });
                }
                else
                {
                    return NotFound(new { message = "No se encontró la dirección a eliminar." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno del servidor al eliminar la dirección.",
                    error = ex.Message
                });
            }
        }
    }
}
