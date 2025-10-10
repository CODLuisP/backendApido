using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.AlarmasCorreo;

namespace VelsatBackendAPI.Data.Services
{
    public class AlertaCorreoService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private DateTime _ultimaActualizacion = DateTime.MinValue;

        public AlertaCorreoService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var repo = unitOfWork.AlertaRepository;

                    var fecha = await repo.ObtenerFechaUltimaAlarmaAsync();
                    Console.WriteLine($"Fecha última alarma detectada: {fecha}");

                    if (fecha.HasValue && fecha > _ultimaActualizacion)
                    {
                        _ultimaActualizacion = fecha.Value;

                        var alertas = await repo.ObtenerAlertasNoEnviadasAsync();
                        Console.WriteLine($"Se encontraron {alertas.Count} alertas no enviadas.");

                        if (alertas.Any())
                        {
                            foreach (var alerta in alertas)
                            {
                                Console.WriteLine($"Enviando correo para la alerta {alerta.Codigo} - StatusCode: {alerta.StatusCode}");

                                try
                                {
                                    await EnviarCorreoAsync("rentaautoschiclayo@gmail.com", alerta);
                                    Console.WriteLine($"Correo enviado exitosamente para la alerta {alerta.Codigo}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error enviando correo para alerta {alerta.Codigo}: {ex.Message}");
                                    // Continuar con las demás alertas aunque una falle
                                }
                            }

                            // ✅ Marcar como enviadas y hacer commit
                            await repo.MarcarComoEnviadasAsync(alertas.Select(a => a.Codigo).ToList());
                            unitOfWork.SaveChanges(); // ✅ AGREGAR ESTO

                            Console.WriteLine($"{alertas.Count} alertas marcadas como procesadas.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en el servicio de alertas: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        private async Task EnviarCorreoAsync(string correo, RegistroAlarmas alerta)
        {
            using var smtp = new SmtpClient("mail.velsat.com.pe")
            {
                Port = 587,
                Credentials = new NetworkCredential("cmyg@velsat.com.pe", "E&=z47Xp4k=N"),
                EnableSsl = true
            };

            string tituloAlerta = alerta.StatusCode switch
            {
                64787 => "🚨 Alerta! Desconexión de Batería",
                63553 => "🚨 Alerta! Botón de Pánico",
                _ => "🚨 Alerta! Evento Desconocido"
            };

            var mail = new MailMessage(
                new MailAddress("cmyg@velsat.com.pe", "Velsat SAC"),
                new MailAddress(correo))
            {
                Subject = tituloAlerta,
                Body = GenerarCuerpoCorreo(alerta),
                IsBodyHtml = true
            };

            mail.To.Add("cmyg@velsat.com.pe");

            await smtp.SendMailAsync(mail);
        }

        private string GenerarCuerpoCorreo(RegistroAlarmas alerta)
        {
            DateTime fechaHora = DateTimeOffset.FromUnixTimeSeconds(alerta.Timestamp).ToLocalTime().DateTime;
            var fecha = fechaHora.ToString("dd/MM/yyyy");
            var hora = fechaHora.ToString("HH:mm:ss");

            string tituloAlerta;
            string imagenAlerta;

            switch (alerta.StatusCode)
            {
                case 64787:
                    tituloAlerta = "DESCONEXIÓN DE BATERÍA";
                    imagenAlerta = "https://res.cloudinary.com/dyc4ik1ko/image/upload/bateria_c59x4t.jpg";
                    break;
                case 63553:
                    tituloAlerta = "BOTÓN DE PÁNICO";
                    imagenAlerta = "https://res.cloudinary.com/dyc4ik1ko/image/upload/panico_i540gn.jpg";
                    break;
                default:
                    tituloAlerta = "ALERTA DESCONOCIDA";
                    imagenAlerta = "https://res.cloudinary.com/dyc4ik1ko/image/upload/bateria_c59x4t.jpg";
                    break;
            }

            return $@"
    <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    color: #333;
                    margin: 0;
                    padding: 0;
                    word-wrap: break-word;
                }}
                .container {{
                    width: 100%;
                    max-width: 600px;
                    margin: auto;
                    background-color: #f4f4f4;
                }}
                .body {{
                    background-color: white;
                    border-radius: 0 0 8px 8px;
                }}
            </style>
        </head>

        <body>
            <div class='container'>
                <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff; padding: 20px; text-align: center;'>
                    <tr>
                        <td style='padding-bottom: 10px;'>
                            <img src='https://res.cloudinary.com/dyc4ik1ko/image/upload/velsatLogo_n8ovrs.jpg' alt='Logo Velsat' style='max-width: 170px; height: auto;' />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <h2 style='margin: 0; font-size: 14px; color: #001d3d;'>CENTRAL DE MONITOREO Y GESTIÓN</h2>
                        </td>
                    </tr>
                </table>

                <div class='body'>
                    <div style='display: flex; align-items: center; justify-content: space-between; background-color: #f4f4f4; padding: 20px;'>
                        <div style='width: 60%;'>
                            <p style='font-size: 11px; color: #001d3d; font-weight: bold; margin: 0; text-transform: uppercase; margin-bottom: 10px;'>Alerta detectada en su unidad {alerta.DeviceID}</p>

                            <div style='margin-top: 20px;'>
                                <h2 style='font-size: 16px; color: #d00000; margin: 5px 0 10px 0;'>{tituloAlerta}</h2>
                                <p style='margin: 3px 0; font-size: 11px;'><strong>Fecha:</strong> {fecha}</p>
                                <p style='margin: 3px 0; font-size: 11px;'><strong>Hora:</strong> {hora}</p>
                                <p style='margin: 3px 0; font-size: 11px;'><strong>Ubicación:</strong> {alerta.Address}</p>
                            </div>
                        </div>

                        <div style='width: 40%; text-align: right;'>
                            <img src='{imagenAlerta}' alt='Imagen Alerta' style='width: 100%; max-width: 200px; height: auto;' />
                        </div>
                    </div>

                    <div style='width: 100%;'>
                        <div style='width: 80%; margin: 0 auto;'>
                            <hr style='border: none; border-top: 0.5px solid black; margin: 20px 0;' />
                            <p style='text-align: center; font-size: 12px;'>
                                Estamos comprometidos con brindarle a usted el mejor servicio. Gracias por su preferencia.
                            </p>
                            <hr style='border: none; border-top: 0.5px solid black; margin: 20px 0;' />
                        </div>
                    </div>

                    <div style='background-color: #fff; text-align: center; padding: 20px; color: #001d3d; font-size: 11px; font-family: Arial, sans-serif;'>
                        <p style='margin: 5px 0;'><strong>Central de Monitoreo y Gestión</strong></p>
                        <p style='margin: 5px 0;'>989112975 - 952075325</p>
                        <p style='margin: 5px 0;'>cmyg@velsat.com.pe</p>
                        <p style='margin: 5px 0;'>Av. Juan Pablo Fernandini 1439 Int. 603F, Pueblo Libre, Lima.</p>
                        <hr style='border: none; border-top: 1px solid #001d3d; margin: 15px 0;' />
                        <p style='margin: 0;'>© {DateTime.Now.Year} Todos los derechos reservados.</p>
                    </div>
                </div>
            </div>
        </body>
    </html>";
        }
    }
}