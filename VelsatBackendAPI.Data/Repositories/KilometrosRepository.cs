using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public class KilometrosRepository : IKilometrosRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbConnection _secondConnection;
        private readonly IDbTransaction _defaultTransaction;
        private readonly IDbTransaction _secondTransaction;

        public KilometrosRepository(IDbConnection defaultConnection, IDbConnection secondConnection, IDbTransaction defaulttransaction, IDbTransaction secondTransaction)
        {
            _defaultConnection = defaultConnection;
            _secondConnection = secondConnection;
            _defaultTransaction = defaulttransaction;
            _secondTransaction = secondTransaction;

        }

        public async Task<KilometrosReporting> GetKmReporting(string fechaini, string fechafin, string deviceID, string accountID)
        {
            accountID = await ObtenerAccountIDCorrecto(deviceID, accountID);

            if (string.IsNullOrEmpty(accountID))
            {
                return new KilometrosReporting
                {
                    Mensaje = $"No se encontró información para el deviceID: {deviceID}"
                };
            }

            var dates = FormatDate(fechaini, fechafin);

            fechaini = dates.dateStart;
            fechafin = dates.dateEnd;

            var resultadoDias = CalcularDias(fechaini, fechafin);

            double numdias = resultadoDias.NumDias;

            if (numdias <= 5)
            {
                const string sql = "select tabla from historicos where timeini<=@FechafinUnix and timefin>=@FechainiUnix";

                var nombresTablas = _defaultConnection.Query<Historicos>(sql, new { FechainiUnix = resultadoDias.UnixFechaInicio, FechafinUnix = resultadoDias.UnixFechaFin }, transaction: _defaultTransaction).ToList();

                var kilometrosReporting = new KilometrosReporting
                {
                    ListaKilometros = new List<KilometrosRecorridos>()
                };

                if (nombresTablas.Count == 0)
                {
                    // Si no se encontraron nombres de tablas, consultamos directamente la tabla "eventdata"
                    string sqlEventData = @"select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from eventdata where accountID = @AccountID and deviceID = @DeviceID and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";

                    kilometrosReporting.ListaKilometros = _defaultConnection.Query<KilometrosRecorridos>(sqlEventData, new { AccountID = accountID, DeviceID = deviceID, FechainiUnix = resultadoDias.UnixFechaInicio, FechafinUnix = resultadoDias.UnixFechaFin }, transaction: _defaultTransaction).ToList();

                    for (int i = 0; i < kilometrosReporting.ListaKilometros.Count; i++)
                    {
                        kilometrosReporting.ListaKilometros[i].Item = i + 1;
                    }

                }

                else

                {
                    foreach (var nombreTabla in nombresTablas)
                    {
                        string consultaTabla = nombreTabla.Tabla;

                        string sqlR = $@"select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from {consultaTabla} where accountID = @AccountID and deviceID = @DeviceID and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";

                        var datosTabla = _secondConnection.Query<KilometrosRecorridos>(sqlR, new { AccountID = accountID, DeviceID = deviceID, FechainiUnix = resultadoDias.UnixFechaInicio, FechafinUnix = resultadoDias.UnixFechaFin }, transaction: _secondTransaction).ToList();

                        kilometrosReporting.ListaKilometros.AddRange(datosTabla);

                    }

                    for (int i = 0; i < kilometrosReporting.ListaKilometros.Count; i++)
                    {
                        kilometrosReporting.ListaKilometros[i].Item = i + 1;
                    }
                }

                if (kilometrosReporting.ListaKilometros.Count == 0 || kilometrosReporting.ListaKilometros == null)
                {
                    return new KilometrosReporting
                    {
                        Mensaje = "No se encontro datos disponible en el rango de fechas ingresado"
                    };
                }

                return kilometrosReporting;
            }
            else
            {
                return new KilometrosReporting
                {
                    Mensaje = "La diferencia entre las fechas es mayor a 5 días; seleccione otra fechas"
                };
            }
        }

        public async Task<KilometrosReporting> GetAllKmReporting(string fechaini, string fechafin, string accountID)
        {
            var dates = FormatDate(fechaini, fechafin);
            fechaini = dates.dateStart;
            fechafin = dates.dateEnd;

            var resultadoDias = CalcularDias(fechaini, fechafin);
            double numdias = resultadoDias.NumDias;

            var kilometrosReporting = new KilometrosReporting
            {
                ListaKilometros = new List<KilometrosRecorridos>()
            };

            if (numdias <= 5)
            {
                // Consultar la tabla "historicos"
                const string sql = "select tabla from historicos where timeini <= @FechafinUnix and timefin >= @FechainiUnix";
                var nombresTablas = _defaultConnection.Query<Historicos>(sql, new { FechainiUnix = resultadoDias.UnixFechaInicio, FechafinUnix = resultadoDias.UnixFechaFin }, transaction: _defaultTransaction).ToList();

                if (nombresTablas.Count == 0)
                {
                    // Consultar directamente la tabla "eventdata"
                    string sqlEventData = $"select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo " +
                        $"from eventdata where deviceID in (select deviceID from device where accountID=@AccountID) and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";


                    var parameters = new DynamicParameters();
                    parameters.Add("FechainiUnix", resultadoDias.UnixFechaInicio);
                    parameters.Add("FechafinUnix", resultadoDias.UnixFechaFin);
                    parameters.Add("AccountID", accountID);

                    kilometrosReporting.ListaKilometros = _defaultConnection.Query<KilometrosRecorridos>(sqlEventData, parameters, transaction: _defaultTransaction).ToList();
                }
                else
                {
                    foreach (var nombreTabla in nombresTablas)
                    {
                        string consultaTabla = nombreTabla.Tabla;

                        // Consultar cada tabla histórica
                        string sqlR = $"select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo " +
                            $"from {consultaTabla} where deviceID in (select deviceID from gts.device where accountID=@AccountID) and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";

                        var parameters = new DynamicParameters();
                        parameters.Add("FechainiUnix", resultadoDias.UnixFechaInicio);
                        parameters.Add("FechafinUnix", resultadoDias.UnixFechaFin);
                        parameters.Add("AccountID", accountID);

                        var datosTabla = _secondConnection.Query<KilometrosRecorridos>(sqlR, parameters, transaction: _secondTransaction).ToList();
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
                        Mensaje = "No se encontró datos disponible en el rango de fechas ingresado"
                    };
                }

                return kilometrosReporting;
            }
            else
            {
                return new KilometrosReporting
                {
                    Mensaje = "La diferencia entre las fechas es mayor a 5 días; seleccione otra fechas"
                };
            }
        }

        public async Task<KilometrajeBatchReporting> GetKmReportingBatch(KilometrajeBatchRequest request)
        {
            var reporting = new KilometrajeBatchReporting();

            var rangosRecibidos = request?.Rangos ?? new List<RangoKilometrajeRequest>();
            var rangosValidos = new List<(string RangoId, string DeviceId, int Ini, int Fin)>();

            foreach (var rango in rangosRecibidos)
            {
                if (string.IsNullOrWhiteSpace(rango.DeviceID) || string.IsNullOrWhiteSpace(rango.FechaIni) || string.IsNullOrWhiteSpace(rango.FechaFin))
                    continue;

                if (!TryParseFechaHoraUnix(rango.FechaIni, out int ini) || !TryParseFechaHoraUnix(rango.FechaFin, out int fin))
                    continue;

                if (fin <= ini)
                    continue;

                rangosValidos.Add((rango.RangoId, rango.DeviceID.Trim().ToUpperInvariant(), ini, fin));
            }

            if (rangosValidos.Count == 0)
            {
                reporting.Mensaje = $"No se recibieron rangos válidos para consultar kilometraje (recibidos: {rangosRecibidos.Count})";
                return reporting;
            }

            int fechainiUnixGlobal = rangosValidos.Min(r => r.Ini);
            int fechafinUnixGlobal = rangosValidos.Max(r => r.Fin);

            const string sqlHistoricos = "select tabla from historicos where timeini<=@FechafinUnix and timefin>=@FechainiUnix";
            var nombresTablas = _defaultConnection.Query<Historicos>(sqlHistoricos, new { FechainiUnix = fechainiUnixGlobal, FechafinUnix = fechafinUnixGlobal }, transaction: _defaultTransaction).ToList();

            var accountIdPorDevice = ObtenerAccountIdsPorDevice(rangosValidos.Select(r => r.DeviceId).Distinct().ToList());

            var rangosConCuenta = rangosValidos
                .Select(r => (
                    r.RangoId,
                    r.DeviceId,
                    r.Ini,
                    r.Fin,
                    AccountId: accountIdPorDevice.TryGetValue(r.DeviceId, out var acc) && !string.IsNullOrEmpty(acc)
                        ? acc
                        : request.AccountID))
                .Where(r => !string.IsNullOrWhiteSpace(r.AccountId))
                .ToList();

            var crudo = new List<KilometrosRecorridosServicio>();

            if (nombresTablas.Count == 0)
            {
                crudo.AddRange(EjecutarQueryBatch(_defaultConnection, _defaultTransaction, "eventdata", rangosConCuenta));
            }
            else
            {
                foreach (var nombreTabla in nombresTablas)
                {
                    crudo.AddRange(EjecutarQueryBatch(_secondConnection, _secondTransaction, nombreTabla.Tabla, rangosConCuenta));
                }
            }

            reporting.Resultados = crudo
                .GroupBy(r => new { r.RangoId, r.DeviceId })
                .Select(g => new KilometrosRecorridosServicio
                {
                    RangoId = g.Key.RangoId,
                    DeviceId = g.Key.DeviceId,
                    Maximo = g.Max(x => x.Maximo),
                    Minimo = g.Min(x => x.Minimo),
                    Kilometros = g.Max(x => x.Maximo) - g.Min(x => x.Minimo),
                })
                .ToList();

            if (reporting.Resultados.Count == 0)
            {
                reporting.Mensaje = "No se encontró datos disponibles para el rango de fechas ingresado";
            }

            return reporting;
        }

        private class DeviceAccountRow
        {
            public string DeviceID { get; set; }
            public string AccountID { get; set; }
        }

        private Dictionary<string, string> ObtenerAccountIdsPorDevice(List<string> deviceIds)
        {
            if (deviceIds.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            const string sql = "SELECT deviceID AS DeviceID, accountID AS AccountID FROM device WHERE deviceID IN @DeviceIds";

            var filas = _defaultConnection.Query<DeviceAccountRow>(sql, new { DeviceIds = deviceIds }, transaction: _defaultTransaction).ToList();

            return filas
                .Where(f => !string.IsNullOrEmpty(f.DeviceID))
                .GroupBy(f => f.DeviceID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().AccountID, StringComparer.OrdinalIgnoreCase);
        }

        private static bool TryParseFechaHoraUnix(string fechaHora, out int unix)
        {
            unix = 0;

            if (!DateTime.TryParseExact(
                    fechaHora.Trim(),
                    new[] { "dd/MM/yyyy HH:mm", "d/M/yyyy H:mm" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fecha))
            {
                return false;
            }

            unix = (int)(fecha.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
            return true;
        }

        private List<KilometrosRecorridosServicio> EjecutarQueryBatch(
            IDbConnection connection,
            IDbTransaction transaction,
            string tabla,
            List<(string RangoId, string DeviceId, int Ini, int Fin, string AccountId)> rangos)
        {
            var parameters = new DynamicParameters();
            var filasUnion = new List<string>();

            for (int i = 0; i < rangos.Count; i++)
            {
                var r = rangos[i];

                filasUnion.Add($"SELECT @RangoId{i} AS rangoId, @DeviceID{i} AS deviceID, @AccountID{i} AS accountID, @Ini{i} AS ini, @Fin{i} AS fin");

                parameters.Add($"RangoId{i}", r.RangoId);
                parameters.Add($"DeviceID{i}", r.DeviceId);
                parameters.Add($"AccountID{i}", r.AccountId);
                parameters.Add($"Ini{i}", r.Ini);
                parameters.Add($"Fin{i}", r.Fin);
            }

            string sql = $@"
                SELECT r.rangoId AS RangoId, e.deviceID AS DeviceId,
                       MAX(e.odometerKM) AS Maximo, MIN(e.odometerKM) AS Minimo
                FROM {tabla} e
                INNER JOIN (
                    {string.Join(" UNION ALL ", filasUnion)}
                ) r ON e.accountID = r.accountID AND e.deviceID = r.deviceID AND e.timestamp BETWEEN r.ini AND r.fin
                GROUP BY r.rangoId, e.deviceID";

            return connection.Query<KilometrosRecorridosServicio>(sql, parameters, transaction: transaction).ToList();
        }

        public class ResultadosCalculoDias
        {
            public double NumDias { get; set; }
            public int UnixFechaInicio { get; set; }
            public int UnixFechaFin { get; set; }
        }

        public int DateUnix(string fecha)
        {
            fecha = WebUtility.UrlDecode(fecha);

            DateTime fechaTime = DateTime.ParseExact(fecha, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            int unixFecha = (int)(fechaTime.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;

            return unixFecha;
        }

        public ResultadosCalculoDias CalcularDias(string fechaI, string fechaF)
        {
            int fechainiUnix = DateUnix(fechaI);
            int fechafinUnix = DateUnix(fechaF);

            int totalsegundos = fechafinUnix - fechainiUnix;
            double numdias = (double)totalsegundos / 86400;

            return new ResultadosCalculoDias
            {
                NumDias = numdias,
                UnixFechaInicio = fechainiUnix,
                UnixFechaFin = fechafinUnix,
            };
        }

        public class ResultDate
        {
            public string dateStart { get; set; }
            public string dateEnd { get; set; }

        }

        public ResultDate FormatDate(string dateS, string dateE)
        {

            DateTime.TryParse(dateS, out DateTime fechaInicio);
            DateTime.TryParse(dateE, out DateTime fechaFin);


            string fechaInicioString = fechaInicio.ToString("dd/MM/yyyy HH:mm");
            string fechaFinString = fechaFin.ToString("dd/MM/yyyy HH:mm");

            return new ResultDate
            {
                dateStart = fechaInicioString,
                dateEnd = fechaFinString,
            };
        }

        private async Task<string> ObtenerAccountIDCorrecto(string deviceID, string accountID)
        {
            const string sqlAccountFromDevice = "SELECT accountID FROM device WHERE deviceID = @DeviceID";

            var newAccountID = _defaultConnection.QueryFirstOrDefault<string>(
                sqlAccountFromDevice,
                new { DeviceID = deviceID },
                transaction: _defaultTransaction);

            if (!string.IsNullOrEmpty(newAccountID))
            {
                return newAccountID; // ✅ Retorna el accountID de la BD
            }
            else
            {
                if (string.IsNullOrEmpty(accountID))
                {
                    return null; // ❌ No se encontró accountID
                }
                return accountID; // ✅ Retorna el accountID del parámetro
            }
        }
    }
}
