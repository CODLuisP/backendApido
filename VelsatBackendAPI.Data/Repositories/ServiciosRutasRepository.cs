using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.ServiciosRutas;

namespace VelsatBackendAPI.Data.Repositories
{
    // Vive bajo DOConnection. Las lecturas y escrituras de un mismo request comparten la
    // transacción del UnitOfWork, así crear/editar una ruta con sus paraderos es atómico.
    public class ServiciosRutasRepository : IServiciosRutasRepository
    {
        public const int ToleranciaPorDefectoMin = 5;

        private readonly IDbConnection _connection;
        private readonly IDbTransaction? _transaction;

        public ServiciosRutasRepository(IDbConnection connection, IDbTransaction? transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        private const string RutaColumns = @"
            id, account_id AS AccountID, codigo, nombre, grupo, descripcion, distancia_km AS DistanciaKm,
            activo, created_at AS CreatedAt, updated_at AS UpdatedAt";

        private const string ParaderoColumns = @"
            id, ruta_id AS RutaId, orden, tipo, nombre, descripcion, latitud, longitud,
            radio_metros AS RadioMetros, minutos_desde_anterior AS MinutosDesdeAnterior, activo";

        private const string ServicioColumns = @"
            s.id, s.account_id AS AccountID, s.device_id AS DeviceID, s.ruta_id AS RutaId,
            r.codigo AS RutaCodigo, r.nombre AS RutaNombre,
            s.fecha_salida AS FechaSalida, s.hora_salida AS HoraSalida,
            s.conductor_nombre AS ConductorNombre, s.conductor_brevete AS ConductorBrevete,
            s.conductor_celular AS ConductorCelular, s.estado,
            s.inicio_real AS InicioReal, s.fin_real AS FinReal, s.distancia_real_km AS DistanciaRealKm,
            s.observaciones, s.created_by AS CreatedBy, s.created_at AS CreatedAt, s.updated_at AS UpdatedAt";

        // =====================================================================
        // RUTAS
        // =====================================================================

        public async Task<IEnumerable<RutaServicio>> GetRutas(string accountID, bool incluirInactivas)
        {
            string sql = $@"
                SELECT {RutaColumns},
                       (SELECT COUNT(*) FROM ruta_paradero p WHERE p.ruta_id = ruta.id AND p.activo = 1) AS CantidadParaderos,
                       (SELECT COALESCE(SUM(p.minutos_desde_anterior), 0) FROM ruta_paradero p WHERE p.ruta_id = ruta.id AND p.activo = 1) AS DuracionMin
                FROM ruta
                WHERE account_id = @AccountID AND (@IncluirInactivas = 1 OR activo = 1)
                ORDER BY nombre";

            return await _connection.QueryAsync<RutaServicio>(
                sql, new { AccountID = accountID, IncluirInactivas = incluirInactivas ? 1 : 0 }, _transaction);
        }

        public async Task<RutaServicio?> GetRutaById(long id, string accountID)
        {
            string sql = $"SELECT {RutaColumns} FROM ruta WHERE id = @Id AND account_id = @AccountID";
            var ruta = await _connection.QueryFirstOrDefaultAsync<RutaServicio>(
                sql, new { Id = id, AccountID = accountID }, _transaction);
            if (ruta == null) return null;

            string sqlParaderos = $@"
                SELECT {ParaderoColumns} FROM ruta_paradero
                WHERE ruta_id = @RutaId AND activo = 1
                ORDER BY orden";
            var paraderos = (await _connection.QueryAsync<RutaParadero>(
                sqlParaderos, new { RutaId = id }, _transaction)).ToList();

            int acumulado = 0;
            foreach (var p in paraderos)
            {
                acumulado += p.MinutosDesdeAnterior;
                p.MinutosAcumulados = acumulado;
            }

            ruta.Paraderos = paraderos;
            ruta.CantidadParaderos = paraderos.Count;
            return ruta;
        }

        public async Task<bool> ExisteCodigoRuta(string accountID, string codigo, long? excluirId)
        {
            const string sql = @"
                SELECT COUNT(1) FROM ruta
                WHERE account_id = @AccountID AND codigo = @Codigo AND (@ExcluirId IS NULL OR id <> @ExcluirId)";
            int n = await _connection.ExecuteScalarAsync<int>(
                sql, new { AccountID = accountID, Codigo = codigo, ExcluirId = excluirId }, _transaction);
            return n > 0;
        }

        public async Task<long> InsertRuta(RutaRequest request)
        {
            const string sql = @"
                INSERT INTO ruta (account_id, codigo, nombre, grupo, descripcion, distancia_km)
                VALUES (@AccountID, @Codigo, @Nombre, @Grupo, @Descripcion, @DistanciaKm);
                SELECT LAST_INSERT_ID();";

            long rutaId = await _connection.QuerySingleAsync<long>(sql, new
            {
                request.AccountID,
                request.Codigo,
                request.Nombre,
                request.Grupo,
                request.Descripcion,
                request.DistanciaKm
            }, _transaction);

            for (int i = 0; i < request.Paraderos.Count; i++)
                await InsertParadero(rutaId, request.Paraderos[i], i, request.Paraderos.Count);

            return rutaId;
        }

        // Reemplaza los datos de la ruta y su lista de paraderos. Los paraderos que ya no vienen en la
        // lista se DESACTIVAN (no se borran) porque pueden tener historial en servicio_ruta_paradero.
        // El controlador ya validó que todos los Id recibidos pertenecen a esta ruta.
        public async Task UpdateRuta(long id, RutaRequest request)
        {
            const string sqlRuta = @"
                UPDATE ruta
                SET codigo = @Codigo, nombre = @Nombre, grupo = @Grupo, descripcion = @Descripcion, distancia_km = @DistanciaKm
                WHERE id = @Id AND account_id = @AccountID";

            await _connection.ExecuteAsync(sqlRuta, new
            {
                Id = id,
                request.AccountID,
                request.Codigo,
                request.Nombre,
                request.Grupo,
                request.Descripcion,
                request.DistanciaKm
            }, _transaction);

            // "Estaciona" todos los paraderos con orden negativo y tipo INTERMEDIO para que reordenar
            // o cambiar origen/destino no choque con los índices únicos (ruta_id+orden, origen, destino).
            await _connection.ExecuteAsync(@"
                UPDATE ruta_paradero
                SET tipo = 'INTERMEDIO', orden = -CAST(id AS SIGNED)
                WHERE ruta_id = @RutaId", new { RutaId = id }, _transaction);

            var idsConservados = new List<long>();
            int total = request.Paraderos.Count;

            for (int i = 0; i < total; i++)
            {
                var p = request.Paraderos[i];
                if (p.Id.HasValue && p.Id.Value > 0)
                {
                    await UpdateParadero(id, p, i, total);
                    idsConservados.Add(p.Id.Value);
                }
                else
                {
                    long nuevoId = await InsertParadero(id, p, i, total);
                    idsConservados.Add(nuevoId);
                }
            }

            await _connection.ExecuteAsync(@"
                UPDATE ruta_paradero SET activo = 0
                WHERE ruta_id = @RutaId AND id NOT IN @Ids",
                new { RutaId = id, Ids = idsConservados }, _transaction);
        }

        public Task SetRutaActivo(long id, string accountID, bool activo)
        {
            const string sql = "UPDATE ruta SET activo = @Activo WHERE id = @Id AND account_id = @AccountID";
            return _connection.ExecuteAsync(sql, new { Id = id, AccountID = accountID, Activo = activo ? 1 : 0 }, _transaction);
        }

        // posicion es base 0: la primera es ORIGEN (0 min), la última DESTINO, el resto INTERMEDIO.
        private static (int orden, string tipo, int minutos) Posicion(ParaderoRequest p, int posicion, int total)
        {
            string tipo = posicion == 0 ? "ORIGEN" : (posicion == total - 1 ? "DESTINO" : "INTERMEDIO");
            int minutos = posicion == 0 ? 0 : p.MinutosDesdeAnterior;
            return (posicion + 1, tipo, minutos);
        }

        private async Task<long> InsertParadero(long rutaId, ParaderoRequest p, int posicion, int total)
        {
            const string sql = @"
                INSERT INTO ruta_paradero
                    (ruta_id, orden, tipo, nombre, descripcion, latitud, longitud, radio_metros, minutos_desde_anterior, activo)
                VALUES
                    (@RutaId, @Orden, @Tipo, @Nombre, @Descripcion, @Latitud, @Longitud, @RadioMetros, @Minutos, 1);
                SELECT LAST_INSERT_ID();";

            var (orden, tipo, minutos) = Posicion(p, posicion, total);
            return await _connection.QuerySingleAsync<long>(sql, new
            {
                RutaId = rutaId,
                Orden = orden,
                Tipo = tipo,
                p.Nombre,
                p.Descripcion,
                p.Latitud,
                p.Longitud,
                p.RadioMetros,
                Minutos = minutos
            }, _transaction);
        }

        private Task UpdateParadero(long rutaId, ParaderoRequest p, int posicion, int total)
        {
            const string sql = @"
                UPDATE ruta_paradero
                SET orden = @Orden, tipo = @Tipo, nombre = @Nombre, descripcion = @Descripcion,
                    latitud = @Latitud, longitud = @Longitud, radio_metros = @RadioMetros,
                    minutos_desde_anterior = @Minutos, activo = 1
                WHERE id = @Id AND ruta_id = @RutaId";

            var (orden, tipo, minutos) = Posicion(p, posicion, total);
            return _connection.ExecuteAsync(sql, new
            {
                Id = p.Id,
                RutaId = rutaId,
                Orden = orden,
                Tipo = tipo,
                p.Nombre,
                p.Descripcion,
                p.Latitud,
                p.Longitud,
                p.RadioMetros,
                Minutos = minutos
            }, _transaction);
        }

        // =====================================================================
        // SERVICIOS
        // =====================================================================

        public async Task<IEnumerable<ServicioRuta>> GetServicios(
            string accountID, DateTime? desde, DateTime? hasta, string? estado, string? deviceID)
        {
            string sql = $@"
                SELECT {ServicioColumns},
                       (SELECT COUNT(*) FROM servicio_ruta_paradero sp
                         WHERE sp.servicio_id = s.id AND sp.hora_llegada_real IS NOT NULL) AS ParaderosCompletados,
                       (SELECT COUNT(*) FROM servicio_ruta_paradero sp
                         WHERE sp.servicio_id = s.id) AS TotalParaderos
                FROM servicio_ruta s
                JOIN ruta r ON r.id = s.ruta_id
                WHERE s.account_id = @AccountID
                  AND (@Desde IS NULL OR s.fecha_salida >= @Desde)
                  AND (@Hasta IS NULL OR s.fecha_salida <= @Hasta)
                  AND (@Estado IS NULL OR s.estado = @Estado)
                  AND (@DeviceID IS NULL OR s.device_id = @DeviceID)
                ORDER BY s.fecha_salida DESC, s.hora_salida DESC";

            return await _connection.QueryAsync<ServicioRuta>(
                sql, new { AccountID = accountID, Desde = desde, Hasta = hasta, Estado = estado, DeviceID = deviceID }, _transaction);
        }

        public async Task<ServicioRuta?> GetServicioById(long id, string accountID)
        {
            string sql = $@"
                SELECT {ServicioColumns}
                FROM servicio_ruta s
                JOIN ruta r ON r.id = s.ruta_id
                WHERE s.id = @Id AND s.account_id = @AccountID";

            var servicio = await _connection.QueryFirstOrDefaultAsync<ServicioRuta>(
                sql, new { Id = id, AccountID = accountID }, _transaction);
            if (servicio == null) return null;

            const string sqlSeguimiento = @"
                SELECT id, servicio_id AS ServicioId, ruta_paradero_id AS RutaParaderoId, orden, nombre,
                       hora_esperada AS HoraEsperada, hora_llegada_real AS HoraLlegadaReal,
                       hora_salida_real AS HoraSalidaReal, diferencia_min AS DiferenciaMin, estado
                FROM servicio_ruta_paradero
                WHERE servicio_id = @Id
                ORDER BY orden";

            servicio.Paraderos = (await _connection.QueryAsync<ServicioRutaParadero>(
                sqlSeguimiento, new { Id = id }, _transaction)).ToList();
            return servicio;
        }

        // Devuelve 0 si la ruta no existe, es de otra cuenta o está inactiva.
        public Task<int> ContarParaderosDeRutaActiva(long rutaId, string accountID)
        {
            const string sql = @"
                SELECT COUNT(p.id)
                FROM ruta r
                JOIN ruta_paradero p ON p.ruta_id = r.id AND p.activo = 1
                WHERE r.id = @RutaId AND r.account_id = @AccountID AND r.activo = 1";
            return _connection.ExecuteScalarAsync<int>(sql, new { RutaId = rutaId, AccountID = accountID }, _transaction);
        }

        public async Task<long> InsertServicio(ServicioRuta servicio)
        {
            const string sql = @"
                INSERT INTO servicio_ruta
                    (account_id, device_id, ruta_id, fecha_salida, hora_salida,
                     conductor_nombre, conductor_brevete, conductor_celular, estado, observaciones, created_by)
                VALUES
                    (@AccountID, @DeviceID, @RutaId, @FechaSalida, @HoraSalida,
                     @ConductorNombre, @ConductorBrevete, @ConductorCelular, 'PROGRAMADO', @Observaciones, @CreatedBy);
                SELECT LAST_INSERT_ID();";

            long id = await _connection.QuerySingleAsync<long>(sql, new
            {
                servicio.AccountID,
                servicio.DeviceID,
                servicio.RutaId,
                servicio.FechaSalida,
                servicio.HoraSalida,
                servicio.ConductorNombre,
                servicio.ConductorBrevete,
                servicio.ConductorCelular,
                servicio.Observaciones,
                servicio.CreatedBy
            }, _transaction);

            await GenerarSeguimiento(id);
            return id;
        }

        // Solo para servicios PROGRAMADO (el controlador lo valida). Como puede cambiar la ruta, la
        // fecha o la hora, se regenera el seguimiento: aún no hay llegadas reales que conservar.
        public async Task UpdateServicio(ServicioRuta servicio)
        {
            const string sql = @"
                UPDATE servicio_ruta
                SET device_id = @DeviceID, ruta_id = @RutaId, fecha_salida = @FechaSalida, hora_salida = @HoraSalida,
                    conductor_nombre = @ConductorNombre, conductor_brevete = @ConductorBrevete,
                    conductor_celular = @ConductorCelular, observaciones = @Observaciones
                WHERE id = @Id AND account_id = @AccountID AND estado = 'PROGRAMADO'";

            await _connection.ExecuteAsync(sql, new
            {
                servicio.Id,
                servicio.AccountID,
                servicio.DeviceID,
                servicio.RutaId,
                servicio.FechaSalida,
                servicio.HoraSalida,
                servicio.ConductorNombre,
                servicio.ConductorBrevete,
                servicio.ConductorCelular,
                servicio.Observaciones
            }, _transaction);

            await _connection.ExecuteAsync(
                "DELETE FROM servicio_ruta_paradero WHERE servicio_id = @Id", new { servicio.Id }, _transaction);
            await GenerarSeguimiento(servicio.Id);
        }

        // Una fila de seguimiento por paradero activo: hora esperada = salida + minutos acumulados.
        private Task GenerarSeguimiento(long servicioId)
        {
            const string sql = @"
                INSERT INTO servicio_ruta_paradero (servicio_id, ruta_paradero_id, orden, nombre, hora_esperada)
                SELECT s.id, p.id, p.orden, p.nombre,
                       DATE_ADD(TIMESTAMP(s.fecha_salida, s.hora_salida),
                                INTERVAL SUM(p.minutos_desde_anterior)
                                         OVER (PARTITION BY p.ruta_id ORDER BY p.orden) MINUTE)
                FROM servicio_ruta s
                JOIN ruta_paradero p ON p.ruta_id = s.ruta_id AND p.activo = 1
                WHERE s.id = @ServicioId";
            return _connection.ExecuteAsync(sql, new { ServicioId = servicioId }, _transaction);
        }

        // Cambia fecha y hora de salida y corre las horas esperadas de los paraderos que aún no tienen
        // llegada (PENDIENTE) la misma cantidad de minutos. Las llegadas ya registradas no se tocan.
        // Devuelve 0 si el estado actual ya no era el esperado (p. ej. lo cambió la app móvil).
        public async Task<int> ReprogramarServicio(long id, string accountID, string estadoActual,
            DateTime nuevaSalida, int desplazamientoMin, string? motivo)
        {
            const string sql = @"
                UPDATE servicio_ruta
                SET fecha_salida = @Fecha, hora_salida = @Hora,
                    observaciones = CASE WHEN @Motivo IS NULL THEN observaciones
                                         ELSE CONCAT_WS(' | ', observaciones, CONCAT('Reprogramado: ', @Motivo)) END
                WHERE id = @Id AND account_id = @AccountID AND estado = @Actual";

            int filas = await _connection.ExecuteAsync(sql, new
            {
                Id = id,
                AccountID = accountID,
                Actual = estadoActual,
                Fecha = nuevaSalida.Date,
                Hora = nuevaSalida.TimeOfDay,
                Motivo = motivo
            }, _transaction);
            if (filas == 0) return 0;

            await _connection.ExecuteAsync(@"
                UPDATE servicio_ruta_paradero
                SET hora_esperada = DATE_ADD(hora_esperada, INTERVAL @Delta MINUTE)
                WHERE servicio_id = @Id AND hora_llegada_real IS NULL AND estado = 'PENDIENTE'",
                new { Id = id, Delta = desplazamientoMin }, _transaction);

            return filas;
        }

        // Devuelve las filas afectadas: 0 si el estado actual ya no era el esperado.
        public Task<int> CambiarEstadoServicio(long id, string accountID, string estadoActual, string nuevoEstado,
            DateTime ahoraUtc, decimal? distanciaRealKm, string? observaciones)
        {
            const string sql = @"
                UPDATE servicio_ruta
                SET estado = @Nuevo,
                    inicio_real = CASE WHEN @Nuevo = 'EN_CURSO' THEN @Ahora ELSE inicio_real END,
                    fin_real = CASE WHEN @Nuevo = 'FINALIZADO' THEN @Ahora ELSE fin_real END,
                    distancia_real_km = COALESCE(@Distancia, distancia_real_km),
                    observaciones = COALESCE(@Observaciones, observaciones)
                WHERE id = @Id AND account_id = @AccountID AND estado = @Actual";

            return _connection.ExecuteAsync(sql, new
            {
                Id = id,
                AccountID = accountID,
                Actual = estadoActual,
                Nuevo = nuevoEstado,
                Ahora = ahoraUtc,
                Distancia = distanciaRealKm,
                Observaciones = observaciones
            }, _transaction);
        }

        // =====================================================================
        // SEGUIMIENTO POR PARADERO
        // =====================================================================

        // Devuelve 0 si el paradero no existe en el servicio o ya tenía llegada registrada.
        public Task<int> RegistrarLlegada(long servicioId, int orden, DateTime horaLlegada, string estado)
        {
            const string sql = @"
                UPDATE servicio_ruta_paradero
                SET hora_llegada_real = @Hora, estado = @Estado
                WHERE servicio_id = @ServicioId AND orden = @Orden AND hora_llegada_real IS NULL";
            return _connection.ExecuteAsync(sql,
                new { ServicioId = servicioId, Orden = orden, Hora = horaLlegada, Estado = estado }, _transaction);
        }

        // Los paraderos anteriores que quedaron sin llegada pasan a OMITIDO.
        public Task<int> MarcarOmitidosAnteriores(long servicioId, int orden)
        {
            const string sql = @"
                UPDATE servicio_ruta_paradero
                SET estado = 'OMITIDO'
                WHERE servicio_id = @ServicioId AND orden < @Orden
                  AND hora_llegada_real IS NULL AND estado = 'PENDIENTE'";
            return _connection.ExecuteAsync(sql, new { ServicioId = servicioId, Orden = orden }, _transaction);
        }

        // Solo si ya hubo llegada y aún no se registró la salida.
        public Task<int> RegistrarSalida(long servicioId, int orden, DateTime horaSalida)
        {
            const string sql = @"
                UPDATE servicio_ruta_paradero
                SET hora_salida_real = @Hora
                WHERE servicio_id = @ServicioId AND orden = @Orden
                  AND hora_llegada_real IS NOT NULL AND hora_salida_real IS NULL";
            return _connection.ExecuteAsync(sql, new { ServicioId = servicioId, Orden = orden, Hora = horaSalida }, _transaction);
        }

        // =====================================================================
        // CONFIGURACIÓN
        // =====================================================================

        public async Task<int> GetTolerancia(string accountID)
        {
            const string sql = "SELECT tolerancia_min FROM servicio_ruta_config WHERE account_id = @AccountID";
            int? valor = await _connection.QueryFirstOrDefaultAsync<int?>(sql, new { AccountID = accountID }, _transaction);
            return valor ?? ToleranciaPorDefectoMin;
        }

        public Task SetTolerancia(string accountID, int toleranciaMin)
        {
            const string sql = @"
                INSERT INTO servicio_ruta_config (account_id, tolerancia_min)
                VALUES (@AccountID, @Tolerancia)
                ON DUPLICATE KEY UPDATE tolerancia_min = VALUES(tolerancia_min)";
            return _connection.ExecuteAsync(sql, new { AccountID = accountID, Tolerancia = toleranciaMin }, _transaction);
        }
    }
}
