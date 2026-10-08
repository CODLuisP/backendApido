using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Security.Claims;
using VelsatBackendAPI.Data.Repositories;
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

        public ConfiguracionController(IUnitOfWork uow, IReadOnlyUnitOfWork readOnlyUow)
        {
            _uow = uow;
            _readOnlyUow = readOnlyUow;
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
