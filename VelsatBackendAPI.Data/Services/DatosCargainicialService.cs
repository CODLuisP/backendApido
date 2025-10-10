using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Services
{
    public class DatosCargainicialService : IDatosCargainicialService
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;
        private List<Geocercausu> _geocercaUsuarios;
        private List<string> _deviceIds;
        private string _currentLogin;

        public DatosCargainicialService(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
        {
            _defaultConnection = defaultConnection;
            _defaultTransaction = defaultTransaction;
            // NO cargar datos aquí - lazy loading
        }

        // ⭐ CORREGIDO: Ahora es async y pasa la transacción
        private async Task<List<Geocercausu>> GetGeocercasAsync()
        {
            if (_geocercaUsuarios == null)
            {
                const string sql = "SELECT codigo, descripcion, latitud, longitud FROM geocercausu";

                var result = await _defaultConnection.QueryAsync<Geocercausu>(
                    sql,
                    transaction: _defaultTransaction); // ⭐ Pasa la transacción

                _geocercaUsuarios = result.ToList();
            }
            return _geocercaUsuarios;
        }

        // ⭐ CORREGIDO: Pasa la transacción
        private async Task<List<string>> GetDeviceIdsAsync(string login)
        {
            if (_deviceIds != null && _currentLogin == login)
            {
                return _deviceIds;
            }

            const string sqlGetAllDeviceIds = @"
                SELECT DISTINCT deviceID 
                FROM (
                    SELECT deviceID FROM device WHERE accountID = @Login 
                    UNION ALL 
                    SELECT deviceID FROM gts.deviceuser WHERE userID = @Login AND Status = 1
                ) AS combined_devices";

            var result = await _defaultConnection.QueryAsync<string>(
                sqlGetAllDeviceIds,
                new { Login = login },
                transaction: _defaultTransaction); // ⭐ Ya tenía la transacción ✅

            _deviceIds = result.ToList();
            _currentLogin = login;

            return _deviceIds;
        }

        // ⭐ CORREGIDO: Ahora es async
        public async Task<Geocercausu> ObtenerGeocercausuPorCodigoAsync(string codigo)
        {
            var geocercas = await GetGeocercasAsync(); // ⭐ Ahora llama al método async
            return geocercas.FirstOrDefault(gu => gu.Codigo.ToString() == codigo);
        }

        // ⭐ CORREGIDO: Usa la versión async de ObtenerGeocercausuPorCodigo
        public async Task<DatosCargainicial> ObtenerDatosCargaInicialAsync(string login)
        {
            var deviceIds = await GetDeviceIdsAsync(login);

            if (!deviceIds.Any())
            {
                return new DatosCargainicial
                {
                    FechaActual = DateTime.Now,
                    DatosDevice = new List<Device>()
                };
            }

            const string sqlGetDevices = @"
                SELECT deviceID, lastValidLatitude, lastValidLongitude, lastOdometerKM, 
                       odometerini, kmini, description, direccion, codgeoact, 
                       lastValidHeading, lastValidSpeed, rutaact, servicio 
                FROM device 
                WHERE deviceID IN @DeviceIDs";

            var devicesResult = await _defaultConnection.QueryAsync<Device>(
                sqlGetDevices,
                new { DeviceIDs = deviceIds },
                transaction: _defaultTransaction); // ⭐ Ya tenía la transacción ✅

            var devices = devicesResult.ToList();

            var serviceCodes = devices
                .Where(d => !string.IsNullOrEmpty(d.Servicio))
                .Select(d => d.Servicio)
                .Distinct()
                .ToList();

            Dictionary<string, Servicio> serviciosDict = new Dictionary<string, Servicio>();

            if (serviceCodes.Any())
            {
                const string sqlGetServicios = @"
                    SELECT s.fecha, t.apellidos, s.numero, s.tipo, s.unidad, 
                           s.codservicio, s.empresa 
                    FROM servicio s, taxi t 
                    WHERE s.codconductor = t.codtaxi 
                      AND s.codservicio IN @Servicios";

                var serviciosData = await _defaultConnection.QueryAsync(
                    sqlGetServicios,
                    new { Servicios = serviceCodes },
                    transaction: _defaultTransaction); // ⭐ Ya tenía la transacción ✅

                var servicios = serviciosData.Select(row => new Servicio
                {
                    Codservicio = row.codservicio.ToString(),
                    Fecha = row.fecha,
                    Numero = row.numero,
                    Tipo = row.tipo,
                    Empresa = row.empresa,
                    Conductor = new Usuario { Apepate = row.apellidos },
                    Unidad = new Unidad { Codunidad = row.unidad }
                });

                serviciosDict = servicios.ToDictionary(s => s.Codservicio, s => s);
            }

            // ⭐ CORREGIDO: Ahora usa await para cada llamada async
            foreach (var device in devices)
            {
                device.DatosGeocercausu = await ObtenerGeocercausuPorCodigoAsync(device.Codgeoact); // ⭐ Async

                if (!string.IsNullOrEmpty(device.Servicio) &&
                    serviciosDict.ContainsKey(device.Servicio))
                {
                    device.UltimoServicio = serviciosDict[device.Servicio];
                }
            }

            return new DatosCargainicial
            {
                FechaActual = DateTime.Now,
                DatosDevice = devices
            };
        }

        // ⭐ CORREGIDO: Ya tenía la transacción ✅
        public async Task<IEnumerable<SimplifiedDevice>> SimplifiedList(string login)
        {
            var deviceIds = await GetDeviceIdsAsync(login);

            if (!deviceIds.Any())
            {
                return new List<SimplifiedDevice>();
            }

            const string sqlGetSimplifiedDevices = @"
                SELECT DISTINCT d.deviceID, d.rutaact, d.lastValidSpeed, 
                       d.lastValidLatitude, d.lastValidLongitude 
                FROM device d 
                WHERE d.deviceID IN @DeviceIds";

            var listDevices = await _defaultConnection.QueryAsync<SimplifiedDevice>(
                sqlGetSimplifiedDevices,
                new { DeviceIds = deviceIds },
                transaction: _defaultTransaction); // ⭐ Ya tenía la transacción ✅

            return listDevices;
        }

        public void ClearDeviceIdsCache()
        {
            _deviceIds = null;
            _currentLogin = null;
        }
    }
}