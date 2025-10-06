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
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction;

        public UserRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        /* private readonly MySqlConfiguration _configuration;
         public UserRepository(MySqlConfiguration configuration)
         {
             _configuration = configuration;
         }

         protected MySqlConnection dbconnection()
         {
             return new MySqlConnection(_configuration.ConnectionString);
         }*/

        public Task<IEnumerable<Account>> GetAllUsers() //Task es siempre asíncrono
        {
            return _defaultConnection.QueryAsync<Account>("Select accountID, password, description from usuarios", new { });
        }

        public Task<Account> GetDetails(int id)
        {
            return _defaultConnection.QueryFirstOrDefaultAsync<Account>("Select accountID, password, description from usuarios where accountID = @id", new {id});
        }

        public Task<bool> InsertUser(Account account)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUser(Account account)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUser(Account account)
        {
            throw new NotImplementedException();
        }

        public Task<Account> ValidarUser(string login, string clave)
        {
            const string sql = "Select accountID, password from usuarios where accountID = @login and password = @clave";
            return _defaultConnection.QueryFirstOrDefaultAsync<Account>(sql, new { login = login, clave = clave });
        }
    }
}
