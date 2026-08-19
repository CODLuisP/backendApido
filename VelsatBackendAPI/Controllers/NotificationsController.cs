using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Notificaciones;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : Controller
    {
        private readonly IUnitOfWork _uow;

        public NotificationsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpPost("save-token")]
        public async Task<IActionResult> SaveToken([FromBody] UserFCMToken request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Los datos son requeridos.");

                if (string.IsNullOrWhiteSpace(request.Codigo))
                    return BadRequest("El campo codigo es obligatorio.");

                if (string.IsNullOrWhiteSpace(request.FCMToken))
                    return BadRequest("El campo fcmtoken es obligatorio.");

                if (string.IsNullOrWhiteSpace(request.Platform) ||
                    (request.Platform != "android" && request.Platform != "ios"))
                    return BadRequest("El campo Platform debe ser 'android' o 'ios'.");

                if (string.IsNullOrWhiteSpace(request.DeviceId))
                    return BadRequest("El campo deviceId es obligatorio.");

                var resultado = await _uow.NotificacionesRepository.SaveFCMTokenAsync(request.Codigo, request.FCMToken, request.Platform, request.DeviceId);

                _uow.SaveChanges();

                if (!resultado)
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "No se pudo guardar el token."
                    });

                return Ok(new
                {
                    success = true,
                    message = "Token FCM guardado correctamente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error interno del servidor: {ex.Message}"
                });
            }
        }
    }
}
