using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{campo}/{accountID}")]
        public async Task<ActionResult<IEnumerable<TurnoAvianca>>> GetListFilter(string campo, string accountID)
        {
            var result = await _unitOfWork.TurnosRepository.GetListFilter(campo, accountID);
            return Ok(result);
        }

        [HttpGet("{accountID}")]
        public async Task<ActionResult<IEnumerable<TurnoAvianca>>> GetTurnos(string accountID)
        {
            return Ok(await _unitOfWork.TurnosRepository.GetTurnos(accountID));
        }

        [HttpPost("{accountID}")]
        public async Task<ActionResult> InsertTurno([FromBody] TurnoAvianca turno, string accountID)
        {
            var result = await _unitOfWork.TurnosRepository.InsertTurno(turno, accountID);

            return Ok(result);
        }

        [HttpPut("{codigo}")]
        public async Task<ActionResult> UpdateTurno([FromBody] TurnoAvianca turno, string codigo)
        {
            return Ok(await _unitOfWork.TurnosRepository.UpdateTurno(turno, codigo));
        }


        [HttpDelete("{codigo}")]
        public async Task<ActionResult> DeleteTurno(string codigo)
        {
            var result = await _unitOfWork.TurnosRepository.DeleteTurno(codigo);
            return Ok(result);
        }
    }
}
