using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow; // ✅ Cambiar a ReadOnly

        public UserController(IReadOnlyUnitOfWork readOnlyUow) // ✅ Cambiar
        {
            _readOnlyUow = readOnlyUow;
        }

        // ✅ GET - Solo lectura
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _readOnlyUow.UserRepository.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los usuarios", error = ex.Message });
            }

        }

        // ✅ GET - Solo lectura
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            try
            {
                var user = await _readOnlyUow.UserRepository.GetDetails(id);

                if (user == null)
                    return NotFound(new { message = $"No se encontró el usuario con ID {id}" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los detalles del usuario", error = ex.Message });
            }

        }
    }
}
