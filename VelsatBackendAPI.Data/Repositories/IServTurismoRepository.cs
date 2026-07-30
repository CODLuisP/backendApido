using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IServTurismoRepository
    {
        Task<List<ServTurismo>> GetByFechas(DateTime fechaInicio, DateTime fechaFin);

        Task<int> Insert(ServTurismo servicio);

        Task<int> InsertBatch(IEnumerable<ServTurismo> servicios);

        Task<int> Patch(int idservicio, ServTurismo campos);

        Task<int> Delete(int idservicio);
    }
}
