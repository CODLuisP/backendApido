using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface ITurnosRepository
    {
        Task<IEnumerable<string>> GetListFilter(string campo, string accountID);

        Task<IEnumerable<TurnoAvianca>> GetTurnos(string accountID);

        Task<string> InsertTurno(TurnoAvianca turno, string accountID);

        Task<string> UpdateTurno(TurnoAvianca turno, string codigo);

        Task<string> DeleteTurno(string codigo);

    }
}
