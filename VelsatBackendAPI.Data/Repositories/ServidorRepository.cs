using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public class ServidorRepository : IServidorRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        // ⭐ CONSTRUCTOR ACTUALIZADO: Ahora recibe la transacción
        public ServidorRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        public async Task<Servidor> GetServidor(string accountID)
        {
            const string sql = "SELECT servidor FROM serverprueba WHERE loginusu = @AccountID";

            // ⭐ IMPORTANTE: Pasar la transacción a Dapper
            return await _defaultConnection.QueryFirstOrDefaultAsync<Servidor>(sql, new { AccountID = accountID }, transaction: _defaultTransaction);
        }
    }
}