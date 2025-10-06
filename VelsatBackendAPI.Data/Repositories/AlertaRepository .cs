using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.AlarmasCorreo;

namespace VelsatBackendAPI.Data.Repositories
{
    public class AlertaRepository : IAlertaRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction;

        public AlertaRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        public async Task<DateTime?> ObtenerFechaUltimaAlarmaAsync()
        {
            string sql = "SELECT fecha FROM tablaalarma WHERE id = 1";
            return await _defaultConnection.QueryFirstOrDefaultAsync<DateTime?>(sql);
        }

        public async Task<List<RegistroAlarmas>> ObtenerAlertasNoEnviadasAsync()
        {
            string sql = "SELECT * FROM registroalarmas WHERE isEnviado = 0";
            var result = await _defaultConnection.QueryAsync<RegistroAlarmas>(sql);
            return result.ToList();
        }

        public async Task MarcarComoEnviadasAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return; // No hacer nada si no hay ids

            string sql = "UPDATE registroalarmas SET isEnviado = 1 WHERE Codigo IN @Ids";
            await _defaultConnection.ExecuteAsync(sql, new { Ids = ids });

            _defaultTransaction?.Commit(); // Si estás usando transacción opcional
        }
    }
}
