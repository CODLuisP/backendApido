using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public class GeocercasRepository : IGeocercasRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        public GeocercasRepository(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
        {
            _defaultConnection = defaultConnection;
            _defaultTransaction = defaultTransaction;
        }

        private const string SelectColumns = @"
            id, accountID, geofenceID, nombre, descripcion, tipo,
            area_wkt AS AreaWkt, coordenadas_json AS CoordenadasJson, color, activo,
            fecha_creacion AS FechaCreacion, fecha_actualizacion AS FechaActualizacion";

        public async Task<IEnumerable<Geocercas>> GetByAccount(string accountID)
        {
            string sql = $@"
                SELECT {SelectColumns} FROM geocercas
                WHERE accountID = @AccountID AND activo = 1
                ORDER BY nombre";

            return await _defaultConnection.QueryAsync<Geocercas>(
                sql, new { AccountID = accountID }, transaction: _defaultTransaction);
        }

        public async Task<Geocercas> GetById(int id)
        {
            string sql = $"SELECT {SelectColumns} FROM geocercas WHERE id = @Id";

            return await _defaultConnection.QueryFirstOrDefaultAsync<Geocercas>(
                sql, new { Id = id }, transaction: _defaultTransaction);
        }

        public async Task<int> Insert(Geocercas geocerca)
        {
            const string sql = @"
                INSERT INTO geocercas (accountID, geofenceID, nombre, descripcion, tipo, area_wkt, coordenadas_json, color)
                VALUES (@AccountID, @GeofenceID, @Nombre, @Descripcion, @Tipo, @AreaWkt, @CoordenadasJson, @Color);
                SELECT LAST_INSERT_ID();";

            var parameters = new
            {
                geocerca.AccountID,
                geocerca.GeofenceID,
                geocerca.Nombre,
                geocerca.Descripcion,
                geocerca.Tipo,
                geocerca.AreaWkt,
                geocerca.CoordenadasJson,
                geocerca.Color
            };

            return await _defaultConnection.QuerySingleAsync<int>(sql, parameters, _defaultTransaction);
        }

        public async Task<string> Update(Geocercas geocerca)
        {
            const string sql = @"
                UPDATE geocercas
                SET nombre = @Nombre, descripcion = @Descripcion, tipo = @Tipo,
                    area_wkt = @AreaWkt, coordenadas_json = @CoordenadasJson, color = @Color
                WHERE id = @Id";

            var parameters = new
            {
                geocerca.Id,
                geocerca.Nombre,
                geocerca.Descripcion,
                geocerca.Tipo,
                geocerca.AreaWkt,
                geocerca.CoordenadasJson,
                geocerca.Color
            };

            await _defaultConnection.ExecuteAsync(sql, parameters, _defaultTransaction);
            return "Success update";
        }

        public async Task<string> Delete(int id)
        {
            const string sql = "DELETE FROM geocercas WHERE id = @Id";
            await _defaultConnection.ExecuteAsync(sql, new { Id = id }, _defaultTransaction);
            return "Success delete";
        }
    }
}
