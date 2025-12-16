using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.RecorridoServicios;

namespace VelsatBackendAPI.Data.Repositories
{
    public class RecorridoRepository : IRecorridoRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public RecorridoRepository(IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        private (string fechaInicio, string fechaFin) RangoFechas(string fecha)
        {
            string fechaInicio = $"{fecha} 00:00";
            string fechaFin = $"{fecha} 23:59";
            return (fechaInicio, fechaFin);
        }

        public async Task<List<SelectedServicio>> GetSelectServicio(string fechaIngresada, string empresa, string usuario)
        {
            var (fechaInicio, fechaFin) = RangoFechas(fechaIngresada);

            const string sql = @"SELECT codservicio, numero, tipo, unidad, empresa FROM servicio WHERE fecha BETWEEN @Fechaini AND @Fechafin AND empresa = @Empresa AND codusuario = @Usuario AND unidad IS NOT NULL AND estado != 'C'";

            var parametros = new
            {
                Fechaini = fechaInicio,
                Fechafin = fechaFin,
                Empresa = empresa,
                Usuario = usuario
            };

            var resultado = await _doConnection.QueryAsync<SelectedServicio>(
                sql,
                parametros,
                transaction: _doTransaction);

            return resultado.ToList();
        }

        public async Task<RecorridoServicio> GetDatosServicio(string fecha, string numero, string empresa, string usuario)
        {
            var (fechaInicio, fechaFin) = RangoFechas(fecha);

            const string sql = @"SELECT codservicio, numero, tipo, unidad, empresa, fechaini, fechafin FROM servicio WHERE empresa = @Empresa AND codusuario = @Usuario AND fecha BETWEEN @Fechaini AND @Fechafin AND estado != 'C' AND numero = @Numero";

            var parameters = new
            {
                Fechaini = fechaInicio,
                Fechafin = fechaFin,
                Numero = numero,
                Empresa = empresa,
                Usuario = usuario
            };

            var result = await _doConnection.QueryAsync<RecorridoServicio>(
                sql,
                parameters,
                transaction: _doTransaction);

            return result.First();
        }

        public async Task<List<PasajeroServicio>> GetPasajerosServicio(string codservicio)
        {
            const string sqlClientes = @"
                SELECT codcliente 
                FROM subservicio 
                WHERE codservicio = @Codservicio AND codcliente <> '4175'";

            var codClientes = await _doConnection.QueryAsync<string>(
                sqlClientes,
                new { Codservicio = codservicio },
                transaction: _doTransaction);

            if (!codClientes.Any())
                return new List<PasajeroServicio>();

            const string sqlPasajeros = @"
                SELECT codlan, apellidos 
                FROM cliente 
                WHERE codcliente IN @Codclientes";

            var parametros = new { Codclientes = codClientes.Distinct().ToList() };

            var pasajerosDB = await _doConnection.QueryAsync<(string codlan, string apellidos)>(
                sqlPasajeros,
                parametros,
                transaction: _doTransaction);

            var resultado = pasajerosDB
                .Select((p, index) => new PasajeroServicio
                {
                    Id = index + 1,
                    Codlan = p.codlan,
                    Apellidos = p.apellidos
                })
                .ToList();

            return resultado;
        }
    }
}