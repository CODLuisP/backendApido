using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.KmServicioAremys;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KServicioController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public KServicioController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("kilometraje")]
        public async Task<IActionResult> GetKmServicios([FromQuery] string fecha)
        {
            if (string.IsNullOrEmpty(fecha))
                return BadRequest("Debe ingresar una fecha.");

            try
            {
                var resultado = await _unitOfWork.KmServicioRepository.GetKmServicios(fecha);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Podrías loguear el error aquí si usas ILogger
                return StatusCode(500, $"Ocurrió un error al obtener los datos: {ex.Message}");
            }
        }

        [HttpGet("ExcelKmServicios")]
        public async Task<IActionResult> ResumenExcelKm([FromQuery] string fecha)
        {
            var resultado = await _unitOfWork.KmServicioRepository.GetKmServicios(fecha);

            var excelBytes = ConvertDataExcel(resultado, fecha);
            string fileName = $"Kilometros_Servicios_Aremys_{fecha}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private byte[] ConvertDataExcel(List<KilometrajeServicio> resultado, string fecha)
        {
            var fechaInicio = $"{fecha} 00:00";
            var fechaFin = $"{fecha} 23:59";

            using (var workbook = new XLWorkbook())
            {
                var usuario = "AREMYS";
                var worksheet = workbook.Worksheets.Add("Servicios");

                // Título principal
                var rangoTitulo = worksheet.Range("B4:G7");
                rangoTitulo.Merge();
                rangoTitulo.Value = "KILÓMETROS RECORRIDOS POR SERVICIO";
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

                worksheet.Range("D9:E9").Merge();
                worksheet.Cell(9, 4).Value = fechaInicio;
                worksheet.Cell(9, 4).Style = worksheet.Cell(9, 2).Style;

                // Fecha de Fin
                worksheet.Range("B10:C10").Merge();
                worksheet.Cell(10, 2).Value = "Fecha de Fin:";
                worksheet.Cell(10, 2).Style = worksheet.Cell(9, 2).Style;

                worksheet.Range("D10:E10").Merge();
                worksheet.Cell(10, 4).Value = fechaFin;
                worksheet.Cell(10, 4).Style = worksheet.Cell(9, 4).Style;

                // Fecha y usuario
                worksheet.Cell("H9").Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cell("H9").Style.Font.FontName = "Cambria";
                worksheet.Cell("H9").Style.Font.FontSize = 10;
                worksheet.Cell("H9").Style.Font.SetBold();
                worksheet.Cell("H9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell("H10").Value = "USUARIO : " + usuario;
                worksheet.Cell("H10").Style.Font.FontName = "Cambria";
                worksheet.Cell("H10").Style.Font.FontSize = 10;
                worksheet.Cell("H10").Style.Font.SetBold();
                worksheet.Cell("H10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range("B10:E10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("B10:E10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");
                worksheet.Range("F10:H10").Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                worksheet.Range("F10:H10").Style.Border.BottomBorderColor = XLColor.FromHtml("#1a3446");

                // Imágenes
                string logo1 = "C:\\inetpub\\wwwroot\\CarLogo.jpg";
                worksheet.AddPicture(logo1).MoveTo(worksheet.Cell("B4")).WithSize(81, 81);

                var mergedRange = worksheet.Range("H4:H7");
                mergedRange.Merge();
                mergedRange.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(224, 224, 224));
                mergedRange.Merge().Style.Alignment.WrapText = true;
                mergedRange.Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                mergedRange.Merge().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string logo2 = "C:\\inetpub\\wwwroot\\VelsatLogo.png";
                worksheet.AddPicture(logo2).MoveTo(worksheet.Cell("H4")).WithSize(240, 80).MoveTo(740, 60);

                // Cabeceras
                var headers = new[]
                {"ITEM", "FECHA", "CÓDIGO SERVICIO", "TIPO", "FECHA INICIAL", "FECHA FINAL", "CONDUCTOR", "UNIDAD", "EMPRESA", "KILÓMETROS RECORRIDOS"};

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
                worksheet.Column(4).Width = 17;
                worksheet.Column(5).Width = 10;
                worksheet.Column(6).Width = 17;
                worksheet.Column(7).Width = 17;
                worksheet.Column(8).Width = 50;
                worksheet.Column(9).Width = 15;
                worksheet.Column(10).Width = 10;
                worksheet.Column(11).Width = 26;


                worksheet.Column(18).Style.Alignment.WrapText = true;
                worksheet.ShowGridLines = false;

                int fila = 13;
                int item = 1;

                foreach (var servicio in resultado)
                {
                    var cell = worksheet.Cell(fila, 4);
                    cell.Value = servicio.Codservicio;
                    cell.Style.Alignment.WrapText = true;
                    worksheet.Row(fila).AdjustToContents();

                    var cell2 = worksheet.Cell(fila, 11);
                    cell2.Value = servicio.Codservicio;
                    cell2.Style.Alignment.WrapText = true;
                    worksheet.Row(fila).AdjustToContents();

                    worksheet.Cell(fila, 2).Value = item++;
                    worksheet.Cell(fila, 3).Value = fecha;
                    worksheet.Cell(fila, 4).Value = servicio.Codservicio;
                    worksheet.Cell(fila, 5).Value = servicio.Tipo.ToUpper() ?? "";
                    worksheet.Cell(fila, 6).Value = servicio.Fechaini ?? "";
                    worksheet.Cell(fila, 7).Value = servicio.Fechafin ?? "";
                    worksheet.Cell(fila, 8).Value = servicio.NombreConductor ?? "";
                    worksheet.Cell(fila, 9).Value = servicio.Unidad.ToUpper() ?? "";
                    worksheet.Cell(fila, 10).Value = servicio.Empresa ?? "";
                    worksheet.Cell(fila, 11).Value = (servicio.KilometrosRecorridos == null || servicio.KilometrosRecorridos == 0) ? "-" : $"{Math.Round(servicio.KilometrosRecorridos.Value, 2)} Km/h";


                    fila++;
                }

                // Aplicar estilos de centrado y color intercalado desde fila 13 hacia abajo
                int colInicio = 2; // Columna B
                int colFin = 11;   // Última columna según tu estructura

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
    }
}
