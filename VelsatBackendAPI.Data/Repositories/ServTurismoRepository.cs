using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Data.Repositories
{
    public class ServTurismoRepository : IServTurismoRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public ServTurismoRepository(IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        // fechainicio y horainicio ya son columnas DATE/TIME reales (ver servturismo_migracion_fecha_hora.sql),
        // así que el filtro compara directamente contra el índice idx_servturismo_fecha_hora, sin necesidad
        // de envolver la columna en ninguna función de conversión.
        // brevete opcional: permite a la app móvil pedir solo los servicios del conductor logueado
        // (se identifica por brevete, no hay FK al conductor en esta tabla).
        // Cuando se filtra por brevete (consulta de la app móvil) se excluyen los servicios cancelados
        // y los que están en Stand By: en ambos casos el conductor no tiene nada que hacer con ese
        // servicio y no debe verlo. Sin brevete (consultas de administración/despacho) sí se devuelven,
        // para poder gestionarlos (incluyendo reanudar un Stand By).
        public async Task<List<ServTurismo>> GetByFechas(DateTime fechaInicio, DateTime fechaFin, string? brevete = null)
        {
            string sql = $@"SELECT idservicio, fechainicio, instrucciones, horainicio, indicaciones, horaretorno,
                                   bus, placa, brevete, piloto, celular, cobrevete, copiloto, cocelular, tipounidad,
                                   cliente, grupo, numpax, origen, destino, guiaturista, vuelocliente, observaciones,
                                   ejecutivo, cotizacion, visto, confirmado, finalizado, reprogramado, cancelado, standby
                            FROM servturismo
                            WHERE fechainicio BETWEEN @FechaInicio AND @FechaFin
                            {(string.IsNullOrWhiteSpace(brevete)
                                ? ""
                                : "AND brevete = @Brevete AND (cancelado IS NULL OR cancelado <> 1) AND (standby IS NULL OR standby <> 1)")}
                            ORDER BY fechainicio, horainicio";

            var parameters = new { FechaInicio = fechaInicio.Date, FechaFin = fechaFin.Date, Brevete = brevete };

            var resultado = await _doConnection.QueryAsync<ServTurismo>(sql, parameters, transaction: _doTransaction);
            return resultado.ToList();
        }

        public async Task<int> Insert(ServTurismo servicio)
        {
            string sql = @"INSERT INTO servturismo
                            (fechainicio, instrucciones, horainicio, indicaciones, horaretorno, bus, placa, brevete,
                             piloto, celular, cobrevete, copiloto, cocelular, tipounidad, cliente, grupo, numpax,
                             origen, destino, guiaturista, vuelocliente, observaciones, ejecutivo, cotizacion)
                            VALUES
                            (@Fechainicio, @Instrucciones, @Horainicio, @Indicaciones, @Horaretorno, @Bus, @Placa, @Brevete,
                             @Piloto, @Celular, @Cobrevete, @Copiloto, @Cocelular, @Tipounidad, @Cliente, @Grupo, @Numpax,
                             @Origen, @Destino, @Guiaturista, @Vuelocliente, @Observaciones, @Ejecutivo, @Cotizacion);
                            SELECT LAST_INSERT_ID();";

            var idservicio = await _doConnection.QuerySingleAsync<int>(sql, servicio, transaction: _doTransaction);
            return idservicio;
        }

        // Por defecto (limpiarNulos = false) solo actualiza las propiedades de "campos" que vengan con
        // valor (no nulas); las no enviadas (null) se ignoran y no modifican el registro.
        // Con limpiarNulos = true, un valor null SÍ se aplica y borra el campo (lo deja NULL en la BD);
        // pensado para un formulario de edición que envía el objeto completo y donde un campo vacío
        // significa "el usuario lo borró".
        // Devuelve -1 (sentinela) cuando el servicio está Cancelado: un servicio cancelado ya no se
        // puede editar, así que el caller (controller) debe convertir eso en un 409 en vez de aplicar el patch.
        public async Task<int> Patch(int idservicio, ServTurismo campos, bool limpiarNulos = false)
        {
            var actual = await _doConnection.QueryFirstOrDefaultAsync(
                "SELECT fechainicio, cancelado FROM servturismo WHERE idservicio = @Idservicio",
                new { Idservicio = idservicio },
                transaction: _doTransaction);

            if (actual == null)
            {
                return 0;
            }

            if (Convert.ToByte(actual.cancelado ?? (byte)0) == 1)
            {
                return -1;
            }

            DateTime? fechaActual = (DateTime?)actual.fechainicio;

            var parameters = new DynamicParameters();
            parameters.Add("Idservicio", idservicio);

            var setClauses = new List<string>();

            void AgregarSiPresente(string columna, string? valor)
            {
                if (valor == null && !limpiarNulos)
                {
                    return;
                }

                setClauses.Add($"{columna} = @{columna}");
                // dbType explícito: si el valor es DBNull, Dapper no puede inferir el tipo del parámetro solo.
                parameters.Add(columna, (object?)valor ?? DBNull.Value, dbType: DbType.String);
            }

            void AgregarValorSiPresente<T>(string columna, T? valor) where T : struct
            {
                DbType? dbType = typeof(T) == typeof(DateTime)
                    ? DbType.Date
                    : typeof(T) == typeof(TimeSpan)
                        ? DbType.Time
                        : typeof(T) == typeof(byte)
                            ? DbType.Byte
                            : (DbType?)null;

                if (!valor.HasValue)
                {
                    if (!limpiarNulos)
                    {
                        return;
                    }

                    setClauses.Add($"{columna} = @{columna}");
                    parameters.Add(columna, DBNull.Value, dbType: dbType);
                    return;
                }

                setClauses.Add($"{columna} = @{columna}");
                parameters.Add(columna, valor.Value, dbType: dbType);
            }

            AgregarValorSiPresente("fechainicio", campos.Fechainicio);
            AgregarSiPresente("instrucciones", campos.Instrucciones);
            AgregarValorSiPresente("horainicio", campos.Horainicio);
            AgregarSiPresente("indicaciones", campos.Indicaciones);
            AgregarSiPresente("horaretorno", campos.Horaretorno);
            AgregarSiPresente("bus", campos.Bus);
            AgregarSiPresente("placa", campos.Placa);
            AgregarSiPresente("brevete", campos.Brevete);
            AgregarSiPresente("piloto", campos.Piloto);
            AgregarSiPresente("celular", campos.Celular);
            AgregarSiPresente("cobrevete", campos.Cobrevete);
            AgregarSiPresente("copiloto", campos.Copiloto);
            AgregarSiPresente("cocelular", campos.Cocelular);
            AgregarSiPresente("tipounidad", campos.Tipounidad);
            AgregarSiPresente("cliente", campos.Cliente);
            AgregarSiPresente("grupo", campos.Grupo);
            AgregarSiPresente("numpax", campos.Numpax);
            AgregarSiPresente("origen", campos.Origen);
            AgregarSiPresente("destino", campos.Destino);
            AgregarSiPresente("guiaturista", campos.Guiaturista);
            AgregarSiPresente("vuelocliente", campos.Vuelocliente);
            AgregarSiPresente("observaciones", campos.Observaciones);
            AgregarSiPresente("ejecutivo", campos.Ejecutivo);
            AgregarSiPresente("cotizacion", campos.Cotizacion);
            // Reprogramación: si el PATCH cambia fechainicio a un día distinto del que tenía el servicio,
            // se marca "reprogramado" y se limpian visto/confirmado/finalizado para que en la columna
            // Estado del conductor solo quede "Reprogramado" hasta que vuelva a ver/confirmar/finalizar
            // el servicio ya con la fecha nueva. Se decide antes de aplicar "campos.Visto/Confirmado/Finalizado"
            // para que la limpieza gane siempre y no queden dos SET del mismo campo en la consulta.
            bool esReprogramacion = campos.Fechainicio.HasValue && fechaActual.HasValue
                && fechaActual.Value.Date != campos.Fechainicio.Value.Date;

            if (esReprogramacion)
            {
                setClauses.Add("reprogramado = 1");
                setClauses.Add("visto = 0");
                setClauses.Add("confirmado = 0");
                setClauses.Add("finalizado = 0");
            }
            else
            {
                AgregarValorSiPresente("visto", campos.Visto);
                AgregarValorSiPresente("confirmado", campos.Confirmado);
                AgregarValorSiPresente("finalizado", campos.Finalizado);
            }

            if (!setClauses.Any())
            {
                return 0;
            }

            string sql = $"UPDATE servturismo SET {string.Join(", ", setClauses)} WHERE idservicio = @Idservicio";

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        // Inserta en lote usando sentencias INSERT multi-VALUES (por bloques) en vez de un INSERT por fila,
        // para evitar N round-trips a la base de datos cuando se cargan muchos registros (ej. importación de Excel).
        private const int TamanioLoteInsert = 500;

        public async Task<int> InsertBatch(IEnumerable<ServTurismo> servicios)
        {
            var lista = servicios?.Where(s => s != null).ToList() ?? new List<ServTurismo>();

            if (!lista.Any())
            {
                return 0;
            }

            int totalInsertados = 0;

            foreach (var lote in lista.Chunk(TamanioLoteInsert))
            {
                var parameters = new DynamicParameters();
                var valuesClauses = new List<string>(lote.Length);

                for (int i = 0; i < lote.Length; i++)
                {
                    var s = lote[i];

                    valuesClauses.Add(
                        $"(@Fechainicio{i}, @Instrucciones{i}, @Horainicio{i}, @Indicaciones{i}, @Horaretorno{i}, @Bus{i}, @Placa{i}, @Brevete{i}, " +
                        $"@Piloto{i}, @Celular{i}, @Cobrevete{i}, @Copiloto{i}, @Cocelular{i}, @Tipounidad{i}, @Cliente{i}, @Grupo{i}, @Numpax{i}, " +
                        $"@Origen{i}, @Destino{i}, @Guiaturista{i}, @Vuelocliente{i}, @Observaciones{i}, @Ejecutivo{i}, @Cotizacion{i})");

                    parameters.Add($"Fechainicio{i}", s.Fechainicio);
                    parameters.Add($"Instrucciones{i}", s.Instrucciones);
                    parameters.Add($"Horainicio{i}", s.Horainicio);
                    parameters.Add($"Indicaciones{i}", s.Indicaciones);
                    parameters.Add($"Horaretorno{i}", s.Horaretorno);
                    parameters.Add($"Bus{i}", s.Bus);
                    parameters.Add($"Placa{i}", s.Placa);
                    parameters.Add($"Brevete{i}", s.Brevete);
                    parameters.Add($"Piloto{i}", s.Piloto);
                    parameters.Add($"Celular{i}", s.Celular);
                    parameters.Add($"Cobrevete{i}", s.Cobrevete);
                    parameters.Add($"Copiloto{i}", s.Copiloto);
                    parameters.Add($"Cocelular{i}", s.Cocelular);
                    parameters.Add($"Tipounidad{i}", s.Tipounidad);
                    parameters.Add($"Cliente{i}", s.Cliente);
                    parameters.Add($"Grupo{i}", s.Grupo);
                    parameters.Add($"Numpax{i}", s.Numpax);
                    parameters.Add($"Origen{i}", s.Origen);
                    parameters.Add($"Destino{i}", s.Destino);
                    parameters.Add($"Guiaturista{i}", s.Guiaturista);
                    parameters.Add($"Vuelocliente{i}", s.Vuelocliente);
                    parameters.Add($"Observaciones{i}", s.Observaciones);
                    parameters.Add($"Ejecutivo{i}", s.Ejecutivo);
                    parameters.Add($"Cotizacion{i}", s.Cotizacion);
                }

                string sql = $@"INSERT INTO servturismo
                                (fechainicio, instrucciones, horainicio, indicaciones, horaretorno, bus, placa, brevete,
                                 piloto, celular, cobrevete, copiloto, cocelular, tipounidad, cliente, grupo, numpax,
                                 origen, destino, guiaturista, vuelocliente, observaciones, ejecutivo, cotizacion)
                                VALUES {string.Join(", ", valuesClauses)}";

                totalInsertados += await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
            }

            return totalInsertados;
        }

        public async Task<int> Delete(int idservicio)
        {
            string sql = "DELETE FROM servturismo WHERE idservicio = @Idservicio";

            return await _doConnection.ExecuteAsync(sql, new { Idservicio = idservicio }, transaction: _doTransaction);
        }

        // ===================== ACUSE DE RECIBO DEL CONDUCTOR =====================

        // El UPDATE va condicionado a que el campo no valga ya 1, porque MySQL informa 0 filas
        // afectadas cuando el valor no cambia; por eso la existencia del servicio se comprueba
        // aparte y no se deduce de las filas afectadas. Así, marcar dos veces el mismo servicio
        // (la app reintenta) sigue devolviendo éxito en lugar de un 404 falso.
        private async Task<bool> MarcarAcuse(int idservicio, string sqlUpdate)
        {
            int existe = await _doConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM servturismo WHERE idservicio = @Idservicio",
                new { Idservicio = idservicio },
                transaction: _doTransaction);

            if (existe == 0)
            {
                return false;
            }

            await _doConnection.ExecuteAsync(sqlUpdate, new { Idservicio = idservicio }, transaction: _doTransaction);
            return true;
        }

        // Ninguno de estos toca un servicio Cancelado: si lo cancelaron, el acuse del conductor
        // (que puede llegar tarde, ya en camino) ya no debe reabrirlo.
        public Task<bool> MarcarVisto(int idservicio) =>
            MarcarAcuse(idservicio,
                @"UPDATE servturismo
                  SET visto = 1
                  WHERE idservicio = @Idservicio
                    AND (cancelado IS NULL OR cancelado <> 1)
                    AND (visto IS NULL OR visto <> 1)");

        public Task<bool> MarcarConfirmado(int idservicio) =>
            MarcarAcuse(idservicio,
                @"UPDATE servturismo
                  SET confirmado = 1
                  WHERE idservicio = @Idservicio
                    AND (cancelado IS NULL OR cancelado <> 1)
                    AND (confirmado IS NULL OR confirmado <> 1)");

        public Task<bool> MarcarCancelado(int idservicio) =>
            MarcarAcuse(idservicio,
                "UPDATE servturismo SET cancelado = 1 WHERE idservicio = @Idservicio AND (cancelado IS NULL OR cancelado <> 1)");

        // Pausa reversible: no toca un servicio ya Cancelado (ese flujo es definitivo, no pasa por Stand By).
        public Task<bool> MarcarStandby(int idservicio) =>
            MarcarAcuse(idservicio,
                @"UPDATE servturismo
                  SET standby = 1
                  WHERE idservicio = @Idservicio
                    AND (cancelado IS NULL OR cancelado <> 1)
                    AND (standby IS NULL OR standby <> 1)");

        // Reanuda un servicio en Stand By: vuelve a estar visible para el conductor con el mismo
        // acuse que tenía antes de pausarse (no se tocan visto/confirmado/finalizado).
        public Task<bool> MarcarReanudar(int idservicio) =>
            MarcarAcuse(idservicio,
                @"UPDATE servturismo
                  SET standby = 0
                  WHERE idservicio = @Idservicio
                    AND standby = 1");

        // Estado final del ciclo del servicio, disparado por el deslizamiento a la derecha en la app
        // (con modal de confirmación porque es irreversible). No toca un servicio ya Cancelado ni
        // uno ya Finalizado (idempotente ante reintentos).
        public Task<bool> MarcarFinalizado(int idservicio) =>
            MarcarAcuse(idservicio,
                @"UPDATE servturismo
                  SET finalizado = 1
                  WHERE idservicio = @Idservicio
                    AND (cancelado IS NULL OR cancelado <> 1)
                    AND (finalizado IS NULL OR finalizado <> 1)");

        // ===================== TAXI (CONDUCTORES) CRUD =====================

        private const string ColumnasTaxi = @"codtaxi, apellidos, login, clave, sexo, codusuario, telefono, brevete, turismo";

        public async Task<List<ConductorTurismo>> GetTaxis(string codusuario)
        {
            string sql = $"SELECT {ColumnasTaxi} FROM taxi WHERE codusuario = @Codusuario AND turismo = '1' ORDER BY apellidos, nombres";

            var resultado = await _doConnection.QueryAsync<ConductorTurismo>(sql, new { Codusuario = codusuario }, transaction: _doTransaction);
            return resultado.ToList();
        }

        public async Task<ConductorTurismo?> GetTaxiById(int codtaxi)
        {
            string sql = $"SELECT {ColumnasTaxi} FROM taxi WHERE codtaxi = @Codtaxi";

            return await _doConnection.QueryFirstOrDefaultAsync<ConductorTurismo>(sql, new { Codtaxi = codtaxi }, transaction: _doTransaction);
        }

        public async Task<int> InsertTaxi(ConductorTurismo taxi)
        {
            if (string.IsNullOrWhiteSpace(taxi.Turismo))
            {
                taxi.Turismo = "1";
            }

            string sql = @"INSERT INTO taxi
                            (apellidos, login, clave, sexo, codusuario, telefono, brevete, turismo)
                            VALUES
                            (@Apellidos, @Login, @Clave, @Sexo, @Codusuario, @Telefono, @Brevete, @Turismo);
                            SELECT LAST_INSERT_ID();";

            var codtaxi = await _doConnection.QuerySingleAsync<int>(sql, taxi, transaction: _doTransaction);
            return codtaxi;
        }

        // Actualiza únicamente las propiedades de "campos" que vengan con valor (no nulas).
        // Las propiedades no enviadas (null) se ignoran y no modifican el registro.
        public async Task<int> PatchTaxi(int codtaxi, ConductorTurismo campos)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Codtaxi", codtaxi);

            var setClauses = new List<string>();

            void AgregarSiPresente(string columna, string? valor)
            {
                if (valor == null)
                {
                    return;
                }

                setClauses.Add($"{columna} = @{columna}");
                parameters.Add(columna, valor);
            }

            AgregarSiPresente("apellidos", campos.Apellidos);
            AgregarSiPresente("login", campos.Login);
            AgregarSiPresente("clave", campos.Clave);
            AgregarSiPresente("sexo", campos.Sexo);
            AgregarSiPresente("codusuario", campos.Codusuario);
            AgregarSiPresente("telefono", campos.Telefono);
            AgregarSiPresente("brevete", campos.Brevete);
            AgregarSiPresente("turismo", campos.Turismo);

            if (!setClauses.Any())
            {
                return 0;
            }

            string sql = $"UPDATE taxi SET {string.Join(", ", setClauses)} WHERE codtaxi = @Codtaxi";

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> DeleteTaxi(int codtaxi)
        {
            string sql = "DELETE FROM taxi WHERE codtaxi = @Codtaxi";

            return await _doConnection.ExecuteAsync(sql, new { Codtaxi = codtaxi }, transaction: _doTransaction);
        }
    }
}
