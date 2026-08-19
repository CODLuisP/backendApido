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

        // BreveteAnterior: valor de "brevete" antes del patch, para que el caller pueda detectar
        // un cambio de conductor y notificarlo (ver ServTurismoController.Patch).
        // usuario/motivo: quién hizo la edición y por qué (motivo es opcional); se usan para dejar
        // un registro en servturismo_auditoria por cada campo que realmente cambió de valor.
        Task<(int Filas, string? BreveteAnterior)> Patch(int idservicio, ServTurismo campos, bool limpiarNulos = false, string? usuario = null, string? motivo = null);

        Task<int> Delete(int idservicio);

        // Historial de auditoría de un servicio, más reciente primero.
        Task<List<ServTurismoAuditoria>> GetAuditoria(int idservicio);

        // Acuse de recibo del conductor. Devuelven false solo si el idservicio no existe;
        // volver a marcar un servicio ya marcado es una operación válida (no cambia nada).
        Task<bool> MarcarVisto(int idservicio);

        Task<bool> MarcarConfirmado(int idservicio);

        Task<bool> MarcarFinalizado(int idservicio);

        Task<bool> MarcarCancelado(int idservicio);

        Task<bool> MarcarStandby(int idservicio);

        Task<bool> MarcarReanudar(int idservicio);

        //CRUD tabla taxi (conductores)
        Task<List<ConductorTurismo>> GetTaxis(string codusuario);

        Task<ConductorTurismo?> GetTaxiById(int codtaxi);

        Task<int> InsertTaxi(ConductorTurismo taxi);

        Task<int> PatchTaxi(int codtaxi, ConductorTurismo campos);

        Task<int> DeleteTaxi(int codtaxi);
    }
}
