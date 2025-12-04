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
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;

        // ✅ CAMBIO: Inyectar Factory en lugar de UnitOfWork
        public TurnosController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
        }

        // ✅ GET - Solo lectura
        [HttpGet("{campo}/{accountID}")]
        public async Task<ActionResult<IEnumerable<string>>> GetListFilter(string campo, string accountID)
        {
            try
            {
                var result = await _readOnlyUow.TurnosRepository.GetListFilter(campo, accountID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la lista de filtros", error = ex.Message });
            }

        }

        // ✅ GET - Solo lectura
        [HttpGet("{accountID}")]
        public async Task<ActionResult<IEnumerable<TurnoAvianca>>> GetTurnos(string accountID)
        {

            try
            {
                var result = await _readOnlyUow.TurnosRepository.GetTurnos(accountID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los turnos", error = ex.Message });
            }

        }

        // ✅ POST - Requiere SaveChanges
        [HttpPost("{accountID}")]
        public async Task<ActionResult> InsertTurno([FromBody] TurnoAvianca turno, string accountID)
        {
            try
            {
                var result = await _uow.TurnosRepository.InsertTurno(turno, accountID);
                _uow.SaveChanges(); // Commit solo para operaciones que modifican datos
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al insertar turno", error = ex.Message });
            }
        }

        // ✅ PUT - Requiere SaveChanges
        [HttpPut("{codigo}")]
        public async Task<ActionResult> UpdateTurno([FromBody] TurnoAvianca turno, string codigo)
        {

            try
            {
                var result = await _uow.TurnosRepository.UpdateTurno(turno, codigo);
                _uow.SaveChanges(); // Commit
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar turno", error = ex.Message });
            }

        }

        // ✅ DELETE - Requiere SaveChanges
        [HttpDelete("{codigo}")]
        public async Task<ActionResult> DeleteTurno(string codigo)
        {
            try
            {
                var result = await _uow.TurnosRepository.DeleteTurno(codigo);
                _uow.SaveChanges(); // Commit
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar turno", error = ex.Message });
            }

        }
    }
}
