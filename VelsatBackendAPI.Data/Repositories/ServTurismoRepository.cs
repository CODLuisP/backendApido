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
        public async Task<List<ServTurismo>> GetByFechas(DateTime fechaInicio, DateTime fechaFin)
        {
            string sql = @"SELECT idservicio, fechainicio, instrucciones, horainicio, indicaciones, horaretorno,
                                   bus, placa, brevete, piloto, celular, cobrevete, copiloto, cocelular, tipounidad,
                                   cliente, grupo, numpax, origen, destino, guiaturista, vuelocliente, observaciones,
                                   ejecutivo, cotizacion
                            FROM servturismo
                            WHERE fechainicio BETWEEN @FechaInicio AND @FechaFin
                            ORDER BY fechainicio, horainicio";

            var parameters = new { FechaInicio = fechaInicio.Date, FechaFin = fechaFin.Date };

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

        // Actualiza únicamente las propiedades de "campos" que vengan con valor (no nulas).
        // Las propiedades no enviadas (null) se ignoran y no modifican el registro.
        public async Task<int> Patch(int idservicio, ServTurismo campos)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Idservicio", idservicio);

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

            void AgregarValorSiPresente<T>(string columna, T? valor) where T : struct
            {
                if (!valor.HasValue)
                {
                    return;
                }

                setClauses.Add($"{columna} = @{columna}");
                parameters.Add(columna, valor.Value);
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
    }
}
