using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Talma;

namespace VelsatBackendAPI.Data.Repositories
{
    public class TalmaRepository : ITalmaRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;


        public TalmaRepository(IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        public async Task<InsertPedidoTalmaResponse> InsertPedido(IEnumerable<RegistroExcel> registros)
        {
            var sw = Stopwatch.StartNew();

            var response = new InsertPedidoTalmaResponse
            {
                Success = true,
                Errores = new List<ListaErroresTalma>()
            };

            if (registros == null || !registros.Any())
            {
                response.Success = false;
                response.Errores.Add(new ListaErroresTalma
                {
                    Id = 0,
                    Codlan = null,
                    Motivo = "No se recibieron registros para procesar"
                });
                return response;
            }

            var listaRegistros = registros.ToList();
            Console.WriteLine($"Iniciando procesamiento de {listaRegistros.Count} registros");

            var pedidosValidos = new List<PedidoTalma>();
            var fechaRegistro = DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                // OPTIMIZACIÓN: Obtener todos los datos de clientes en una sola consulta
                Console.WriteLine("Obteniendo datos de clientes en batch...");
                var codlans = listaRegistros
                    .Where(r => !string.IsNullOrWhiteSpace(r.Codlan))
                    .Select(r => r.Codlan)
                    .Distinct()
                    .ToList();

                var datosClientes = await ObtenerDatosClientesBatch(codlans);
                Console.WriteLine($"Datos de clientes obtenidos: {datosClientes.Count} de {codlans.Count} encontrados");

                // Validar y procesar cada registro
                foreach (var registro in listaRegistros)
                {
                    // Validar campos requeridos
                    var validacionResult = ValidarRegistro(registro);

                    if (!validacionResult.EsValido)
                    {
                        response.Errores.Add(new ListaErroresTalma
                        {
                            Id = registro.Id,
                            Codlan = registro.Codlan,
                            Motivo = validacionResult.Mensaje
                        });
                        continue;
                    }

                    // Buscar datos del cliente en el diccionario (O(1) en lugar de consulta a BD)
                    if (!datosClientes.TryGetValue(registro.Codlan, out var datosCliente))
                    {
                        response.Errores.Add(new ListaErroresTalma
                        {
                            Id = registro.Id,
                            Codlan = registro.Codlan,
                            Motivo = "Cliente no encontrado o inactivo en el sistema"
                        });
                        continue;
                    }

                    // Crear el objeto PedidoTalma
                    var pedidoTalma = new PedidoTalma
                    {
                        Codlan = registro.Codlan,
                        Codcliente = datosCliente.Codcliente,
                        Nombre = datosCliente.Apellidos,
                        Fecha = registro.Fecha.Trim(),
                        Hora = registro.Hora.Trim(),
                        Tipo = registro.Tipo.ToString(),
                        Fecreg = fechaRegistro,
                        Distancia = null,
                        Horaprog = null,
                        Orden = null,
                        Grupo = null,
                        Codconductor = null,
                        Codunidad = null,
                        Usuario = !string.IsNullOrWhiteSpace(registro.Usuario) ? registro.Usuario : "cgacela",
                        Empresa = "TALMA",
                        Eliminado = "0",
                        Cerrado = "0",
                        Destinocodigo = "4175",
                        Destinocodlugar = datosCliente.Codlugar,
                        Direccionalterna = null,
                        Codservicio = null
                    };

                    pedidosValidos.Add(pedidoTalma);
                }

                Console.WriteLine($"Registros válidos para insertar: {pedidosValidos.Count}");
                Console.WriteLine($"Registros con errores: {response.Errores.Count}");

                // Insertar pedidos válidos
                if (pedidosValidos.Any())
                {
                    try
                    {
                        // Para grandes volúmenes, insertar en lotes
                        if (pedidosValidos.Count > 100)
                        {
                            Console.WriteLine($"Insertando {pedidosValidos.Count} registros en lotes de 100");
                            await InsertarPedidosEnLotes(pedidosValidos, 100);
                        }
                        else
                        {
                            Console.WriteLine($"Insertando {pedidosValidos.Count} registros en un solo batch");
                            await InsertarPedidosEnDB(pedidosValidos);
                        }

                        if (response.Errores.Any())
                        {
                            response.Success = false; // Parcialmente exitoso
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al insertar pedidos en la base de datos");
                        response.Success = false;

                        // Agregar error para todos los pedidos que no se pudieron insertar
                        foreach (var pedido in pedidosValidos)
                        {
                            if (!response.Errores.Any(e => e.Codlan == pedido.Codlan))
                            {
                                response.Errores.Add(new ListaErroresTalma
                                {
                                    Id = listaRegistros.FirstOrDefault(r => r.Codlan == pedido.Codlan)?.Id ?? 0,
                                    Codlan = pedido.Codlan,
                                    Motivo = $"Error en base de datos: {ex.Message}"
                                });
                            }
                        }
                    }
                }
                else
                {
                    response.Success = false;
                    Console.WriteLine("No hay registros válidos para insertar");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error general en el procesamiento de pedidos");
                response.Success = false;
                response.Errores.Add(new ListaErroresTalma
                {
                    Id = 0,
                    Codlan = null,
                    Motivo = $"Error general: {ex.Message}"
                });
            }

            sw.Stop();
            Console.WriteLine($"Procesamiento completado en {sw.ElapsedMilliseconds}ms. Éxito: {response.Success}");

            return response;
        }

        // Método optimizado para obtener datos de múltiples clientes en una sola consulta
        private async Task<Dictionary<string, DatosCliente>> ObtenerDatosClientesBatch(IEnumerable<string> codlans)
        {
            var codlansList = codlans.Distinct().ToList();

            if (!codlansList.Any())
                return new Dictionary<string, DatosCliente>();

            string sql = @"
                SELECT c.codlan, c.codcliente, c.apellidos, l.codlugar 
                FROM cliente c
                INNER JOIN lugarcliente l ON c.codlugar = l.codcli
                WHERE c.estadocuenta = 'A' 
                  AND l.estado = 'A' 
                  AND c.codlan IN @Codlans";

            var parameters = new { Codlans = codlansList };

            try
            {
                var sw = Stopwatch.StartNew();
                var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);
                sw.Stop();

                Console.WriteLine($"Consulta de clientes batch completada en {sw.ElapsedMilliseconds}ms");

                var diccionario = new Dictionary<string, DatosCliente>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in results)
                {
                    var codlan = row.codlan.ToString();
                    if (!diccionario.ContainsKey(codlan))
                    {
                        diccionario[codlan] = new DatosCliente
                        {
                            Codcliente = row.codcliente.ToString(),
                            Apellidos = row.apellidos,
                            Codlugar = row.codlugar.ToString()
                        };
                    }
                }

                return diccionario;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener clientes en batch");
                return new Dictionary<string, DatosCliente>();
            }
        }

        // Método para insertar en la base de datos (batch pequeño)
        private async Task InsertarPedidosEnDB(List<PedidoTalma> pedidos)
        {
            if (pedidos == null || !pedidos.Any())
                return;

            var sql = @"
                INSERT INTO preplan_talma 
                (
                    codlan, codcliente, nombre, fecha, hora, tipo, fecreg, 
                    distancia, horaprog, orden, grupo, codconductor, codunidad, 
                    usuario, empresa, eliminado, cerrado, borrado, 
                    destinocodigo, destinocodlugar, direccionalterna, codservicio
                ) 
                VALUES 
                (
                    @Codlan, @Codcliente, @Nombre, @Fecha, @Hora, @Tipo, @Fecreg,
                    @Distancia, @Horaprog, @Orden, @Grupo, @Codconductor, @Codunidad,
                    @Usuario, @Empresa, @Eliminado, @Cerrado, @Borrado,
                    @Destinocodigo, @Destinocodlugar, @Direccionalterna, @Codservicio
                )";

            try
            {
                var sw = Stopwatch.StartNew();
                var filasAfectadas = await _doConnection.ExecuteAsync(sql, pedidos, transaction: _doTransaction);
                sw.Stop();

                Console.WriteLine($"Inserción de {filasAfectadas} registros completada en {sw.ElapsedMilliseconds}ms");

                if (filasAfectadas != pedidos.Count)
                {
                    throw new Exception($"Se esperaba insertar {pedidos.Count} registros, pero solo se insertaron {filasAfectadas}");
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                Console.WriteLine("Error de duplicado al insertar en preplan_talma");
                throw new Exception("Uno o más pedidos ya existen en el sistema", ex);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error de MySQL al insertar en preplan_talma");
                throw new Exception($"Error de base de datos MySQL: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al insertar pedidos en preplan_talma");
                throw;
            }
        }

        // Método para insertar en lotes grandes
        private async Task InsertarPedidosEnLotes(List<PedidoTalma> pedidos, int tamañoLote = 100)
        {
            var sql = @"
                INSERT INTO preplan_talma 
                (
                    codlan, codcliente, nombre, fecha, hora, tipo, fecreg, 
                    distancia, horaprog, orden, grupo, codconductor, codunidad, 
                    usuario, empresa, eliminado, cerrado, borrado, 
                    destinocodigo, destinocodlugar, direccionalterna, codservicio
                ) 
                VALUES 
                (
                    @Codlan, @Codcliente, @Nombre, @Fecha, @Hora, @Tipo, @Fecreg,
                    @Distancia, @Horaprog, @Orden, @Grupo, @Codconductor, @Codunidad,
                    @Usuario, @Empresa, @Eliminado, @Cerrado, @Borrado,
                    @Destinocodigo, @Destinocodlugar, @Direccionalterna, @Codservicio
                )";

            var totalInsertados = 0;
            var totalLotes = (int)Math.Ceiling((double)pedidos.Count / tamañoLote);

            Console.WriteLine($"Insertando {pedidos.Count} registros en {totalLotes} lotes de {tamañoLote}");

            for (int i = 0; i < pedidos.Count; i += tamañoLote)
            {
                var lote = pedidos.Skip(i).Take(tamañoLote).ToList();
                var numeroLote = (i / tamañoLote) + 1;

                try
                {
                    var sw = Stopwatch.StartNew();
                    var filasAfectadas = await _doConnection.ExecuteAsync(sql, lote, transaction: _doTransaction);
                    sw.Stop();

                    totalInsertados += filasAfectadas;
                    Console.WriteLine($"Lote {numeroLote}/{totalLotes} insertado: {filasAfectadas} registros en {sw.ElapsedMilliseconds}ms");
                }
                catch (MySqlException ex) when (ex.Number == 1062)
                {
                    Console.WriteLine($"Error de duplicado en lote {numeroLote}");
                    throw new Exception($"Duplicados encontrados en lote {numeroLote}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al insertar lote {numeroLote}");
                    throw new Exception($"Error al insertar lote {numeroLote} de {totalLotes}: {ex.Message}", ex);
                }
            }

            if (totalInsertados != pedidos.Count)
            {
                throw new Exception($"Inserción incompleta: {totalInsertados} de {pedidos.Count} registros insertados");
            }

            Console.WriteLine($"Inserción por lotes completada: {totalInsertados} registros en {totalLotes} lotes");
        }

        // Método auxiliar para validar registro
        private (bool EsValido, string Mensaje) ValidarRegistro(RegistroExcel registro)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(registro.Codlan))
                errores.Add("Código LAN es requerido");

            if (registro.Tipo == '\0' || string.IsNullOrWhiteSpace(registro.Tipo.ToString()))
                errores.Add("Tipo es requerido");

            if (string.IsNullOrWhiteSpace(registro.Fecha))
                errores.Add("Fecha es requerida");
            else if (!ValidarFormatoFecha(registro.Fecha))
                errores.Add("Formato de fecha inválido (debe ser dd/MM/yyyy)");

            if (string.IsNullOrWhiteSpace(registro.Hora))
                errores.Add("Hora es requerida");
            else if (!ValidarFormatoHora(registro.Hora))
                errores.Add("Formato de hora inválido (debe ser HH:mm)");

            return errores.Any()
                ? (false, string.Join(", ", errores))
                : (true, string.Empty);
        }

        // Método para validar formato de fecha dd/MM/yyyy
        private bool ValidarFormatoFecha(string fecha)
        {
            return DateTime.TryParseExact(fecha, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _);
        }

        // Método para validar formato de hora HH:mm
        private bool ValidarFormatoHora(string hora)
        {
            if (string.IsNullOrWhiteSpace(hora) || hora.Length != 5)
                return false;

            return DateTime.TryParseExact(hora, "HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _);
        }

        public async Task<IEnumerable<PreplanTalmaResponse>> GetPreplanTalma(string tipo, string fecha, string hora)
        {
            string sql = @"SELECT codigo, nombre, fecha, hora, tipo, horaprog, orden, grupo, 
                  cerrado, eliminado, codconductor, codunidad, empresa, destinocodigo, destinocodlugar 
           FROM preplan_talma 
           WHERE cerrado = '0' AND eliminado = '0' AND tipo = @Tipo AND fecha = @Fecha AND hora = @Hora";
            var parameters = new
            {
                Tipo = tipo,
                Fecha = fecha,
                Hora = hora
            };
            var results = await _doConnection.QueryAsync<dynamic>(sql, parameters, transaction: _doTransaction);
            if (!results.Any())
                return new List<PreplanTalmaResponse>();

            // Objeto predefinido para el aeropuerto
            var aeropuertoInfo = new LugarInfo
            {
                Direccion = "Destino Nuevo Aeropuerto Internacional Jorge Chávez",
                Distrito = "Callao",
                Wy = "-12.034004553836395",
                Wx = "-77.11457919557617",
                Referencia = null
            };

            // Obtener todos los códigos de lugar únicos (excluyendo 4175 para destinocodigo)
            var todosLosCodigos = new HashSet<string>();
            foreach (var row in results)
            {
                // Solo agregar destinocodigo si es diferente de 4175
                if (!string.IsNullOrEmpty(row.destinocodigo?.ToString()) && row.destinocodigo.ToString() != "4175")
                    todosLosCodigos.Add(row.destinocodigo.ToString());

                if (!string.IsNullOrEmpty(row.destinocodlugar?.ToString()))
                    todosLosCodigos.Add(row.destinocodlugar.ToString());
            }

            // Una sola consulta para obtener todos los lugares
            var lugares = new Dictionary<string, LugarInfo>();

            // Agregar el aeropuerto al diccionario
            lugares["4175"] = aeropuertoInfo;

            if (todosLosCodigos.Any())
            {
                // Convertir los strings a int para la consulta
                var codigosInt = todosLosCodigos
                    .Select(c => int.TryParse(c, out var num) ? num : (int?)null)
                    .Where(n => n.HasValue)
                    .Select(n => n.Value)
                    .ToList();

                string sqlLugares = @"SELECT codlugar, direccion, distrito, wy, wx, referencia 
                     FROM lugarcliente 
                     WHERE estado = 'A' AND codlugar IN @Codigos";

                var lugaresResult = await _doConnection.QueryAsync<dynamic>(sqlLugares,
                    new { Codigos = codigosInt },
                    transaction: _doTransaction);

                foreach (var lugar in lugaresResult)
                {
                    lugares[lugar.codlugar.ToString()] = new LugarInfo
                    {
                        Direccion = lugar.direccion?.ToString(),
                        Distrito = lugar.distrito?.ToString(),
                        Wy = lugar.wy?.ToString(),
                        Wx = lugar.wx?.ToString(),
                        Referencia = lugar.referencia?.ToString()
                    };
                }
            }

            // Construir la respuesta
            var responseList = new List<PreplanTalmaResponse>();
            foreach (var row in results)
            {
                var response = new PreplanTalmaResponse
                {
                    Codigo = row.codigo.ToString(),
                    Nombre = row.nombre.ToString(),
                    Fecha = row.fecha.ToString(),
                    Hora = row.hora.ToString(),
                    Tipo = row.tipo.ToString(),
                    Horaprog = row.horaprog?.ToString(),
                    Orden = row.orden?.ToString(),
                    Grupo = row.grupo?.ToString(),
                    Codconductor = row.codconductor?.ToString(),
                    Codunidad = row.codunidad?.ToString(),
                    Empresa = row.empresa.ToString()
                };

                var destinoCodigo = row.destinocodigo?.ToString();
                var destinoCodLugar = row.destinocodlugar?.ToString();

                if (!string.IsNullOrEmpty(destinoCodigo) && lugares.ContainsKey(destinoCodigo))
                    response.Destino = lugares[destinoCodigo];
                if (!string.IsNullOrEmpty(destinoCodLugar) && lugares.ContainsKey(destinoCodLugar))
                    response.DireccionPasajero = lugares[destinoCodLugar];

                responseList.Add(response);
            }
            return responseList;
        }

        public async Task<bool> DeletePreplanTalma(int codigo)
        {
            string sql = @"UPDATE preplan_talma SET eliminado = '1' WHERE codigo = @Codigo";

            var result = await _doConnection.ExecuteAsync(sql, new { Codigo = codigo }, transaction: _doTransaction);
            return result > 0;
        }

        public async Task<IEnumerable<PreplanTalmaResponse>> GetPreplanTalmaEliminados(string tipo, string fecha, string hora)
        {
            string sql = @"SELECT codigo, nombre, fecha, hora, tipo, horaprog, orden, grupo, 
                  eliminado, codconductor, codunidad, empresa, destinocodigo, destinocodlugar 
           FROM preplan_talma 
           WHERE eliminado = '1' AND tipo = @Tipo AND fecha = @Fecha AND hora = @Hora";
            var parameters = new
            {
                Tipo = tipo,
                Fecha = fecha,
                Hora = hora
            };
            var results = await _doConnection.QueryAsync<dynamic>(sql, parameters, transaction: _doTransaction);
            if (!results.Any())
                return new List<PreplanTalmaResponse>();

            // Objeto predefinido para el aeropuerto
            var aeropuertoInfo = new LugarInfo
            {
                Direccion = "Destino Nuevo Aeropuerto Internacional Jorge Chávez",
                Distrito = "Callao",
                Wy = "-12.034004553836395",
                Wx = "-77.11457919557617",
                Referencia = null
            };

            // Obtener todos los códigos de lugar únicos (excluyendo 4175 para destinocodigo)
            var todosLosCodigos = new HashSet<string>();
            foreach (var row in results)
            {
                // Solo agregar destinocodigo si es diferente de 4175
                if (!string.IsNullOrEmpty(row.destinocodigo?.ToString()) && row.destinocodigo.ToString() != "4175")
                    todosLosCodigos.Add(row.destinocodigo.ToString());

                if (!string.IsNullOrEmpty(row.destinocodlugar?.ToString()))
                    todosLosCodigos.Add(row.destinocodlugar.ToString());
            }

            // Una sola consulta para obtener todos los lugares
            var lugares = new Dictionary<string, LugarInfo>();

            // Agregar el aeropuerto al diccionario
            lugares["4175"] = aeropuertoInfo;

            if (todosLosCodigos.Any())
            {
                // Convertir los strings a int para la consulta
                var codigosInt = todosLosCodigos
                    .Select(c => int.TryParse(c, out var num) ? num : (int?)null)
                    .Where(n => n.HasValue)
                    .Select(n => n.Value)
                    .ToList();

                string sqlLugares = @"SELECT codlugar, direccion, distrito, wy, wx, referencia 
                     FROM lugarcliente 
                     WHERE estado = 'A' AND codlugar IN @Codigos";

                var lugaresResult = await _doConnection.QueryAsync<dynamic>(sqlLugares,
                    new { Codigos = codigosInt },
                    transaction: _doTransaction);

                foreach (var lugar in lugaresResult)
                {
                    lugares[lugar.codlugar.ToString()] = new LugarInfo
                    {
                        Direccion = lugar.direccion?.ToString(),
                        Distrito = lugar.distrito?.ToString(),
                        Wy = lugar.wy?.ToString(),
                        Wx = lugar.wx?.ToString(),
                        Referencia = lugar.referencia?.ToString()
                    };
                }
            }

            // Construir la respuesta
            var responseList = new List<PreplanTalmaResponse>();
            foreach (var row in results)
            {
                var response = new PreplanTalmaResponse
                {
                    Codigo = row.codigo.ToString(),
                    Nombre = row.nombre.ToString(),
                    Fecha = row.fecha.ToString(),
                    Hora = row.hora.ToString(),
                    Tipo = row.tipo.ToString(),
                    Horaprog = row.horaprog?.ToString(),
                    Orden = row.orden?.ToString(),
                    Grupo = row.grupo?.ToString(),
                    Codconductor = row.codconductor?.ToString(),
                    Codunidad = row.codunidad?.ToString(),
                    Empresa = row.empresa.ToString()
                };

                var destinoCodigo = row.destinocodigo?.ToString();
                var destinoCodLugar = row.destinocodlugar?.ToString();

                if (!string.IsNullOrEmpty(destinoCodigo) && lugares.ContainsKey(destinoCodigo))
                    response.Destino = lugares[destinoCodigo];
                if (!string.IsNullOrEmpty(destinoCodLugar) && lugares.ContainsKey(destinoCodLugar))
                    response.DireccionPasajero = lugares[destinoCodLugar];

                responseList.Add(response);
            }
            return responseList;
        }

        public async Task<int> SavePreplanTalma(List<UpdatePreplanTalma> pedidos)
        {
            // Aplicar valor por defecto para Destinocodigo si es null
            foreach (var pedido in pedidos)
            {
                if (string.IsNullOrEmpty(pedido.Destinocodigo))
                {
                    pedido.Destinocodigo = "4175";
                }
            }

            string sql = @"UPDATE preplan_talma 
                   SET horaprog = @Horaprog,
                       orden = @Orden,
                       grupo = @Grupo,
                       codconductor = @Codconductor,
                       codunidad = @Codunidad,
                       destinocodigo = @Destinocodigo,
                       destinocodlugar = @Destinocodlugar
                   WHERE codigo = @Codigo";

            var result = await _doConnection.ExecuteAsync(sql, pedidos, transaction: _doTransaction);
            return result;
        }
    }
}
