using Dapper;
using System.Data;
using VelsatBackendAPI.Model.Telemetria;

namespace VelsatBackendAPI.Data.Repositories
{
    // Todas las consultas sobre un conductor existente filtran telemetria = '1', para que este
    // CRUD nunca lea, modifique ni elimine conductores de otros módulos de la misma tabla.
    public class ConductorTelemetriaRepository : IConductorTelemetriaRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction? _doTransaction;

        private const string Columnas = @"codtaxi AS Codtaxi, nombres AS Nombres, apellidos AS Apellidos, dni AS Dni,
                                          telefono AS Telefono, brevete AS Brevete, codusuario AS Codusuario,
                                          codigo_ibutton AS CodigoIbutton, traccar_driver_id AS TraccarDriverId,
                                          traccar_sync AS TraccarSync";

        public ConductorTelemetriaRepository(IDbConnection doConnection, IDbTransaction? doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        public async Task<List<ConductorTelemetria>> GetByUsuario(string codusuario)
        {
            string sql = $@"SELECT {Columnas} FROM taxi
                            WHERE telemetria = '1' AND codusuario = @Codusuario
                            ORDER BY apellidos, nombres";

            var resultado = await _doConnection.QueryAsync<ConductorTelemetria>(
                sql, new { Codusuario = codusuario }, transaction: _doTransaction);

            return resultado.ToList();
        }

        public async Task<ConductorTelemetria?> GetById(int codtaxi)
        {
            string sql = $"SELECT {Columnas} FROM taxi WHERE codtaxi = @Codtaxi AND telemetria = '1'";

            return await _doConnection.QueryFirstOrDefaultAsync<ConductorTelemetria>(
                sql, new { Codtaxi = codtaxi }, transaction: _doTransaction);
        }

        // Busca en toda la tabla (no solo telemetría) porque codigo_ibutton es UNIQUE.
        public async Task<bool> ExisteCodigoIbutton(string codigoIbutton, int? excluirCodtaxi = null)
        {
            const string sql = @"SELECT COUNT(1) FROM taxi
                                 WHERE codigo_ibutton = @CodigoIbutton
                                   AND (@ExcluirCodtaxi IS NULL OR codtaxi <> @ExcluirCodtaxi)";

            int total = await _doConnection.ExecuteScalarAsync<int>(
                sql, new { CodigoIbutton = codigoIbutton, ExcluirCodtaxi = excluirCodtaxi }, transaction: _doTransaction);

            return total > 0;
        }

        public async Task<int> Insert(ConductorTelemetriaRequest conductor)
        {
            const string sql = @"INSERT INTO taxi
                                    (nombres, apellidos, dni, telefono, brevete, codusuario, codigo_ibutton, telemetria)
                                 VALUES
                                    (@Nombres, @Apellidos, @Dni, @Telefono, @Brevete, @Codusuario, @CodigoIbutton, '1');
                                 SELECT LAST_INSERT_ID();";

            return await _doConnection.QuerySingleAsync<int>(sql, conductor, transaction: _doTransaction);
        }

        // traccar_sync vuelve a 0: solo pasa a 1 cuando Traccar confirma el cambio.
        public async Task<int> Update(int codtaxi, ConductorTelemetriaRequest conductor)
        {
            const string sql = @"UPDATE taxi
                                 SET nombres = @Nombres,
                                     apellidos = @Apellidos,
                                     dni = @Dni,
                                     telefono = @Telefono,
                                     brevete = @Brevete,
                                     codusuario = @Codusuario,
                                     codigo_ibutton = @CodigoIbutton,
                                     traccar_sync = 0
                                 WHERE codtaxi = @Codtaxi AND telemetria = '1'";

            var parametros = new DynamicParameters(conductor);
            parametros.Add("Codtaxi", codtaxi);

            return await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
        }

        public async Task<int> Delete(int codtaxi)
        {
            const string sql = "DELETE FROM taxi WHERE codtaxi = @Codtaxi AND telemetria = '1'";

            return await _doConnection.ExecuteAsync(sql, new { Codtaxi = codtaxi }, transaction: _doTransaction);
        }

        public async Task<int> ActualizarEstadoTraccar(int codtaxi, int? traccarDriverId, bool traccarSync)
        {
            const string sql = @"UPDATE taxi
                                 SET traccar_driver_id = @TraccarDriverId, traccar_sync = @TraccarSync
                                 WHERE codtaxi = @Codtaxi AND telemetria = '1'";

            return await _doConnection.ExecuteAsync(
                sql,
                new { Codtaxi = codtaxi, TraccarDriverId = traccarDriverId, TraccarSync = traccarSync },
                transaction: _doTransaction);
        }
    }
}
