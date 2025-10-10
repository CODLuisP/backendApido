using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TurnosController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public TurnosController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET - No necesita SaveChanges (solo lectura)
        [HttpGet("{campo}/{accountID}")]
        public async Task<ActionResult<IEnumerable<string>>> GetListFilter(string campo, string accountID)
        {
            var result = await _unitOfWork.TurnosRepository.GetListFilter(campo, accountID);
            return Ok(result);
        }

        // GET - No necesita SaveChanges (solo lectura)
        [HttpGet("{accountID}")]
        public async Task<ActionResult<IEnumerable<TurnoAvianca>>> GetTurnos(string accountID)
        {
            var result = await _unitOfWork.TurnosRepository.GetTurnos(accountID);
            return Ok(result);
        }

        // POST - Necesita SaveChanges
        [HttpPost("{accountID}")]
        public async Task<ActionResult> InsertTurno([FromBody] TurnoAvianca turno, string accountID)
        {
            try
            {
                var result = await _unitOfWork.TurnosRepository.InsertTurno(turno, accountID);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                // El rollback se hace automáticamente en Dispose
                return StatusCode(500, new { message = "Error al insertar turno", error = ex.Message });
            }
        }

        // PUT - Necesita SaveChanges
        [HttpPut("{codigo}")]
        public async Task<ActionResult> UpdateTurno([FromBody] TurnoAvianca turno, string codigo)
        {
            try
            {
                var result = await _unitOfWork.TurnosRepository.UpdateTurno(turno, codigo);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar turno", error = ex.Message });
            }
        }

        // DELETE - Necesita SaveChanges
        [HttpDelete("{codigo}")]
        public async Task<ActionResult> DeleteTurno(string codigo)
        {
            try
            {
                var result = await _unitOfWork.TurnosRepository.DeleteTurno(codigo);
                _unitOfWork.SaveChanges(); // ✅ Commit aquí
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar turno", error = ex.Message });
            }
        }
    }
}