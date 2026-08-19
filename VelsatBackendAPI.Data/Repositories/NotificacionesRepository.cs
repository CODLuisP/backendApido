using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Notificaciones;

namespace VelsatBackendAPI.Data.Repositories
{
    // Vive bajo DOConnection: es la misma base (gts) donde están taxi, servicio, servturismo y preplan,
    // así que el token FCM se puede resolver por codtaxi/brevete sin cruzar bases de datos.
    public class NotificacionesRepository : INotificacionesRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public NotificacionesRepository(IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        // Upsert por (Codigo, DeviceId): un conductor puede tener un token activo POR DISPOSITIVO,
        // así que loguearse en un segundo celular no pisa el token del primero — ambos reciben la alerta.
        // Requiere índice único en UserFCMTokens(Codigo, DeviceId).
        public async Task<bool> SaveFCMTokenAsync(string codigo, string fcmToken, string platform, string deviceId)
        {
            string sqlUpsert = @"INSERT INTO UserFCMTokens
                                  (Codigo, FCMToken, Platform, DeviceId, CreatedAt, UpdatedAt)
                                  VALUES (@Codigo, @FCMToken, @Platform, @DeviceId, NOW(), NOW())
                                  ON DUPLICATE KEY UPDATE
                                      FCMToken = VALUES(FCMToken),
                                      Platform = VALUES(Platform),
                                      UpdatedAt = NOW()";

            int rows = await _doConnection.ExecuteAsync(
                sqlUpsert,
                new { Codigo = codigo, FCMToken = fcmToken, Platform = platform, DeviceId = deviceId },
                transaction: _doTransaction);

            return rows > 0;
        }

        // Limpieza cuando Firebase reporta el token como no registrado/inválido.
        public async Task<bool> DeleteFCMTokenAsync(string fcmToken)
        {
            string sql = @"DELETE FROM UserFCMTokens WHERE FCMToken = @FCMToken";

            int rows = await _doConnection.ExecuteAsync(
                sql,
                new { FCMToken = fcmToken },
                transaction: _doTransaction);

            return rows > 0;
        }

        public async Task<IEnumerable<UserFCMToken>> GetFCMTokensByCodigosAsync(IEnumerable<string> codigos)
        {
            var lista = codigos?.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

            if (lista == null || !lista.Any())
                return Enumerable.Empty<UserFCMToken>();

            string sql = @"SELECT Id, Codigo, FCMToken, Platform, CreatedAt, UpdatedAt
                            FROM UserFCMTokens
                            WHERE Codigo IN @Codigos";

            return await _doConnection.QueryAsync<UserFCMToken>(
                sql,
                new { Codigos = lista },
                transaction: _doTransaction);
        }

        public async Task<UserFCMToken> GetFCMTokenByBreveteAsync(string brevete)
        {
            if (string.IsNullOrWhiteSpace(brevete))
                return null;

            string sql = @"SELECT t.Id, t.Codigo, t.FCMToken, t.Platform, t.CreatedAt, t.UpdatedAt
                            FROM UserFCMTokens t
                            INNER JOIN taxi ON taxi.codtaxi = t.Codigo
                            WHERE taxi.brevete = @Brevete
                            ORDER BY t.UpdatedAt DESC
                            LIMIT 1";

            return await _doConnection.QueryFirstOrDefaultAsync<UserFCMToken>(
                sql,
                new { Brevete = brevete },
                transaction: _doTransaction);
        }
    }
}
