using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Configuracion;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IConfiguracionRepository
    {
        Task<PerfilCuenta?> GetPerfilAsync(string accountID);
        Task<int> UpdatePerfilAsync(string accountID, ActualizarPerfilRequest perfil);

        Task<NotificacionesConfig?> GetNotificacionesAsync(string accountID);
        Task<int> UpdateNotificacionesAsync(string accountID, NotificacionesConfig config);

        // UPDATE condicionado a la contraseña actual: devuelve 0 filas si no coincide.
        Task<int> CambiarPasswordAsync(string accountID, string passwordActual, string passwordNueva);

        Task<IEnumerable<IconoCatalogo>> GetCatalogoIconosAsync();
        Task<IconoCatalogo?> GetIconoCatalogoAsync(int iconoId);

        // Unidades de la cuenta con su ícono asignado (IconoUrl null = por defecto).
        Task<IEnumerable<DispositivoIconoDto>> GetIconosCuentaAsync(string accountID);
        Task<bool> DispositivoPerteneceACuentaAsync(string accountID, string deviceID);
        Task<int> UpsertIconoAsync(string accountID, string deviceID, string iconoUrl);
        Task<int> DeleteIconoAsync(string accountID, string deviceID);
    }
}
