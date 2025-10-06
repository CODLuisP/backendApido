using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.RecorridoServicios;

namespace VelsatBackendAPI.Data.Repositories
{
    public class RecorridoRepository : IRecorridoRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction;

        public RecorridoRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        private (string fechaInicio, string fechaFin) RangoFechas(string fecha)
        {
            // Se espera que 'fecha' ya esté en formato "dd/MM/yyyy"
            string fechaInicio = $"{fecha} 00:00";
            string fechaFin = $"{fecha} 23:59";
            return (fechaInicio, fechaFin);
        }

        public async Task<List<SelectedServicio>> GetSelectServicio(string fechaIngresada)
        {
            var (fechaInicio, fechaFin) = RangoFechas(fechaIngresada);

            string sql = @"Select codservicio, numero, tipo, unidad, empresa from servicio where fecha between @Fechaini and @Fechafin and empresa = 'TALMA' and codusuario = 'cgacela' and unidad is not null and estado != 'C'";

            var parametros = new
            {
                Fechaini = fechaInicio,
                Fechafin = fechaFin
            };

            var resultado = await _defaultConnection.QueryAsync<SelectedServicio>(sql, parametros);

            return resultado.ToList();
        }


        public async Task<RecorridoServicio> GetDatosServicio(string fecha, string numero)
        {
            var (fechaInicio, fechaFin) = RangoFechas(fecha);

            string sql = @"select codservicio, numero, tipo, unidad, empresa, fechaini, fechafin from servicio where empresa = 'TALMA' and codusuario = 'cgacela' and fecha between @Fechaini and @Fechafin and estado != 'C' and numero = @Numero";

            var parameters = new
            {
                Fechaini = fechaInicio,
                Fechafin = fechaFin,
                Numero = numero
            };

            var result = await _defaultConnection.QueryAsync<RecorridoServicio>(sql, parameters);

            return result.First();
        }

        public async Task<List<PasajeroServicio>> GetPasajerosServicio(string codservicio)
        {
            var sqlClientes = @"Select codcliente from subservicio where codservicio = @Codservicio and codcliente <> '4175'";

            var codClientes = await _defaultConnection.QueryAsync<string>(sqlClientes, new { Codservicio = codservicio });

            if (!codClientes.Any())
                return new List<PasajeroServicio>();


            var sqlPasajeros = @"SELECT codlan, apellidos FROM cliente WHERE codcliente IN @Codclientes";

            var parametros = new { Codclientes = codClientes.Distinct().ToList() };

            var pasajerosDB = await _defaultConnection.QueryAsync<(string codlan, string apellidos)>(sqlPasajeros, parametros);

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
