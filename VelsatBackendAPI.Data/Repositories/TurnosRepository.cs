using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Data.Repositories
{
    public class TurnosRepository : ITurnosRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public TurnosRepository(IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        public async Task<IEnumerable<TurnoAvianca>> GetTurnos(string accountID)
        {
            const string sql = @"
                SELECT * FROM turnoavianca 
                WHERE usuario = @AccountID AND eliminado = '0' 
                ORDER BY area, subarea, codrl";

            return await _doConnection.QueryAsync<TurnoAvianca>(
                sql,
                new { AccountID = accountID },
                transaction: _doTransaction);
        }

        public async Task<string> InsertTurno(TurnoAvianca turno, string accountID)
        {
            const string sql = @"
                INSERT INTO turnoavianca (codrl, hora, tipo, subarea, area, programa, empresa, usuario) 
                VALUES (@Codrl, @Hora, @Tipo, @Subarea, @Area, @Programa, @Empresa, @Usuario)";

            var parameters = new
            {
                Codrl = turno.Codrl,
                Hora = turno.Hora,
                Tipo = turno.Tipo,
                Subarea = turno.Subarea,
                Area = turno.Area,
                Programa = turno.Programa,
                Empresa = turno.Empresa,
                Usuario = accountID
            };

            await _doConnection.ExecuteAsync(sql, parameters, _doTransaction);

            return "Success insertion";
        }

        public async Task<string> UpdateTurno(TurnoAvianca turno, string codigo)
        {
            const string sql = @"
                UPDATE turnoavianca 
                SET codrl = @Codrl, hora = @Hora, tipo = @Tipo, 
                    subarea = @Subarea, area = @Area, programa = @Programa 
                WHERE codigo = @Codigo";

            var parameters = new
            {
                Codigo = codigo,
                Codrl = turno.Codrl,
                Hora = turno.Hora,
                Tipo = turno.Tipo,
                Subarea = turno.Subarea,
                Area = turno.Area,
                Programa = turno.Programa
            };

            await _doConnection.ExecuteAsync(sql, parameters, _doTransaction);

            return "Success update";
        }

        public async Task<string> DeleteTurno(string codigo)
        {
            const string sql = "UPDATE turnoavianca SET eliminado = '1' WHERE codigo = @Codigo";

            await _doConnection.ExecuteAsync(sql, new { Codigo = codigo }, _doTransaction);

            return "Success delete";
        }

        public async Task<IEnumerable<string>> GetListFilter(string campo, string accountID)
        {
            var sql = $@"
                SELECT {campo} FROM turnoavianca 
                WHERE usuario = @AccountID 
                GROUP BY {campo} 
                ORDER BY {campo}";

            return await _doConnection.QueryAsync<string>(
                sql,
                new { AccountID = accountID },
                transaction: _doTransaction);
        }
    }
}