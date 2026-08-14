using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Notificaciones;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface INotificacionesRepository
    {
        Task<bool> SaveFCMTokenAsync(string codigo, string fcmToken, string platform);

        // Batch: usado por AsignarServicio (varios conductores en un solo POST).
        Task<IEnumerable<UserFCMToken>> GetFCMTokensByCodigosAsync(IEnumerable<string> codigos);

        // ServTurismo solo tiene "brevete" (no codtaxi), así que resuelve el token vía JOIN a taxi.
        Task<UserFCMToken> GetFCMTokenByBreveteAsync(string brevete);
    }
}
