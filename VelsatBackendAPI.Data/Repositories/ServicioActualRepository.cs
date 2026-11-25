using Dapper;
using System;
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

            // Por placa, prioridad (entre iguales gana el codservicio más alto):
            //   0) en curso que empezó hoy
            //   1) programado de hoy que ya es hora o falta poco (hasta HorasAnticipacion h antes)
            //   2) en curso de ayer (últimas 24 h): cubre servicios que cruzan la medianoche
            //   3) programado lejano (hoy más tarde o futuro)
            // Así un en curso de ayer sin finalizar cede ante el programado que ya se acerca, pero un
            // servicio nocturno real no lo tapa un programado de la mañana.
            // Programado = status 1. Finalizado (3) no se devuelve: la unidad queda "sin servicio".
            // Ventanas para descartar servicios que quedaron sin cerrar (fecha es texto dd/MM/yyyy HH:mm):
            //  - En curso (2): solo si empezó en las últimas 24 h (cubre los que cruzan la medianoche).
            //  - Programado (1): solo de hoy 00:00 en adelante.
            // Las fechas se calculan en la app (TZ America/Lima) y no con NOW() para no depender de la zona de MySQL.
            var ahora = DateTime.Now;
            var desdeEnCurso = ahora.AddHours(-24);
            var desdeProgramado = ahora.Date;
            const int HorasAnticipacion = 2;
            var hastaCercano = ahora.AddHours(HorasAnticipacion);

            const string sqlServicios = @"
                SELECT DeviceId, Codservicio, Status, Numero, Tipo, Empresa, Conductor, Fecha
                FROM (
                    SELECT s.unidad AS DeviceId,
                           s.codservicio AS Codservicio,
                           s.status AS Status,
                           s.numero AS Numero,
                           s.tipo AS Tipo,
                           s.empresa AS Empresa,
                           t.apellidos AS Conductor,
                           s.fecha AS Fecha,
                           ROW_NUMBER() OVER (
                               PARTITION BY s.unidad
                               ORDER BY CASE
                                            WHEN s.status = '2'
                                                 AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') >= @DesdeProgramado THEN 0
                                            WHEN s.status = '1'
                                                 AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') >= @DesdeProgramado
                                                 AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') <= @HastaCercano THEN 1
                                            WHEN s.status = '2' THEN 2
                                            ELSE 3
                                        END,
                                        CAST(s.codservicio AS UNSIGNED) DESC
                           ) AS rn
                    FROM servicio s
                    LEFT JOIN taxi t ON s.codconductor = t.codtaxi
                    WHERE s.unidad IN @DeviceIds
                      AND (
                            (s.status = '2' AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') >= @DesdeEnCurso)
                         OR (s.status = '1' AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') >= @DesdeProgramado)
                      )
                ) x
                WHERE rn = 1";

            var servicios = await _doConnection.QueryAsync<ServicioActual>(sqlServicios, new
            {
                DeviceIds = deviceIds,
                DesdeEnCurso = desdeEnCurso,
                DesdeProgramado = desdeProgramado,
                HastaCercano = hastaCercano
            });
            return servicios.ToList();
        }
    }
}
