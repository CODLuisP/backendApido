using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Configuracion;

namespace VelsatBackendAPI.Data.Repositories
{
    // Vive bajo DefaultConnection (esquema gts del servidor principal): ahí están usuarios, device y dispositivo_icono.
    public class ConfiguracionRepository : IConfiguracionRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        public ConfiguracionRepository(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
        {
            _defaultConnection = defaultConnection;
            _defaultTransaction = defaultTransaction;
        }

        // Mismo criterio que AdminRepository.InsertUser: timestamp Unix en hora de Perú (UTC-5).
        private static int NowUnixPeru()
        {
            var peruTime = DateTime.UtcNow.AddHours(-5);
            return (int)((DateTimeOffset)peruTime).ToUnixTimeSeconds();
        }

        public Task<PerfilCuenta?> GetPerfilAsync(string accountID)
        {
            const string sql = @"SELECT accountID, displayName, contactName, contactPhone, contactEmail,
                                        addressLine1, addressLine2, addressLine3, addressCity, addressState,
                                        addressPostalCode, addressCountry
                                 FROM usuarios WHERE accountID = @AccountID LIMIT 1";
            return _defaultConnection.QueryFirstOrDefaultAsync<PerfilCuenta?>(sql, new { AccountID = accountID }, _defaultTransaction);
        }

        public Task<int> UpdatePerfilAsync(string accountID, ActualizarPerfilRequest p)
        {
            const string sql = @"UPDATE usuarios SET
                                    displayName = @DisplayName, contactName = @ContactName,
                                    contactPhone = @ContactPhone, contactEmail = @ContactEmail,
                                    addressLine1 = @AddressLine1, addressLine2 = @AddressLine2, addressLine3 = @AddressLine3,
                                    addressCity = @AddressCity, addressState = @AddressState,
                                    addressPostalCode = @AddressPostalCode, addressCountry = @AddressCountry,
                                    lastUpdateTime = @Now
                                 WHERE accountID = @AccountID";
            return _defaultConnection.ExecuteAsync(sql, new
            {
                AccountID = accountID,
                p.DisplayName, p.ContactName, p.ContactPhone, p.ContactEmail,
                p.AddressLine1, p.AddressLine2, p.AddressLine3,
                p.AddressCity, p.AddressState, p.AddressPostalCode, p.AddressCountry,
                Now = NowUnixPeru()
            }, _defaultTransaction);
        }

        public Task<NotificacionesConfig?> GetNotificacionesAsync(string accountID)
        {
            const string sql = @"SELECT recibir_notificaciones AS RecibirNotificaciones, notifyEmail
                                 FROM usuarios WHERE accountID = @AccountID LIMIT 1";
            return _defaultConnection.QueryFirstOrDefaultAsync<NotificacionesConfig?>(sql, new { AccountID = accountID }, _defaultTransaction);
        }

        public Task<int> UpdateNotificacionesAsync(string accountID, NotificacionesConfig config)
        {
            const string sql = @"UPDATE usuarios SET recibir_notificaciones = @Recibir, notifyEmail = @NotifyEmail,
                                        lastUpdateTime = @Now
                                 WHERE accountID = @AccountID";
            return _defaultConnection.ExecuteAsync(sql, new
            {
                AccountID = accountID,
                Recibir = config.RecibirNotificaciones ? 1 : 0,
                config.NotifyEmail,
                Now = NowUnixPeru()
            }, _defaultTransaction);
        }

        public Task<int> CambiarPasswordAsync(string accountID, string passwordActual, string passwordNueva)
        {
            // BINARY: la comparación por defecto de MySQL no distingue mayúsculas de minúsculas.
            const string sql = @"UPDATE usuarios SET password = @Nueva, passwdChangeTime = @Now, lastUpdateTime = @Now
                                 WHERE accountID = @AccountID AND password = BINARY @Actual";
            return _defaultConnection.ExecuteAsync(sql, new
            {
                AccountID = accountID,
                Actual = passwordActual,
                Nueva = passwordNueva,
                Now = NowUnixPeru()
            }, _defaultTransaction);
        }

        public Task<IEnumerable<IconoCatalogo>> GetCatalogoIconosAsync()
        {
            const string sql = @"SELECT id, nombre, icono_url AS IconoUrl, orden
                                 FROM icono_catalogo WHERE activo = 1 ORDER BY orden, nombre";
            return _defaultConnection.QueryAsync<IconoCatalogo>(sql, transaction: _defaultTransaction);
        }

        public Task<IconoCatalogo?> GetIconoCatalogoAsync(int iconoId)
        {
            const string sql = @"SELECT id, nombre, icono_url AS IconoUrl, orden
                                 FROM icono_catalogo WHERE id = @Id AND activo = 1";
            return _defaultConnection.QueryFirstOrDefaultAsync<IconoCatalogo?>(sql, new { Id = iconoId }, _defaultTransaction);
        }

        public Task<IEnumerable<DispositivoIconoDto>> GetIconosCuentaAsync(string accountID)
        {
            const string sql = @"SELECT d.deviceID, d.description AS Descripcion, i.icono_url AS IconoUrl
                                 FROM device d
                                 LEFT JOIN dispositivo_icono i ON i.accountID = d.accountID AND i.deviceID = d.deviceID
                                 WHERE d.accountID = @AccountID
                                 ORDER BY d.description";
            return _defaultConnection.QueryAsync<DispositivoIconoDto>(sql, new { AccountID = accountID }, _defaultTransaction);
        }

        public async Task<bool> DispositivoPerteneceACuentaAsync(string accountID, string deviceID)
        {
            const string sql = "SELECT COUNT(1) FROM device WHERE accountID = @AccountID AND deviceID = @DeviceID";
            var n = await _defaultConnection.ExecuteScalarAsync<int>(sql, new { AccountID = accountID, DeviceID = deviceID }, _defaultTransaction);
            return n > 0;
        }

        // Requiere el índice único uq_cuenta_dispositivo (accountID, deviceID): ver configuracion_modulo.sql.
        public Task<int> UpsertIconoAsync(string accountID, string deviceID, string iconoUrl)
        {
            const string sql = @"INSERT INTO dispositivo_icono (accountID, deviceID, icono_url, fecha_actualizacion)
                                 VALUES (@AccountID, @DeviceID, @IconoUrl, NOW())
                                 ON DUPLICATE KEY UPDATE icono_url = VALUES(icono_url), fecha_actualizacion = NOW()";
            return _defaultConnection.ExecuteAsync(sql, new { AccountID = accountID, DeviceID = deviceID, IconoUrl = iconoUrl }, _defaultTransaction);
        }

        public Task<int> DeleteIconoAsync(string accountID, string deviceID)
        {
            const string sql = "DELETE FROM dispositivo_icono WHERE accountID = @AccountID AND deviceID = @DeviceID";
            return _defaultConnection.ExecuteAsync(sql, new { AccountID = accountID, DeviceID = deviceID }, _defaultTransaction);
        }
    }
}
