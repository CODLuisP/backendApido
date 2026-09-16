using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public class GeocercasVehiculosRepository : IGeocercasVehiculosRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        public GeocercasVehiculosRepository(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
        {
            _defaultConnection = defaultConnection;
            _defaultTransaction = defaultTransaction;
        }

        public async Task<IEnumerable<GeocercasVehiculos>> GetByGeocerca(int idGeocerca)
        {
            const string sql = @"
                SELECT * FROM geocercas_vehiculos
                WHERE id_geocerca = @IdGeocerca AND activo = 1
                ORDER BY deviceID";

            return await _defaultConnection.QueryAsync<GeocercasVehiculos>(
                sql, new { IdGeocerca = idGeocerca }, transaction: _defaultTransaction);
        }

        public async Task<IEnumerable<Geocercas>> GetGeocercasByDevice(string deviceID)
        {
            const string sql = @"
                SELECT g.* FROM geocercas g
                INNER JOIN geocercas_vehiculos gv ON gv.id_geocerca = g.id
                WHERE gv.deviceID = @DeviceID AND gv.activo = 1 AND g.activo = 1
                ORDER BY g.nombre";

            return await _defaultConnection.QueryAsync<Geocercas>(
                sql, new { DeviceID = deviceID }, transaction: _defaultTransaction);
        }

        public async Task<string> Vincular(int idGeocerca, IEnumerable<string> deviceIds)
        {
            const string sql = @"
                INSERT INTO geocercas_vehiculos (id_geocerca, deviceID)
                VALUES (@IdGeocerca, @DeviceID)
                ON DUPLICATE KEY UPDATE activo = 1, fecha_vinculacion = CURRENT_TIMESTAMP";

            foreach (var deviceId in deviceIds)
            {
                await _defaultConnection.ExecuteAsync(
                    sql, new { IdGeocerca = idGeocerca, DeviceID = deviceId }, _defaultTransaction);
            }

            return "Success insertion";
        }

        public async Task<string> Desvincular(int idGeocerca, string deviceID)
        {
            const string sql = @"
                DELETE FROM geocercas_vehiculos
                WHERE id_geocerca = @IdGeocerca AND deviceID = @DeviceID";

            await _defaultConnection.ExecuteAsync(
                sql, new { IdGeocerca = idGeocerca, DeviceID = deviceID }, _defaultTransaction);

            return "Success delete";
        }
    }
}
