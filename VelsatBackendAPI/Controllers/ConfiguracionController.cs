using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Data.Services;
using VelsatBackendAPI.Model.Configuracion;

namespace VelsatBackendAPI.Controllers
{
    // Todo el módulo opera sobre la cuenta del token: el accountID NUNCA viene del body ni de la URL.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConfiguracionController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IReadOnlyUnitOfWork _readOnlyUow;
        private readonly ICorreoService _correo;
        private readonly IConfiguration _config;

        // Reset de contraseña por enlace
        private const int ResetMinutosValidez = 30;
        private const int ResetSegundosEntreSolicitudes = 60;
        private const string OrigenFront = "trackvel"; // el front lo usa para saber a qué backend llamar

        public ConfiguracionController(IUnitOfWork uow, IReadOnlyUnitOfWork readOnlyUow,
            ICorreoService correo, IConfiguration config)
        {
            _uow = uow;
            _readOnlyUow = readOnlyUow;
            _correo = correo;
            _config = config;
        }

        private string? AccountId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ---------------------------------------------------------------- PERFIL

        [HttpGet("perfil")]
        public async Task<IActionResult> GetPerfil()
        {
            try
            {
                var perfil = await _readOnlyUow.ConfiguracionRepository.GetPerfilAsync(AccountId!);
                if (perfil == null)
                    return NotFound(new { message = "No se encontró la cuenta." });

                return Ok(perfil);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el perfil", error = ex.Message });
            }
        }

        [HttpPut("perfil")]
        public async Task<IActionResult> UpdatePerfil([FromBody] ActualizarPerfilRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Los datos son requeridos." });

            var error = ValidarLargo(request.DisplayName, 40, "displayName")
                     ?? ValidarLargo(request.ContactName, 64, "contactName")
                     ?? ValidarLargo(request.ContactPhone, 32, "contactPhone")
                     ?? ValidarEmail(request.ContactEmail, 200, "contactEmail")
                     ?? ValidarLargo(request.AddressLine1, 70, "addressLine1")
                     ?? ValidarLargo(request.AddressLine2, 70, "addressLine2")
                     ?? ValidarLargo(request.AddressLine3, 70, "addressLine3")
                     ?? ValidarLargo(request.AddressCity, 50, "addressCity")
                     ?? ValidarLargo(request.AddressState, 50, "addressState")
                     ?? ValidarLargo(request.AddressPostalCode, 20, "addressPostalCode")
                     ?? ValidarLargo(request.AddressCountry, 20, "addressCountry");
            if (error != null)
                return BadRequest(new { message = error });

            try
            {
                var rows = await _uow.ConfiguracionRepository.UpdatePerfilAsync(AccountId!, request);
                _uow.SaveChanges();

                if (rows == 0)
                    return NotFound(new { message = "No se encontró la cuenta." });

                return Ok(new { message = "Perfil actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el perfil", error = ex.Message });
            }
        }

        // -------------------------------------------------------- NOTIFICACIONES

        [HttpGet("notificaciones")]
        public async Task<IActionResult> GetNotificaciones()
        {
            try
            {
                var config = await _readOnlyUow.ConfiguracionRepository.GetNotificacionesAsync(AccountId!);
                if (config == null)
                    return NotFound(new { message = "No se encontró la cuenta." });

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las notificaciones", error = ex.Message });
            }
        }

        [HttpPut("notificaciones")]
        public async Task<IActionResult> UpdateNotificaciones([FromBody] NotificacionesConfig request)
        {
            if (request == null)
                return BadRequest(new { message = "Los datos son requeridos." });

            var error = ValidarEmail(request.NotifyEmail, 128, "notifyEmail");
            if (error != null)
                return BadRequest(new { message = error });

            try
            {
                var rows = await _uow.ConfiguracionRepository.UpdateNotificacionesAsync(AccountId!, request);
                _uow.SaveChanges();

                if (rows == 0)
                    return NotFound(new { message = "No se encontró la cuenta." });

                return Ok(new { message = "Notificaciones actualizadas correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar las notificaciones", error = ex.Message });
            }
        }

        // -------------------------------------------------------------- SEGURIDAD

        [HttpPut("password")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
        {
            if (request == null
                || string.IsNullOrEmpty(request.PasswordActual)
                || string.IsNullOrEmpty(request.PasswordNueva))
                return BadRequest(new { message = "La contraseña actual y la nueva son obligatorias." });

            // usuarios.password es varchar(32)
            if (request.PasswordNueva.Length < 6 || request.PasswordNueva.Length > 32)
                return BadRequest(new { message = "La nueva contraseña debe tener entre 6 y 32 caracteres." });

            if (request.PasswordNueva == request.PasswordActual)
                return BadRequest(new { message = "La nueva contraseña debe ser distinta de la actual." });

            try
            {
                var rows = await _uow.ConfiguracionRepository.CambiarPasswordAsync(AccountId!, request.PasswordActual, request.PasswordNueva);
                _uow.SaveChanges();

                if (rows == 0)
                    return BadRequest(new { message = "La contraseña actual es incorrecta." });

                return Ok(new { message = "Contraseña actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cambiar la contraseña", error = ex.Message });
            }
        }

        // Paso 1 (con sesión): envía al correo de la cuenta un enlace de un solo uso para elegir otra contraseña.
        [HttpPost("password/solicitar-reset")]
        public async Task<IActionResult> SolicitarResetPassword()
        {
            var frontBase = _config["Front:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrEmpty(frontBase))
                return StatusCode(500, new { message = "El envío del enlace no está configurado (Front:BaseUrl)." });

            try
            {
                var contacto = await _uow.ConfiguracionRepository.GetContactoAsync(AccountId!);
                if (contacto == null)
                    return NotFound(new { message = "No se encontró la cuenta." });

                if (string.IsNullOrWhiteSpace(contacto.ContactEmail))
                    return BadRequest(new { message = "Tu cuenta no tiene un correo registrado. Agrégalo en Mi cuenta." });

                if (await _uow.ConfiguracionRepository.HayResetRecienteAsync(AccountId!, ResetSegundosEntreSolicitudes))
                    return StatusCode(429, new { message = "Ya te enviamos un enlace hace un momento. Espera un minuto para pedir otro." });

                var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                await _uow.ConfiguracionRepository.CrearResetTokenAsync(AccountId!, HashToken(token), ResetMinutosValidez);
                _uow.SaveChanges();

                var enlace = $"{frontBase}/reset-password?token={token}&origen={OrigenFront}";
                var html = CorreoPlantillas.ResetPassword(contacto.DisplayName ?? "", enlace, ResetMinutosValidez);

                if (!await _correo.EnviarAsync(contacto.ContactEmail!, "Cambio de contraseña - Velsat", html))
                    return StatusCode(502, new { message = "No se pudo enviar el correo. Inténtalo de nuevo más tarde." });

                return Ok(new
                {
                    message = "Te enviamos un enlace para cambiar tu contraseña.",
                    email = EnmascararCorreo(contacto.ContactEmail!),
                    expiraEnMinutos = ResetMinutosValidez
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al solicitar el cambio de contraseña", error = ex.Message });
            }
        }

        // Paso 2 (sin sesión): el enlace del correo trae el token; con él se fija la nueva contraseña.
        [AllowAnonymous]
        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Token)
                || string.IsNullOrEmpty(request.PasswordNueva))
                return BadRequest(new { message = "El token y la nueva contraseña son obligatorios." });

            // usuarios.password es varchar(32)
            if (request.PasswordNueva.Length < 6 || request.PasswordNueva.Length > 32)
                return BadRequest(new { message = "La nueva contraseña debe tener entre 6 y 32 caracteres." });

            try
            {
                var ok = await _uow.ConfiguracionRepository.ConsumirResetTokenAsync(HashToken(request.Token.Trim()), request.PasswordNueva);
                if (!ok)
                    return BadRequest(new { message = "El enlace no es válido o ya venció. Solicita uno nuevo." });

                _uow.SaveChanges();
                return Ok(new { message = "Contraseña actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cambiar la contraseña", error = ex.Message });
            }
        }

        // ------------------------------------------------------------------ ÍCONOS

        // Catálogo predefinido que el usuario puede elegir.
        [HttpGet("iconos/catalogo")]
        public async Task<IActionResult> GetCatalogoIconos()
        {
            try
            {
                var catalogo = await _readOnlyUow.ConfiguracionRepository.GetCatalogoIconosAsync();
                return Ok(catalogo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el catálogo de íconos", error = ex.Message });
            }
        }

        // Unidades de la cuenta con el ícono que tienen asignado (iconoUrl null = por defecto).
        [HttpGet("iconos")]
        public async Task<IActionResult> GetIconos()
        {
            try
            {
                var iconos = await _readOnlyUow.ConfiguracionRepository.GetIconosCuentaAsync(AccountId!);
                return Ok(iconos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los íconos de las unidades", error = ex.Message });
            }
        }

        [HttpPut("iconos/{deviceID}")]
        public async Task<IActionResult> AsignarIcono(string deviceID, [FromBody] AsignarIconoRequest request)
        {
            if (string.IsNullOrWhiteSpace(deviceID))
                return BadRequest(new { message = "El deviceID es obligatorio." });

            if (request == null || request.IconoId <= 0)
                return BadRequest(new { message = "El iconoId es obligatorio." });

            try
            {
                var repo = _uow.ConfiguracionRepository;

                if (!await repo.DispositivoPerteneceACuentaAsync(AccountId!, deviceID))
                    return NotFound(new { message = "La unidad no existe en tu cuenta." });

                var icono = await repo.GetIconoCatalogoAsync(request.IconoId);
                if (icono == null)
                    return BadRequest(new { message = "El ícono no existe en el catálogo." });

                await repo.UpsertIconoAsync(AccountId!, deviceID, icono.IconoUrl);
                _uow.SaveChanges();

                return Ok(new { message = "Ícono actualizado correctamente.", iconoUrl = icono.IconoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al asignar el ícono", error = ex.Message });
            }
        }

        // Vuelve al ícono por defecto.
        [HttpDelete("iconos/{deviceID}")]
        public async Task<IActionResult> QuitarIcono(string deviceID)
        {
            if (string.IsNullOrWhiteSpace(deviceID))
                return BadRequest(new { message = "El deviceID es obligatorio." });

            try
            {
                // El DELETE filtra por accountID del token, así que no puede tocar unidades ajenas.
                await _uow.ConfiguracionRepository.DeleteIconoAsync(AccountId!, deviceID);
                _uow.SaveChanges();

                return Ok(new { message = "Se restableció el ícono por defecto." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al restablecer el ícono", error = ex.Message });
            }
        }

        // ----------------------------------------------------------- RESET: UTILIDADES

        private static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // En la BD solo se guarda el hash: si se filtra la tabla, los enlaces no sirven.
        private static string HashToken(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

        private static string EnmascararCorreo(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return email;
            return email[0] + new string('*', Math.Min(at - 1, 6)) + email[at..];
        }

        // ----------------------------------------------------------- VALIDACIONES

        private static string? ValidarLargo(string? valor, int max, string campo)
            => valor != null && valor.Length > max ? $"{campo} no puede superar {max} caracteres." : null;

        private static string? ValidarEmail(string? valor, int max, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null; // vacío permitido: el usuario puede borrar el correo

            if (valor.Length > max)
                return $"{campo} no puede superar {max} caracteres.";

            return MailAddress.TryCreate(valor, out _) ? null : $"{campo} no tiene un formato válido.";
        }
    }
}
