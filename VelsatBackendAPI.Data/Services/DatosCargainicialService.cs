using Dapper;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Services
{
    public class DatosCargainicialService : IDatosCargainicialService
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;
        private List<Geocercausu> _geocercaUsuarios;
        private List<string> _deviceIds; // Variable global para los deviceIDs
        private string _currentLogin; // Para trackear si cambió el usuario

        public DatosCargainicialService(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
            _geocercaUsuarios = _defaultConnection.Query<Geocercausu>("SELECT codigo, descripcion, latitud, longitud FROM geocercausu", transaction: _defaultTransaction).ToList();
        }

        // Método para cargar los deviceIDs una sola vez por usuario
        private async Task<List<string>> GetDeviceIdsAsync(string login)
        {
            // Si ya tenemos los IDs para este usuario, los devolvemos
            if (_deviceIds != null && _currentLogin == login)
            {
                return _deviceIds;
            }

            // Cargar los deviceIDs
            const string sqlGetAllDeviceIds = @"
                SELECT DISTINCT deviceID 
                FROM (
                    SELECT deviceID FROM device WHERE accountID = @Login 
                    UNION ALL 
                    SELECT deviceID FROM gts.deviceuser WHERE userID = @Login AND Status = 1
                ) AS combined_devices";

            _deviceIds = (await _defaultConnection.QueryAsync<string>(sqlGetAllDeviceIds,
                new { Login = login }, transaction: _defaultTransaction)).ToList();
            _currentLogin = login;

            return _deviceIds;
        }

        public Geocercausu ObtenerGeocercausuPorCodigo(string codigo)
        {
            var geocercausu = _geocercaUsuarios.FirstOrDefault(gu => gu.Codigo.ToString() == codigo);
            return geocercausu;
        }

        public async Task<DatosCargainicial> ObtenerDatosCargaInicialAsync(string login)
        {
            // Obtener los deviceIDs (desde cache o BD)
            var deviceIds = await GetDeviceIdsAsync(login);

            if (!deviceIds.Any())
            {
                return new DatosCargainicial
                {
                    FechaActual = DateTime.Now,
                    DatosDevice = new List<Device>()
                };
            }

            // PRIMERA CONSULTA: Dispositivos
            const string sqlGetDevices = @"SELECT deviceID, lastValidLatitude, lastValidLongitude, lastOdometerKM, odometerini, kmini, description, direccion, codgeoact, lastValidHeading, lastValidSpeed, rutaact, servicio FROM device WHERE deviceID IN @DeviceIDs";

            var devices = (await _defaultConnection.QueryAsync<Device>(sqlGetDevices,
                new { DeviceIDs = deviceIds }, transaction: _defaultTransaction)).ToList();

            // Obtener los códigos de servicio únicos y no nulos
            var serviceCodes = devices
                .Where(d => !string.IsNullOrEmpty(d.Servicio))
                .Select(d => d.Servicio)
                .Distinct()
                .ToList();

            // SEGUNDA CONSULTA: Servicios (solo si hay códigos)
            Dictionary<string, Servicio> serviciosDict = new Dictionary<string, Servicio>();
            if (serviceCodes.Any())
            {
                const string sqlGetServicios = @"SELECT s.fecha, t.apellidos, s.numero, s.tipo, s.unidad, s.codservicio, s.empresa FROM servicio s, taxi t WHERE s.codconductor = t.codtaxi AND s.codservicio IN @Servicios";

                var serviciosData = await _defaultConnection.QueryAsync(sqlGetServicios, new { Servicios = serviceCodes }, transaction: _defaultTransaction);

                // Mapear manualmente a tu modelo Servicio
                var servicios = serviciosData.Select(row => new Servicio
                {
                    Codservicio = row.codservicio.ToString(),
                    Fecha = row.fecha,
                    Numero = row.numero,
                    Tipo = row.tipo,
                    Empresa = row.empresa,
                    Conductor = new Usuario
                    {
                        Apepate = row.apellidos
                    },
                    Unidad = new Unidad
                    {
                        Codunidad = row.unidad
                    }
                });

                serviciosDict = servicios.ToDictionary(s => s.Codservicio, s => s);
            }

            // Asignar geocercas Y servicios
            foreach (var device in devices)
            {
                device.DatosGeocercausu = ObtenerGeocercausuPorCodigo(device.Codgeoact);

                // NUEVO: Asignar el último servicio si existe
                if (!string.IsNullOrEmpty(device.Servicio) && serviciosDict.ContainsKey(device.Servicio))
                {
                    device.UltimoServicio = serviciosDict[device.Servicio];
                }
            }

            var datosCargaInicial = new DatosCargainicial
            {
                FechaActual = DateTime.Now,
                DatosDevice = devices
            };

            return datosCargaInicial;
        }

        public async Task<IEnumerable<SimplifiedDevice>> SimplifiedList(string login)
        {
            // Obtener los deviceIDs (desde cache o BD)
            var deviceIds = await GetDeviceIdsAsync(login);

            if (!deviceIds.Any())
            {
                return new List<SimplifiedDevice>();
            }

            // Consulta optimizada usando los IDs en cache
            const string sqlGetSimplifiedDevices = @"SELECT DISTINCT d.deviceID, d.rutaact, d.lastValidSpeed, d.lastValidLatitude, d.lastValidLongitude FROM device d WHERE d.deviceID IN @DeviceIds";

            var listDevices = await _defaultConnection.QueryAsync<SimplifiedDevice>(
                sqlGetSimplifiedDevices,
                new { DeviceIds = deviceIds },
                transaction: _defaultTransaction);

            return listDevices;
        }

        // Método opcional para limpiar el cache si es necesario
        public void ClearDeviceIdsCache()
        {
            _deviceIds = null;
            _currentLogin = null;
        }
    }
}