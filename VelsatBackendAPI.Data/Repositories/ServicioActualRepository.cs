using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    // Las posiciones (device) viven en la base local (DefaultConnection); los servicios, en la base DO.
    // Por eso las placas del usuario se leen de una y los servicios de la otra.
    public class ServicioActualRepository : IServicioActualRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbConnection _doConnection;

        public ServicioActualRepository(IDbConnection defaultConnection, IDbConnection doConnection)
        {
            _defaultConnection = defaultConnection;
            _doConnection = doConnection;
        }

        public async Task<List<ServicioActual>> ObtenerServiciosActuales(string login)
        {
            // Mismas placas que usa DeviceList para este usuario.
            const string sqlDeviceIds = @"
                SELECT DISTINCT deviceID
                FROM (
                    SELECT deviceID FROM device WHERE accountID = @Login
                    UNION ALL
                    SELECT deviceID FROM gts.deviceuser WHERE userID = @Login AND Status = 1
                ) AS combined_devices";

            var deviceIds = (await _defaultConnection.QueryAsync<string>(sqlDeviceIds, new { Login = login })).ToList();
            if (deviceIds.Count == 0)
                return new List<ServicioActual>();

            // Por placa: primero el servicio en curso (status 2) y, entre iguales, el más reciente.
            // Programado = status 1. Finalizado (3) no se devuelve: la unidad queda "sin servicio".
            const string sqlServicios = @"
                SELECT DeviceId, Codservicio, Status, Numero, Tipo, Empresa, Conductor
                FROM (
                    SELECT s.unidad AS DeviceId,
                           s.codservicio AS Codservicio,
                           s.status AS Status,
                           s.numero AS Numero,
                           s.tipo AS Tipo,
                           s.empresa AS Empresa,
                           t.apellidos AS Conductor,
                           ROW_NUMBER() OVER (
                               PARTITION BY s.unidad
                               ORDER BY (s.status = '2') DESC, CAST(s.codservicio AS UNSIGNED) DESC
                           ) AS rn
                    FROM servicio s
                    LEFT JOIN taxi t ON s.codconductor = t.codtaxi
                    WHERE s.unidad IN @DeviceIds
                      AND s.status IN ('1', '2')
                ) x
                WHERE rn = 1";

            var servicios = await _doConnection.QueryAsync<ServicioActual>(sqlServicios, new { DeviceIds = deviceIds });
            return servicios.ToList();
        }
    }
}
