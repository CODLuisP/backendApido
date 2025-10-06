using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Data.Repositories
{
    public class TurnosRepository : ITurnosRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction;

        public TurnosRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        public async Task<IEnumerable<TurnoAvianca>> GetTurnos(string accountID)
        {
            const string sql = "Select * from turnoavianca where usuario = @AccountID and eliminado = '0' order by area, subarea, codrl";
            return await _defaultConnection.QueryAsync<TurnoAvianca>(sql, new { AccountID = accountID });
        }

        public async Task<string> InsertTurno(TurnoAvianca turno, string accountID)
        {
            const string sql = "insert into turnoavianca (codrl, hora, tipo, subarea, area, programa, empresa, usuario) " +
                "values (@Codrl,@Hora, @Tipo, @Subarea, @Area, @Programa, @Empresa, @Usuario)";

            var parameters = new
            {
                CodRL = turno.Codrl,
                Hora = turno.Hora,
                Tipo = turno.Tipo,
                Subarea = turno.Subarea,
                Area = turno.Area,
                Programa = turno.Programa,
                Empresa = turno.Empresa,
                Usuario = accountID,
            };

            var result = await _defaultConnection.QueryAsync<TurnoAvianca>(sql, parameters, _defaultTransaction);

            _defaultTransaction.Commit();

            return "Success insertion";
        }

        public async Task<string> UpdateTurno(TurnoAvianca turno, string codigo)
        {
            const string sql = "update turnoavianca set codrl=@Codrl, hora=@Hora, tipo=@Tipo, subarea=@Subarea, area=@Area, programa=@Programa where codigo=@Codigo";

            var parameters = new
            {
                Codigo = codigo,
                CodRL = turno.Codrl,
                Hora = turno.Hora,
                Tipo = turno.Tipo,
                Subarea = turno.Subarea,
                Area = turno.Area,
                Programa = turno.Programa
            };

            var result = await _defaultConnection.QueryAsync<TurnoAvianca>(sql, parameters, _defaultTransaction);

            _defaultTransaction.Commit();

            return "Success update";
        }

        public async Task<string> DeleteTurno(string codigo)
        {
            const string sql = "update turnoavianca set eliminado = '1' where codigo = @Codigo";

            var result = await _defaultConnection.QueryAsync<TurnoAvianca>(sql, new { Codigo = codigo }, _defaultTransaction);

            _defaultTransaction.Commit();

            return "Success delete";
        }

        public async Task<IEnumerable<string>> GetListFilter(string campo, string accountID)
        {
            var sql = $"Select {campo} from turnoavianca where usuario = @AccountID group by {campo} order by {campo}";

            return await _defaultConnection.QueryAsync<string>(sql, new { AccountID = accountID }, _defaultTransaction);
        }
    }
}
