using VelsatBackendAPI.Model.Telemetria;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IConductorTelemetriaRepository
    {
        Task<List<ConductorTelemetria>> GetByUsuario(string codusuario);

        Task<ConductorTelemetria?> GetById(int codtaxi);

        Task<bool> ExisteCodigoIbutton(string codigoIbutton, int? excluirCodtaxi = null);

        Task<int> Insert(ConductorTelemetriaRequest conductor);

        Task<int> Update(int codtaxi, ConductorTelemetriaRequest conductor);

        Task<int> Delete(int codtaxi);

        Task<int> ActualizarEstadoTraccar(int codtaxi, int? traccarDriverId, bool traccarSync);
    }
}
