using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IServTurismoRepository
    {
        Task<List<ServTurismo>> GetByFechas(DateTime fechaInicio, DateTime fechaFin, string? brevete = null);

        Task<int> Insert(ServTurismo servicio);

        Task<int> InsertBatch(IEnumerable<ServTurismo> servicios);

        Task<int> Patch(int idservicio, ServTurismo campos, bool limpiarNulos = false);

        Task<int> Delete(int idservicio);

        // Acuse de recibo del conductor. Devuelven false solo si el idservicio no existe;
        // volver a marcar un servicio ya marcado es una operación válida (no cambia nada).
        Task<bool> MarcarVisto(int idservicio);

        Task<bool> MarcarConfirmado(int idservicio);

        Task<bool> MarcarCancelado(int idservicio);

        //CRUD tabla taxi (conductores)
        Task<List<ConductorTurismo>> GetTaxis(string codusuario);

        Task<ConductorTurismo?> GetTaxiById(int codtaxi);

        Task<int> InsertTaxi(ConductorTurismo taxi);

        Task<int> PatchTaxi(int codtaxi, ConductorTurismo campos);

        Task<int> DeleteTaxi(int codtaxi);
    }
}
