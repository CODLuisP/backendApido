using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.KmServicioAremys;

namespace VelsatBackendAPI.Data.Repositories
{
    public class KmServicioRepository : IKmServicioRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbConnection _secondConnection;
        private readonly IDbTransaction _defaultTransaction;
        private readonly IDbTransaction _secondTransaction;

        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public KmServicioRepository(
            IDbConnection defaultConnection,
            IDbConnection secondConnection,
            IDbTransaction defaultTransaction,
            IDbTransaction secondTransaction,
            IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _defaultConnection = defaultConnection;
            _secondConnection = secondConnection;
            _defaultTransaction = defaultTransaction;
            _secondTransaction = secondTransaction;
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        public async Task<List<KilometrajeServicio>> GetKmServicios(string fechaIngresada)
        {
            var (fechaInicio, fechaFin) = RangoFechas(fechaIngresada);

            const string sql = @"
                SELECT codservicio, numero, tipo, codconductor, unidad, fechaini, fechafin, empresa 
                FROM servicio 
                WHERE fecha BETWEEN @Fechaini AND @Fechafin 
                  AND empresa = 'SASAA' 
                  AND codusuario = 'aremys' 
                  AND unidad IS NOT NULL 
                  AND estado != 'C'";

            var parametros = new { Fechaini = fechaInicio, Fechafin = fechaFin };

            var servicios = (await _doConnection.QueryAsync<KilometrajeServicio>(
                sql,
                parametros,
                transaction: _doTransaction)).ToList(); // ✅ Ya tiene transaction

            foreach (var servicio in servicios)
            {
                servicio.Tipo = servicio.Tipo == "I" ? "Ingreso" : "Salida";
                servicio.Empresa = servicio.Empresa == "SASAA" ? "SAASA" : servicio.Empresa;

                servicio.NombreConductor = await GetDetalleConductor(servicio.Codconductor);

                if (string.IsNullOrWhiteSpace(servicio.Fechaini) || string.IsNullOrWhiteSpace(servicio.Fechafin))
                {
                    continue;
                }

                var kmRecorrido = await ObtenerKilometrosRecorridos(
                    servicio.Fechaini,
                    servicio.Fechafin,
                    servicio.Unidad,
                    "aremys");

                servicio.KilometrosRecorridos = kmRecorrido.Kilometros;
            }

            return servicios;
        }

        private (string fechaInicio, string fechaFin) RangoFechas(string fecha)
        {
            string fechaInicio = $"{fecha} 00:00";
            string fechaFin = $"{fecha} 23:59";
            return (fechaInicio, fechaFin);
        }

        private async Task<KilometrosRecorridos> ObtenerKilometrosRecorridos(
            string fechaini,
            string fechafin,
            string deviceID,
            string accountID)
        {
            var resultado = await GetKmReporting(fechaini, fechafin, deviceID, accountID);
            var primerItem = resultado.ListaKilometros.FirstOrDefault();
            return primerItem ?? new KilometrosRecorridos { Kilometros = 0 };
        }

        private async Task<KilometrosReporting> GetKmReporting(
            string fechaini,
            string fechafin,
            string deviceID,
            string accountID)
        {
            int unixFechaInicio = ConvertToUnixTimestamp(fechaini);
            int unixFechaFin = ConvertToUnixTimestamp(fechafin);

            const string sql = @"
                SELECT tabla 
                FROM historicos 
                WHERE timeini <= @FechafinUnix AND timefin >= @FechainiUnix";

            var nombresTablas = (await _defaultConnection.QueryAsync<Historicos>(
                sql,
                new { FechainiUnix = unixFechaInicio, FechafinUnix = unixFechaFin },
                transaction: _defaultTransaction)).ToList(); // ✅ Ya tiene transaction

            var kilometrosReporting = new KilometrosReporting
            {
                ListaKilometros = new List<KilometrosRecorridos>()
            };

            if (nombresTablas.Count == 0)
            {
                const string sqlEventData = @"
                    SELECT deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, 
                           (MAX(odometerKM) - MIN(odometerKM)) AS kilometros 
                    FROM eventdata 
                    WHERE accountID = @AccountID 
                      AND deviceID = @DeviceID 
                      AND timestamp BETWEEN @FechainiUnix AND @FechafinUnix 
                    GROUP BY deviceID";

                kilometrosReporting.ListaKilometros = (await _defaultConnection.QueryAsync<KilometrosRecorridos>(
                    sqlEventData,
                    new { AccountID = accountID, DeviceID = deviceID, FechainiUnix = unixFechaInicio, FechafinUnix = unixFechaFin },
                    transaction: _defaultTransaction)).ToList(); // ✅ Ya tiene transaction
            }
            else
            {
                foreach (var nombreTabla in nombresTablas)
                {
                    string consultaTabla = nombreTabla.Tabla;

                    string sqlR = $@"
                        SELECT deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, 
                               (MAX(odometerKM) - MIN(odometerKM)) AS kilometros 
                        FROM {consultaTabla} 
                        WHERE accountID = @AccountID 
                          AND deviceID = @DeviceID 
                          AND timestamp BETWEEN @FechainiUnix AND @FechafinUnix 
                        GROUP BY deviceID";

                    var datosTabla = (await _secondConnection.QueryAsync<KilometrosRecorridos>(
                        sqlR,
                        new { AccountID = accountID, DeviceID = deviceID, FechainiUnix = unixFechaInicio, FechafinUnix = unixFechaFin },
                        transaction: _secondTransaction)).ToList(); // ✅ Ya tiene transaction

                    kilometrosReporting.ListaKilometros.AddRange(datosTabla);
                }
            }

            for (int i = 0; i < kilometrosReporting.ListaKilometros.Count; i++)
            {
                kilometrosReporting.ListaKilometros[i].Item = i + 1;
            }

            if (kilometrosReporting.ListaKilometros.Count == 0)
            {
                return new KilometrosReporting
                {
                    Mensaje = "No se encontró datos disponibles en el rango de fechas ingresado"
                };
            }

            return kilometrosReporting;
        }

        public int ConvertToUnixTimestamp(string fecha)
        {
            if (DateTime.TryParseExact(
                fecha,
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fechaConvertida))
            {
                DateTimeOffset dateTimeOffset = new DateTimeOffset(fechaConvertida);
                return (int)dateTimeOffset.ToUnixTimeSeconds();
            }
            else
            {
                throw new ArgumentException("Formato de fecha inválido. Usa 'dd/MM/yyyy HH:mm'.");
            }
        }

        private async Task<string> GetDetalleConductor(string codtaxi)
        {
            if (!int.TryParse(codtaxi, out int codtaxiInt))
            {
                return null;
            }

            const string sql = "SELECT apellidos FROM taxi WHERE estado = 'A' AND codtaxi = @Codtaxi";

            var result = await _doConnection.QueryFirstOrDefaultAsync<string>(
                sql,
                new { Codtaxi = codtaxiInt },
                transaction: _doTransaction); // ✅ AGREGAR ESTO

            return result;
        }
    }
}