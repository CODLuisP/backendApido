using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Administracion;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using System.Diagnostics;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        public AdminController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        [HttpGet("Usuarios")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _readOnlyUow.AdminRepository.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los usuarios", error = ex.Message });
            }
        }

        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] Usuarioadmin usuario)
        {
            try
            {
                if (usuario == null)
                {
                    return BadRequest(new { message = "El usuario no puede ser nulo" });
                }

                var rowsAffected = await _uow.AdminRepository.UpdateUser(usuario);

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }
                _uow.SaveChanges();

                return Ok(new { message = "Usuario actualizado correctamente", rowsAffected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el usuario", error = ex.Message });
            }
        }

        [HttpDelete("DeleteUsuario/{accountID}")]
        public async Task<IActionResult> DeleteUser(string accountID)
        {
            try
            {
                if (string.IsNullOrEmpty(accountID))
                {
                    return BadRequest(new { message = "El accountID no puede ser nulo o vacío" });
                }

                var rowsAffected = await _uow.AdminRepository.DeleteUser(accountID);

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Usuario eliminado correctamente", accountID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar el usuario", error = ex.Message });
            }
        }

        [HttpPost("InsertUsuario")]
        public async Task<IActionResult> InsertUser([FromBody] Usuarioadmin usuario)
        {
            try
            {
                if (usuario == null)
                {
                    return BadRequest(new { message = "El usuario no puede ser nulo" });
                }

                if (string.IsNullOrEmpty(usuario.AccountID) || string.IsNullOrEmpty(usuario.Password))
                {
                    return BadRequest(new { message = "AccountID y Password son obligatorios" });
                }

                var rowsAffected = await _uow.AdminRepository.InsertUser(usuario);

                if (rowsAffected == 0)
                {
                    return StatusCode(500, new { message = "No se pudo insertar el usuario" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Usuario creado correctamente", accountID = usuario.AccountID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el usuario", error = ex.Message });
            }
        }

        [HttpGet("SubUsuarios")]
        public async Task<IActionResult> GetSubUsers()
        {
            try
            {
                var users = await _readOnlyUow.AdminRepository.GetSubUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los subusuarios", error = ex.Message });
            }
        }

        [HttpPost("InsertDeviceUser")]
        public async Task<IActionResult> InsertSubUser([FromBody] Deviceuser usuario)
        {
            try
            {
                if (usuario == null)
                {
                    return BadRequest(new { message = "El device user no puede ser nulo" });
                }

                var rowsAffected = await _uow.AdminRepository.InsertSubUser(usuario);

                if (rowsAffected == 0)
                {
                    return StatusCode(500, new { message = "No se pudo insertar el device user" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Device user creado correctamente", id = usuario.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el device user", error = ex.Message });
            }
        }

        [HttpPut("UpdateDeviceUser")]
        public async Task<IActionResult> UpdateSubUser([FromBody] Deviceuser usuario)
        {
            try
            {
                if (usuario == null)
                {
                    return BadRequest(new { message = "El device user no puede ser nulo" });
                }

                var rowsAffected = await _uow.AdminRepository.UpdateSubUser(usuario);

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Device user no encontrado" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Device user actualizado correctamente", rowsAffected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el device user", error = ex.Message });
            }
        }

        [HttpDelete("DeleteDeviceUser/{id}")]
        public async Task<IActionResult> DeleteSubUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "El id no puede ser nulo o vacío" });
                }

                var rowsAffected = await _uow.AdminRepository.DeleteSubUser(id);

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Device user no encontrado" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Device user eliminado correctamente", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar el device user", error = ex.Message });
            }
        }

        [HttpGet("GetDevices")]
        public async Task<IActionResult> GetDevices()
        {
            try
            {
                var devices = await _readOnlyUow.AdminRepository.GetDevices();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las unidades", error = ex.Message });
            }
        }

        [HttpPut("UpdateDevice")]
        public async Task<IActionResult> UpdateDevice([FromBody] DeviceAdmin device, string oldDeviceID, string oldAccountID)
        {
            try
            {
                var resultado = await _uow.AdminRepository.UpdateDevice(device, oldDeviceID, oldAccountID);

                if (resultado == 0)
                {
                    return NotFound(new { message = "Dispositivo no encontrado" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Dispositivo actualizado correctamente", rowsAffected = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el dispositivo", error = ex.Message });
            }
        }

        [HttpPost("InsertDevice")]
        public async Task<IActionResult> InsertDevice([FromBody] DeviceAdmin device)
        {
            try
            {
                var resultado = await _uow.AdminRepository.InsertDevice(device);

                if (resultado == 0)
                {
                    return BadRequest(new { message = "No se pudo crear el dispositivo" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Dispositivo creado correctamente", rowsAffected = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el dispositivo", error = ex.Message });
            }
        }

        [HttpDelete("DeleteDevice/{deviceID}/{accountID}")]
        public async Task<IActionResult> DeleteDevice(string deviceID, string accountID)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceID) || string.IsNullOrEmpty(accountID))
                {
                    return BadRequest(new { message = "El deviceID y accountID no pueden ser nulos o vacíos" });
                }

                var rowsAffected = await _uow.AdminRepository.DeleteDevice(deviceID, accountID);

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Dispositivo no encontrado" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Dispositivo eliminado correctamente", deviceID, accountID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar el dispositivo", error = ex.Message });
            }
        }

        [HttpGet("GetDevicesConex")]
        public async Task<IActionResult> GetConexDesconex()
        {
            try
            {
                var devices = await _readOnlyUow.AdminRepository.GetConexDesconex();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las unidades", error = ex.Message });
            }
        }

        [HttpGet("GetUnidadesSutran")]
        public async Task<IActionResult> GetUnidadesSutran()
        {
            try
            {
                var devices = await _readOnlyUow.AdminRepository.GetUnidadesSutran();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las unidades Sutran", error = ex.Message });
            }
        }

        [HttpPut("HabilitarSutran")]
        public async Task<IActionResult> HabilitarSutran(string accountID, string deviceID, char valor)
        {
            try
            {
                if (string.IsNullOrEmpty(accountID) || string.IsNullOrEmpty(deviceID))
                {
                    return BadRequest(new { message = "El accountID y deviceID no pueden ser nulos o vacíos" });
                }

                var rowsAffected = await _uow.AdminRepository.HabilitarSutran(accountID, deviceID, valor);

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Dispositivo no encontrado" });
                }

                _uow.SaveChanges();

                return Ok(new { message = "Sutran actualizado correctamente", accountID, deviceID, valor });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar Sutran", error = ex.Message });
            }
        }

        [HttpGet("GetAuditoriaSutran")]
        public async Task<IActionResult> GetUltimosRegistrosAuditoriaSutran(string accountID, string deviceID)
        {
            try
            {
                if (string.IsNullOrEmpty(accountID) || string.IsNullOrEmpty(deviceID))
                {
                    return BadRequest(new { message = "El accountID y deviceID no pueden ser nulos o vacíos" });
                }

                var registros = await _readOnlyUow.AdminRepository.GetUltimosRegistrosAuditoriaSutran(accountID, deviceID);
                return Ok(registros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la auditoría UTRAN", error = ex.Message });
            }
        }

        // MEMBRETE
        // Recibe un Excel de reporte (como el generado por ReportingController), le quita la columna A y las
        // filas 1-2, recentra el logo de VELSAT en la celda combinada resultante, fija el área de impresión
        // en A4 vertical a 1 página de ancho, y devuelve el Word (docx) ya listo para agregarle encabezado/pie
        // de página (equivalente a pasar por PDF + iLovePDF, pero en un solo paso).
        [HttpPost("GenerarMembreteWord")]
        [RequestSizeLimit(20_000_000)]
        public IActionResult GenerarMembreteWord(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(new { message = "Debe adjuntar el archivo Excel" });
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                return BadRequest(new { message = "El archivo debe ser un .xlsx" });
            }

            try
            {
                byte[] excelConMembrete;
                using (var input = archivo.OpenReadStream())
                {
                    excelConMembrete = AplicarFormatoMembrete(input);
                }

                var docxBytes = ConvertirExcelAWord(excelConMembrete);
                var nombreSalida = Path.GetFileNameWithoutExtension(archivo.FileName) + "_membrete.docx";

                return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", nombreSalida);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar el membrete", error = ex.Message });
            }
        }

        private static byte[] AplicarFormatoMembrete(Stream inputXlsx)
        {
            byte[] editedBytes;
            int targetColZeroBased, targetRowZeroBased, mergeWidthPx, mergeHeightPx;

            using (var workbook = new XLWorkbook(inputXlsx))
            {
                var worksheet = workbook.Worksheets.First();

                // El bloque de encabezado (título + placeholder del logo) siempre vive en las filas 3-8
                var headerMerges = worksheet.MergedRanges
                    .Where(r => r.RangeAddress.FirstAddress.RowNumber >= 3 && r.RangeAddress.FirstAddress.RowNumber <= 8)
                    .ToList();

                if (headerMerges.Count < 2)
                {
                    throw new InvalidOperationException("El Excel no tiene la estructura de encabezado esperada (título + logo).");
                }

                var tituloMerge = headerMerges.OrderByDescending(m => m.ColumnCount()).First();
                var logoMergeAntes = headerMerges.First(m => m != tituloMerge);

                worksheet.Column(1).Delete();
                worksheet.Rows(1, 2).Delete();

                var headerMergesDespues = worksheet.MergedRanges
                    .Where(r => r.RangeAddress.FirstAddress.RowNumber >= 1 && r.RangeAddress.FirstAddress.RowNumber <= 6)
                    .ToList();
                var tituloMergeDespues = headerMergesDespues.OrderByDescending(m => m.ColumnCount()).First();
                var logoMergeDespues = headerMergesDespues.First(m => m != tituloMergeDespues);

                targetColZeroBased = logoMergeDespues.RangeAddress.FirstAddress.ColumnNumber - 1;
                targetRowZeroBased = logoMergeDespues.RangeAddress.FirstAddress.RowNumber - 1;

                mergeWidthPx = 0;
                for (int c = logoMergeDespues.RangeAddress.FirstAddress.ColumnNumber; c <= logoMergeDespues.RangeAddress.LastAddress.ColumnNumber; c++)
                    mergeWidthPx += ColumnWidthPixels(worksheet, c);

                mergeHeightPx = 0;
                for (int r = logoMergeDespues.RangeAddress.FirstAddress.RowNumber; r <= logoMergeDespues.RangeAddress.LastAddress.RowNumber; r++)
                    mergeHeightPx += RowHeightPixels(worksheet, r);

                worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
                worksheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                worksheet.PageSetup.FitToPages(1, 0);
                worksheet.PageSetup.PrintAreas.Clear();
                worksheet.PageSetup.PrintAreas.Add(worksheet.RangeUsed()!.RangeAddress.ToString());

                using var msOut = new MemoryStream();
                workbook.SaveAs(msOut);
                editedBytes = msOut.ToArray();
            }

            // ClosedXML no reposiciona correctamente las imágenes "FreeFloating" (posición absoluta) al borrar
            // filas/columnas, así que el logo se recentra manipulando el XML del dibujo directamente y
            // convirtiéndolo a un anclaje de celda (se mueve solo si el reporte cambia en el futuro).
            const int emuPorPixel = 9525;
            using (var msEdit = new MemoryStream(editedBytes))
            {
                using (var doc = SpreadsheetDocument.Open(msEdit, true))
                {
                    var worksheetPart = doc.WorkbookPart!.WorksheetParts.First();
                    var drawingsPart = worksheetPart.DrawingsPart;
                    if (drawingsPart != null)
                    {
                        var worksheetDrawing = drawingsPart.WorksheetDrawing;
                        var anclasAbsolutas = worksheetDrawing.Elements<Xdr.AbsoluteAnchor>().ToList();

                        foreach (var ancla in anclasAbsolutas)
                        {
                            var extent = (Xdr.Extent)ancla.Extent!.CloneNode(true);
                            var picture = (Xdr.Picture)ancla.GetFirstChild<Xdr.Picture>()!.CloneNode(true);
                            var clientData = (Xdr.ClientData)ancla.GetFirstChild<Xdr.ClientData>()!.CloneNode(true);

                            int picWidthPx = (int)(extent.Cx!.Value / emuPorPixel);
                            int picHeightPx = (int)(extent.Cy!.Value / emuPorPixel);
                            int offsetXpx = Math.Max(0, (mergeWidthPx - picWidthPx) / 2);
                            int offsetYpx = Math.Max(0, (mergeHeightPx - picHeightPx) / 2);

                            var nuevaAncla = new Xdr.OneCellAnchor(
                                new Xdr.FromMarker
                                {
                                    ColumnId = new Xdr.ColumnId(targetColZeroBased.ToString()),
                                    ColumnOffset = new Xdr.ColumnOffset((offsetXpx * emuPorPixel).ToString()),
                                    RowId = new Xdr.RowId(targetRowZeroBased.ToString()),
                                    RowOffset = new Xdr.RowOffset((offsetYpx * emuPorPixel).ToString())
                                },
                                extent,
                                picture,
                                clientData
                            );

                            worksheetDrawing.ReplaceChild(nuevaAncla, ancla);
                        }

                        worksheetDrawing.Save();
                    }
                }
                editedBytes = msEdit.ToArray();
            }

            return editedBytes;
        }

        private static int ColumnWidthPixels(IXLWorksheet worksheet, int column)
        {
            double width = worksheet.Column(column).Width;
            return (int)(((256 * width + 18) / 256) * 7);
        }

        private static int RowHeightPixels(IXLWorksheet worksheet, int row)
        {
            double height = worksheet.Row(row).Height;
            return (int)Math.Round(height * 96 / 72.0);
        }

        // xlsx -> pdf -> docx, igual que el proceso manual (Excel > Guardar como PDF > iLovePDF > Word),
        // pero encadenando las dos conversiones con LibreOffice headless en el mismo servidor.
        private byte[] ConvertirExcelAWord(byte[] xlsxBytes)
        {
            var libreOfficePath = Environment.GetEnvironmentVariable("LIBREOFFICE_PATH") ?? "soffice";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var xlsxPath = Path.Combine(tempDir, "membrete.xlsx");
                System.IO.File.WriteAllBytes(xlsxPath, xlsxBytes);

                var pdfPath = Path.Combine(tempDir, "membrete.pdf");
                EjecutarSoffice(libreOfficePath, tempDir, "pdf", xlsxPath, pdfPath);

                var docxPath = Path.Combine(tempDir, "membrete.docx");
                EjecutarSoffice(libreOfficePath, tempDir, "docx", pdfPath, docxPath);

                return System.IO.File.ReadAllBytes(docxPath);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* limpieza best-effort */ }
            }
        }

        private static void EjecutarSoffice(string libreOfficePath, string tempDir, string formatoDestino, string archivoOrigen, string archivoEsperado)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = libreOfficePath,
                Arguments = $"--headless --norestore --convert-to {formatoDestino} --outdir \"{tempDir}\" \"{archivoOrigen}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo)!;
            if (!process.WaitForExit(60_000))
            {
                process.Kill(true);
                throw new TimeoutException($"La conversión a {formatoDestino} (LibreOffice) tardó demasiado.");
            }

            if (!System.IO.File.Exists(archivoEsperado))
            {
                var stderr = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"LibreOffice no generó el archivo {formatoDestino}: {stderr}");
            }
        }
    }
}
