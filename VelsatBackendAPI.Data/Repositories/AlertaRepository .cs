using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.AlarmasCorreo;

namespace VelsatBackendAPI.Data.Repositories
{
    public class AlertaRepository : IAlertaRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        public AlertaRepository(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
        {
            _defaultConnection = defaultConnection;
            _defaultTransaction = defaultTransaction;
        }

        public async Task<DateTime?> ObtenerFechaUltimaAlarmaAsync()
        {
            const string sql = "SELECT fecha FROM tablaalarma WHERE id = 1";
            return await _defaultConnection.QueryFirstOrDefaultAsync<DateTime?>(
                sql,
                transaction: _defaultTransaction); // ✅ Agregar transaction
        }

        public async Task<List<RegistroAlarmas>> ObtenerAlertasNoEnviadasAsync()
        {
            const string sql = "SELECT * FROM registroalarmas WHERE isEnviado = 0";
            var result = await _defaultConnection.QueryAsync<RegistroAlarmas>(
                sql,
                transaction: _defaultTransaction); // ✅ Agregar transaction
            return result.ToList();
        }

        public async Task MarcarComoEnviadasAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return;

            const string sql = "UPDATE registroalarmas SET isEnviado = 1 WHERE Codigo IN @Ids";
            await _defaultConnection.ExecuteAsync(
                sql,
                new { Ids = ids },
                transaction: _defaultTransaction); // ✅ Agregar transaction
        }
    }
}