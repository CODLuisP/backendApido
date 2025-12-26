using Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.MovilProgramacion;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Data.Repositories
{
    public class PreplanRepository : IPreplanRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction; IDbConnection _doConnection; IDbTransaction _doTransaction;

        public PreplanRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction, IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        private string SetEmpresa(string empresa)
        {
            return empresa switch
            {
                "Total KLM" => "KLM",
                "Total DELTA" => "DELTA",
                "Total Quality Products" => "Quality Products",
                "Total REP" => "REP",
                "Total INDECOPI" => "INDECOPI",
                "Total AMERICAN" => "AMERICAN",
                "PLUSPETROL" => "PLUSPETROL",
                "PROSEGUR" => "PROSEGUR",
                "TALMA" => "TALMA",
                "Total DHL" => "DHL",
                "OI PERU" => "OI PERU",
                "CHINALCO" => "CHINALCO",
                "METSO" => "METSO",
                "MOVILBUS" => "MOVILBUS",
                "OI LURIN" => "OI LURIN",
                "METSO SSGG" => "METSO SSGG",
                "AMERICAN TIERRA" => "AMERICAN TIERRA",
                "Total LCP" => "LCP",
                "Total COPA AIR" => "COPA AIR",
                "Tierra LATAM" => "LATAM",
                _ => "AVIANCA"
            };
        }

        public async Task<List<Pedido>> GetPedidos(string dato, string empresas, string usuario)
        {
            string sql = dato switch
            {
                "1" => @"select p.codigo, p.codcliente, p.nombre, p.rol, p.fecha, p.area, p.horaprog, p.orden, p.numero, p.usuario, p.codtarifa, p.codconductor, p.codunidad, p.tipo, p.empresa, p.duracion, STR_TO_DATE(p.fecha,'%d/%m/%Y %H:%i')as formato, p.eliminado,p.direccionalterna, p.destinocodigo, p.fecreg, p.codservicio, p.destinocodlugar from preplan p where cerrado='0' and borrado='0' and usuario=@Usuario AND empresa=@Empresa order by empresa, eliminado, tipo, formato, numero, orden*1, distancia",

                "2" => @"select p.codigo, p.codcliente, p.nombre, p.rol, p.fecha, p.area, p.horaprog, p.lastorden, p.lastnumero, p.usuario, p.codtarifa, p.codconductor, p.codunidad, p.tipo, p.empresa, p.duracion, STR_TO_DATE(p.fecha,'%d/%m/%Y %H:%i')as formato, p.eliminado, p.direccionalterna, p.destinocodigo, p.fecreg, p.codservicio, p.destinocodlugar from preplan p where cerrado='0' and borrado='0' and usuario=@Usuario AND empresa=@Empresa order by empresa, eliminado, tipo, formato, lastnumero, lastorden*1, distancia",

                _ => throw new ArgumentException("El valor de 'dato' no es válido.", nameof(dato))
            };

            var parameters = new { Empresa = empresas, Usuario = usuario };

            var pedidos = (await _doConnection.QueryAsync<Pedido>(sql, parameters, transaction: _doTransaction)).ToList();

            int cont = 1;

            foreach (var pedido in pedidos)
            {
                Console.WriteLine($"Fila devuelta: {JsonConvert.SerializeObject(pedido)}");

                pedido.Id = cont++;
                pedido.Servicio ??= new Servicio();
                pedido.Servicio.Conductor ??= new Usuario();
                pedido.Servicio.Unidad ??= new Unidad();

                // Enriquecimiento: Detalle destino
                if (!string.IsNullOrEmpty(pedido.Destinocodigo))
                {
                    var destino = await GetDestino(pedido.Destinocodigo);
                    pedido.Nomdestino = destino?.Nomdestino ?? "Destino no encontrado";
                    Console.WriteLine($"Destino actualizado: {pedido.Nomdestino}");
                }
                else
                {
                    var destinoDefault = await GetDestino("4175");
                    pedido.Nomdestino = destinoDefault?.Nomdestino ?? "Destino predeterminado no disponible";
                    Console.WriteLine($"Destino predeterminado establecido: {pedido.Nomdestino}");
                }

                // Enriquecimiento: Detalle del conductor y unidad
                var conductorActualizado = await GetDetalleConductor(pedido.Codconductor);
                if (conductorActualizado != null)
                {
                    pedido.Servicio.Conductor = conductorActualizado;
                    pedido.Servicio.Unidad.Codunidad = pedido.Codunidad;
                    Console.WriteLine($"Conductor actualizado: {conductorActualizado.Nombre} {conductorActualizado.Apepate}");
                }
                else
                {
                    Console.WriteLine($"No se encontró el conductor con código: {pedido.Codconductor}");
                }

                // Enriquecimiento: Dirección pasajero
                pedido.Lugar = string.IsNullOrEmpty(pedido.Destinocodlugar)
                    ? await GetLugarActualCliente(pedido.Codcliente)
                    : await GetLugarDetalle(pedido.Destinocodlugar);

                if (pedido.Lugar == null)
                {
                    Console.WriteLine("Lugar no encontrado para el cliente o código de lugar.");
                }

            }

            return pedidos;
        }

        private async Task<LugarCliente> GetLugarDetalle(string codlugar)
        {
            string sql = "select * from lugarcliente where codlugar = @Codlugar";

            var parameters = new
            {
                Codlugar = codlugar
            };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!results.Any())
            {
                return null;
            }

            var row = results.First();

            var lugarCliente = new LugarCliente
            {
                Codlugar = row.codlugar,
                Codcli = row.codcli,
                Direccion = row.direccion,
                Distrito = row.distrito,
                Wy = row.wy,
                Wx = row.wx,
                Estado = 'A',
                Referencia = row.referencia,
                Zona = row.zona
            };

            return lugarCliente;
        }

        private async Task<LugarCliente> GetLugarActualCliente(string codcli)
        {
            string sql = "SELECT * FROM lugarcliente WHERE estado = 'A' and codcli = @Codcli";

            var parameters = new
            {
                Codcli = codcli
            };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!results.Any())
            {
                return null;
            }

            var row = results.First();

            var lugarCliente = new LugarCliente
            {
                Codlugar = row.codlugar,
                Codcli = codcli,
                Direccion = row.direccion,
                Distrito = row.distrito,
                Wy = row.wy,
                Wx = row.wx,
                Estado = 'A',
                Referencia = row.referencia,
                Zona = row.zona
            };

            return lugarCliente;
        }

        private async Task<Usuario> GetDetalleConductor(string codtaxi)
        {
            if (!int.TryParse(codtaxi, out int codtaxiInt))
            {
                return null; // Retornamos null si la conversión falla
            }

            string sql = "SELECT codtaxi, nombres, apellidos, login, clave, telefono, servicioactual FROM taxi WHERE estado = 'A' and codtaxi = @Codtaxi";

            var parameters = new
            {
                Codtaxi = codtaxiInt
            };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!results.Any())
            {
                return null;
            }

            var row = results.First();

            var conductor = new Usuario
            {
                Codigo = row.codtaxi.ToString(),
                Nombre = row.nombres,
                Apepate = row.apellidos,
                Login = row.login,
                Clave = row.clave,
                Telefono = row.telefono,
                Servicioactual = new Servicio
                {
                    Codservicio = row.servicioactual
                }
            };

            return conductor;
        }

        private async Task<Pedido> GetDestino(string codlan)
        {
            if (!int.TryParse(codlan, out int codlanInt))
            {
                return null;
            }

            string sql = "SELECT apellidos FROM cliente WHERE codcliente = @Codlan";

            var parameters = new
            {
                Codlan = codlanInt,
            };

            var result = await _doConnection.QueryFirstOrDefaultAsync<string>(sql, parameters, transaction: _doTransaction);

            if (result == null)
            {
                return null;
            }

            return new Pedido
            {
                Nomdestino = result
            };
        }


        public async Task<InsertPedidoResponse> InsertPedido(IEnumerable<ExcelAvianca> excel, string fecact, string tipo, string usuario)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Iniciando InsertPedido - Usuario: {usuario}, Tipo: {tipo}, FecAct: {fecact}");
                Console.WriteLine($"[DEBUG] Cantidad de elementos Excel: {excel?.Count() ?? 0}");

                if (excel == null || !excel.Any())
                    throw new ArgumentException("El arreglo Excel está vacío");

                string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");
                Console.WriteLine($"[DEBUG] Fecha actual: {fechaActual}");

                var listaErrores = new List<ListaErrores>();
                var preturno = new List<PreTurno>();
                var pedido = new List<Pedido>();
                int contErrores = 0;
                string archivo = SetEmpresa(tipo);

                Console.WriteLine($"[DEBUG] Archivo empresa: {archivo}");

                string fecactFormat = DateTime.ParseExact(fecact, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
                Console.WriteLine($"[DEBUG] Fecha formateada: {fecactFormat}");

                foreach (var item in excel)
                {
                    Console.WriteLine($"[DEBUG] Procesando item - CodigoOracle: {item.CodigoOracle}, Nombre: {item.Nombre}, Rol: {item.Rol}");

                    var consultaCliente = @"SELECT apellidos FROM cliente WHERE codlugar = @CodLugar";
                    var parametrosCliente = new { CodLugar = item.CodigoOracle };

                    var nombreCliente = await _doConnection.QueryFirstOrDefaultAsync<string>(consultaCliente, parametrosCliente, transaction: _doTransaction);
                    Console.WriteLine($"[DEBUG] Nombre cliente obtenido: {nombreCliente ?? "NULL"}");

                    if (string.IsNullOrWhiteSpace(nombreCliente))
                    {
                        Console.WriteLine($"[DEBUG] ERROR: No se encontró cliente para código: {item.CodigoOracle}");
                        listaErrores.Add(new ListaErrores
                        {
                            Item = ++contErrores,
                            CodigoOracle = item.CodigoOracle,
                            Nombre = item.Nombre,
                            Subarea = item.Subarea,
                            Rol = item.Rol,
                            Motivo = "No se encontró el nombre.",
                            Archivo = archivo
                        });
                        continue;
                    }

                    item.Nombre = nombreCliente;

                    if (string.IsNullOrWhiteSpace(item.CodigoOracle))
                    {
                        Console.WriteLine($"[DEBUG] ERROR: CodigoOracle vacío");
                        listaErrores.Add(new ListaErrores
                        {
                            Item = ++contErrores,
                            Motivo = "Falta CodigoOracle.",
                            Archivo = archivo
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(item.Nombre))
                    {
                        Console.WriteLine($"[DEBUG] ERROR: Nombre vacío para código: {item.CodigoOracle}");
                        listaErrores.Add(new ListaErrores
                        {
                            Item = ++contErrores,
                            CodigoOracle = item.CodigoOracle,
                            Nombre = item.Nombre,
                            Subarea = item.Subarea,
                            Rol = item.Rol,
                            Motivo = "La columna Nombre está vacía.",
                            Archivo = archivo
                        });
                        continue;
                    }

                    var turnoList = new List<TurnoAvianca>();

                    if (!string.IsNullOrWhiteSpace(item.Rol))
                    {
                        item.Empresa = SetEmpresa(tipo);
                        Console.WriteLine($"[DEBUG] Empresa asignada: {item.Empresa}");

                        var parametros = new
                        {
                            Subarea = item.Subarea,
                            CodRol = item.Rol,
                            Empresa = item.Empresa,
                            Usuario = usuario
                        };

                        Console.WriteLine($"[DEBUG] Buscando turnos - Subarea: {item.Subarea}, Rol: {item.Rol}, Empresa: {item.Empresa}, Usuario: {usuario}");

                        var consulta = @"SELECT * FROM turnoavianca WHERE subarea = @Subarea AND codrl = @CodRol AND empresa = @Empresa AND usuario = @Usuario AND eliminado = '0'";

                        var turnosObtenidos = await _doConnection.QueryAsync<TurnoAvianca>(consulta, parametros, transaction: _doTransaction);
                        Console.WriteLine($"[DEBUG] Turnos encontrados: {turnosObtenidos?.Count() ?? 0}");

                        if (!turnosObtenidos.Any())
                        {
                            Console.WriteLine($"[DEBUG] ERROR: No se encontraron turnos para los parámetros especificados");
                            listaErrores.Add(new ListaErrores
                            {
                                Item = ++contErrores,
                                CodigoOracle = item.CodigoOracle,
                                Nombre = item.Nombre,
                                Subarea = item.Subarea,
                                Rol = item.Rol,
                                Motivo = "Turno no encontrado",
                                Archivo = archivo
                            });
                        }
                        else
                        {
                            turnoList.AddRange(turnosObtenidos);
                            Console.WriteLine($"[DEBUG] Agregados {turnosObtenidos.Count()} turnos a la lista");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] ERROR: Campo Rol vacío para código: {item.CodigoOracle}");
                        listaErrores.Add(new ListaErrores
                        {
                            Item = ++contErrores,
                            CodigoOracle = item.CodigoOracle,
                            Nombre = item.Nombre,
                            Subarea = item.Subarea,
                            Rol = item.Rol,
                            Motivo = "El campo Rol está vacío",
                            Archivo = archivo
                        });
                    }

                    foreach (var turno in turnoList)
                    {
                        Console.WriteLine($"[DEBUG] Procesando turno - Tipo: {turno.Tipo}, Hora: {turno.Hora}");

                        var consultaPreplan = @"SELECT codigo, orden, numero FROM preplan WHERE codcliente = @CodCliente AND tipo = @Tipo AND cerrado = '1' AND borrado = '0' AND eliminado = '0' ORDER BY codigo DESC LIMIT 1";

                        var parametrosPreplan = new
                        {
                            CodCliente = item.CodigoOracle,
                            Tipo = turno.Tipo,
                        };

                        var preplanObtenido = await _doConnection.QueryFirstOrDefaultAsync<PreTurno>(consultaPreplan, parametrosPreplan, transaction: _doTransaction);
                        Console.WriteLine($"[DEBUG] Preplan obtenido: {(preplanObtenido != null ? $"Codigo: {preplanObtenido.Codigo}, Orden: {preplanObtenido.Orden}, Numero: {preplanObtenido.Numero}" : "NULL")}");

                        // Valores por defecto para clientes nuevos
                        string lastOrden = "0";
                        string lastNumero = "0";

                        if (preplanObtenido != null)
                        {
                            Console.WriteLine($"[DEBUG] Usando preplan existente como referencia");
                            preturno.Add(new PreTurno
                            {
                                Codigo = preplanObtenido.Codigo,
                                Orden = preplanObtenido.Orden,
                                Numero = preplanObtenido.Numero
                            });

                            lastOrden = preplanObtenido.Orden;
                            lastNumero = preplanObtenido.Numero;
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] No se encontró preplan previo. Creando como cliente nuevo con valores por defecto");
                            // Para clientes nuevos, usar valores iniciales
                            lastOrden = "0";
                            lastNumero = "0";
                        }

                        var programa = await ObtenerProgramaAsync(turno.Tipo, item.Subarea, item.Rol, turno.Empresa, usuario);
                        Console.WriteLine($"[DEBUG] Programa obtenido: {programa}");

                        // Determina la fecha ajustada
                        DateTime fechaBase = DateTime.ParseExact(fecactFormat, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        Console.WriteLine($"[DEBUG] Fecha base antes del ajuste: {fechaBase:dd/MM/yyyy}");

                        if (programa == "3")
                            fechaBase = fechaBase.AddDays(-1);
                        else if (programa == "2")
                            fechaBase = fechaBase.AddDays(1);

                        Console.WriteLine($"[DEBUG] Fecha final ajustada: {fechaBase:dd/MM/yyyy}");

                        var nuevoPedido = new Pedido
                        {
                            Codcliente = item.CodigoOracle,
                            Nombre = item.Nombre,
                            Rol = item.Rol,
                            Fecha = fechaBase.ToString("dd/MM/yyyy") + " " + turno.Hora,
                            Tipo = turno.Tipo,
                            Usuario = turno.Usuario,
                            Area = item.Area,
                            Fecreg = fechaActual,
                            Lastorden = lastOrden,
                            Lastnumero = lastNumero,
                            Empresa = turno.Empresa,
                        };

                        pedido.Add(nuevoPedido);
                        Console.WriteLine($"[DEBUG] Pedido agregado: {item.CodigoOracle} - {nuevoPedido.Fecha} (LastOrden: {lastOrden}, LastNumero: {lastNumero})");
                    }
                }

                Console.WriteLine($"[DEBUG] Total de pedidos a insertar: {pedido.Count}");
                Console.WriteLine($"[DEBUG] Total de errores encontrados: {listaErrores.Count}");

                if (!pedido.Any())
                {
                    Console.WriteLine($"[DEBUG] No hay pedidos para insertar, retornando con errores");
                    return new InsertPedidoResponse
                    {
                        Success = false,
                        Errores = listaErrores
                    };
                }

                Console.WriteLine($"[DEBUG] Iniciando inserción en base de datos...");

                var consultaPedido = @"INSERT INTO preplan (codcliente, nombre, rol, fecha, tipo, usuario, area, fecreg, lastorden, lastnumero, empresa, arealatam, destinocodigo) 
            VALUES (@Codcliente, @Nombre, @Rol, @Fecha, @Tipo, @Usuario, @Area, @Fecreg, @Lastorden, @Lastnumero, @Empresa, '', '')";

                int insertedCount = 0;
                foreach (var item in pedido)
                {
                    Console.WriteLine($"[DEBUG] Insertando pedido {++insertedCount}/{pedido.Count}: {item.Codcliente} - {item.Fecha}");
                    await _doConnection.ExecuteAsync(consultaPedido, item, transaction: _doTransaction);
                }

                Console.WriteLine($"[DEBUG] Transacción confirmada exitosamente");

                return new InsertPedidoResponse
                {
                    Success = true,
                    Errores = listaErrores
                };
            }
            catch (Exception ex)
            {
                return new InsertPedidoResponse();
            }
        }


        private async Task<string> ObtenerProgramaAsync(string tipo, string subarea, string codrl, string empresa, string usuario)
        {
            var sql = @"SELECT programa FROM turnoavianca WHERE tipo = @Tipo AND subarea = @Subarea AND codrl = @CodRol AND empresa = @Empresa AND usuario = @Usuario AND eliminado = '0' LIMIT 1";

            var parametros = new { Tipo = tipo, Subarea = subarea, CodRol = codrl, Empresa = empresa, Usuario = usuario };

            return await _doConnection.QueryFirstOrDefaultAsync<string>(sql, parametros, transaction: _doTransaction);
        }


        public async Task<int> SavePedidos(IEnumerable<Pedido> pedidos, string usuario)
        {
            int resultado = 1;

            foreach (var pedido in pedidos)
            {
                int operacionExitosa = 0;

                if (pedido.Eliminado == "0")
                {
                    operacionExitosa = await Savepreplan(pedido, usuario);
                }
                else if (pedido.Eliminado == "1")
                {
                    operacionExitosa = await SaveElimPlan(pedido, usuario);
                }

                if (operacionExitosa == 0)
                {
                    resultado = 0;
                }
            }

            return resultado;
        }

        private async Task<int> Savepreplan(Pedido pedido, string usuario)
        {
            string sql = @"UPDATE preplan SET horaprog = @HoraProg, orden = @Orden, numero = @Numero, codtarifa = @CodTarifa, codconductor = @CodConductor, codunidad = @CodUnidad, eliminado = '0', destinocodigo = @DestinoCodigo, replanorden = @ReplanOrden, replannumero = @ReplanNumero WHERE codigo = @Codigo AND usuario = @Usuario";

            var parameters = new
            {
                HoraProg = pedido.Horaprog,
                Orden = pedido.Orden,
                Numero = pedido.Numero,
                CodTarifa = pedido.Codtarifa,
                CodConductor = pedido.Codconductor,
                CodUnidad = pedido.Codunidad,
                DestinoCodigo = pedido.Destinocodigo,
                ReplanOrden = pedido.Replanorden,
                ReplanNumero = pedido.Replannumero,
                Codigo = pedido.Codigo,
                Usuario = usuario
            };

            int rowsAffected = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);

            return rowsAffected > 0 ? 1 : 0;
        }

        private async Task<int> SaveElimPlan(Pedido pedido, string usuario)
        {
            string sql = @"UPDATE preplan SET eliminado = '1' WHERE codigo = @Codigo AND usuario = @Usuario";

            var parameters = new
            {
                Codigo = pedido.Codigo,
                Usuario = usuario
            };

            int rowsAffected = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);

            return rowsAffected > 0 ? 1 : 0;
        }

        public async Task<int> BorrarPlan(string empresa, string fecha, string usuario)
        {
            // Normaliza el nombre de la empresa antes de ejecutar la consulta
            string empresaNormalizada = SetEmpresa(empresa);

            // Definir consulta SQL según la empresa
            string sql = empresaNormalizada == "LATAM"
                ? "UPDATE preplan SET borrado='1' WHERE empresa='LATAM' AND arealatam='TIERRA' AND usuario=@Usuario AND cerrado='0' AND fecreg=@Fecha"
                : "UPDATE preplan SET borrado='1' WHERE empresa=@Empresa AND usuario=@Usuario AND cerrado='0' AND fecreg=@Fecha";

            var parameters = new
            {
                Empresa = empresaNormalizada,
                Usuario = usuario,
                Fecha = fecha
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<List<LugarCliente>> GetLugares(string coddcliente)
        {
            string sql = "SELECT * FROM lugarcliente WHERE codcli = @Codcliente";

            var lugares = await _doConnection.QueryAsync<LugarCliente>(sql, new { Codcliente = coddcliente }, transaction: _doTransaction);

            return lugares.ToList();
        }

        public async Task<int> UpdateDirec(string coddire, string codigo)
        {
            string sql = "UPDATE preplan SET destinocodlugar = @CodDire WHERE codigo = @Codigo";

            var parameters = new
            {
                CodDire = coddire,
                Codigo = codigo
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<List<Usuario>> GetConductores(string usuario)
        {
            string sql = @"SELECT codtaxi, nombres, apellidos FROM taxi WHERE codusuario = @Usuario and estado = 'A' and habilitado = '1'";

            var conductores = await _doConnection.QueryAsync<dynamic>(sql, new { Usuario = usuario }, transaction: _doTransaction);

            var listaConductores = new List<Usuario>();

            foreach (var row in conductores)
            {
                var conductor = new Usuario
                {
                    Codigo = row.codtaxi.ToString(),
                    Nombre = row.nombres,
                    Apepate = row.apellidos
                };

                listaConductores.Add(conductor);
            }

            return listaConductores;
        }

        public async Task<List<Unidad>> GetUnidades(string usuario)
        {
            const string sqlDevices = @"SELECT deviceID FROM device WHERE habilitada = '1' AND accountID = @Usuario";

            const string sqlDeviceUsers = @"SELECT DeviceID FROM deviceuser WHERE UserId = @Usuario";

            var dispositivos = await _defaultConnection.QueryAsync<string>(
                sqlDevices,
                new { Usuario = usuario },
                transaction: _defaultTransaction);

            var dispositivosUsuarios = await _defaultConnection.QueryAsync<string>(
                sqlDeviceUsers,
                new { Usuario = usuario },
                transaction: _defaultTransaction);

            // Combina y elimina duplicados
            var codigosUnicos = dispositivos.Concat(dispositivosUsuarios).Distinct();

            var unidades = new List<Unidad>();
            int contador = 1;

            foreach (var device in codigosUnicos)
            {
                unidades.Add(new Unidad
                {
                    Id = contador++,
                    Codunidad = device
                });
            }

            return unidades;
        }

        public async Task<List<Servicio>> CreateServicios(string fecha, string empresa, string usuario)
        {
            Console.WriteLine($"[CreateServicios] Iniciando método - Fecha: {fecha}, Empresa: {empresa}, Usuario: {usuario}");

            try
            {
                List<Servicio> lista = new List<Servicio>();
                string usu = "";

                if (new HashSet<string> { "dramirez", "mnazario", "pruebas", "joalbarracin", "wilbarrientos" }.Contains(usuario))
                {
                    usu = "movilbus";
                    Console.WriteLine($"[CreateServicios] Usuario convertido de '{usuario}' a 'movilbus'");
                }
                else
                {
                    usu = usuario;
                    Console.WriteLine($"[CreateServicios] Usuario mantenido: {usu}");
                }

                // Parsear la fecha
                DateTime fec1;
                if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fec1))
                {
                    Console.WriteLine($"[CreateServicios] ERROR: Formato de fecha incorrecto - {fecha}");
                    throw new FormatException("Formato de fecha incorrecto");
                }

                string fecini = fec1.ToString("dd/MM/yyyy") + " 00:00";
                string fecfin = fec1.ToString("dd/MM/yyyy") + " 23:59";
                Console.WriteLine($"[CreateServicios] Rango de fechas - Inicio: {fecini}, Fin: {fecfin}");

                List<Pedido> listapreplan = await Listacierre(usu, fecini, fecfin, empresa);
                Console.WriteLine($"[CreateServicios] Pedidos obtenidos de Listacierre: {listapreplan.Count}");

                if (listapreplan.Count > 0)
                {
                    Console.WriteLine("[CreateServicios] Procesando pedidos...");
                    string serv = "", servact = "";
                    Servicio? s = null;
                    List<Pedido>? subserv = null;
                    int numlista = listapreplan.Count - 1;
                    int numlan = 0;

                    for (int x = 0; x < listapreplan.Count; x++)
                    {
                        Pedido pe = listapreplan[x];
                        servact = pe.Numero;
                        Console.WriteLine($"[CreateServicios] Procesando pedido {x + 1}/{listapreplan.Count} - Número: {servact}");

                        if (serv != servact)
                        {
                            Console.WriteLine($"[CreateServicios] Nuevo servicio detectado: {servact}");

                            if (subserv != null && s != null)
                            {
                                s.Listapuntos = subserv;
                                lista.Add(s);
                                Console.WriteLine($"[CreateServicios] Servicio {s.Numero} agregado a la lista con {subserv.Count} puntos");
                            }

                            s = new Servicio();
                            subserv = new List<Pedido>();
                            Pedido pedato = new Pedido { Orden = "0" };
                            Servicio sdestino = pe.Servicio;
                            Usuario? aereo = null;
                            string destinoFinal = "";

                            if (string.IsNullOrEmpty(sdestino.Destino))
                            {
                                Console.WriteLine("[CreateServicios] Destino vacío, usando código 4175 por defecto");
                                aereo = new Usuario { Codigo = "4175", Codlan = "4175", Empresa = "ninguno" };
                                destinoFinal = "4175";
                                pedato.Pasajero = aereo;
                                pedato.Numero = pe.Numero;
                            }
                            else
                            {
                                Console.WriteLine($"[CreateServicios] Buscando destino: {sdestino.Destino}");
                                aereo = await BuscarDestino(sdestino.Destino);

                                if (aereo == null)
                                {
                                    Console.WriteLine($"[CreateServicios] Destino '{sdestino.Destino}' no encontrado, usando 4175 por defecto");
                                    aereo = new Usuario { Codigo = "4175", Codlan = "4175", Empresa = "ninguno" };
                                    destinoFinal = "4175";
                                }
                                else
                                {
                                    Console.WriteLine($"[CreateServicios] Destino encontrado: {sdestino.Destino}");
                                    destinoFinal = sdestino.Destino;
                                }

                                pedato.Pasajero = aereo;
                                pedato.Numero = pe.Numero;
                            }

                            pedato.Fechaini = (pe.Servicio.Tipo == "S") ? pe.Fecha : pe.Fechaini;
                            pedato.Arealan = pe.Arealan;
                            subserv.Add(pedato);

                            numlan = (pe.Empresa == "LATAM") ? int.Parse(pe.Lastnumero) : int.Parse(pe.Numero) + 1;
                            Console.WriteLine($"[CreateServicios] Número asignado: {numlan} (Empresa: {pe.Empresa})");

                            s.Numero = numlan.ToString();
                            s.Usuario = usu;
                            s.Tipo = pe.Servicio.Tipo;
                            s.Grupo = (pe.Empresa == "LATAM") ? "T" : "N";
                            s.Fecha = pe.Fecha;
                            s.Empresa = pe.Empresa;
                            s.Conductor = pe.Servicio.Conductor;
                            s.Unidad = pe.Servicio.Unidad;
                            s.Zona = pe.Servicio.Zona;
                            s.Fecpreplan = pe.Fechaini;
                            s.Destino = destinoFinal;
                            serv = pe.Numero;

                            pedato.Numero = numlan.ToString();
                            Console.WriteLine($"[CreateServicios] Servicio configurado - Tipo: {s.Tipo}, Grupo: {s.Grupo}, Destino: {s.Destino}");
                        }

                        pe.Numero = numlan.ToString();
                        subserv?.Add(pe);

                        if (x == numlista && subserv != null && s != null)
                        {
                            s.Listapuntos = subserv;
                            lista.Add(s);
                            Console.WriteLine($"[CreateServicios] Último servicio {s.Numero} agregado con {subserv.Count} puntos");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[CreateServicios] No hay pedidos para procesar");
                }

                Console.WriteLine($"[CreateServicios] Total de servicios creados en lista: {lista.Count}");

                List<Servicio> serviciosValidosProcesados = new List<Servicio>();

                if (lista.Count > 0)
                {
                    Console.WriteLine("[CreateServicios] Iniciando validación de servicios...");

                    // Filtrar solo servicios válidos
                    List<Servicio> serviciosValidos = new List<Servicio>();
                    List<string> erroresValidacion = new List<string>();

                    foreach (Servicio suf in lista)
                    {
                        Console.WriteLine($"[CreateServicios] Validando servicio {suf.Numero}...");

                        if (ValidarFechasServicio(suf))
                        {
                            serviciosValidos.Add(suf);
                            Console.WriteLine($"[CreateServicios] Servicio {suf.Numero} validado correctamente");
                        }
                        else
                        {
                            string error = $"Servicio {suf.Numero} no pasó validación de fechas";
                            erroresValidacion.Add(error);
                            Console.WriteLine($"[CreateServicios] ERROR: {error}");
                        }
                    }

                    Console.WriteLine($"[CreateServicios] Servicios válidos: {serviciosValidos.Count}/{lista.Count}");
                    if (erroresValidacion.Count > 0)
                    {
                        Console.WriteLine($"[CreateServicios] Errores de validación: {string.Join(", ", erroresValidacion)}");
                    }

                    Console.WriteLine("[CreateServicios] Iniciando grabación de servicios...");
                    foreach (Servicio su in serviciosValidos)
                    {
                        Console.WriteLine($"[CreateServicios] Grabando servicio {su.Numero}...");
                        Servicio? sur = await GrabarServicios(su, usu);

                        if (sur?.Codservicio != null)
                        {
                            Console.WriteLine($"[CreateServicios] Servicio {su.Numero} grabado con código: {sur.Codservicio}");
                            Console.WriteLine($"[CreateServicios] Cerrando preplanes para servicio {sur.Codservicio}...");

                            int puntosProcessados = 0;
                            foreach (Pedido pe in su.Listapuntos ?? new List<Pedido>())
                            {
                                await CerrarPreplan(usu, pe.Codigo, sur.Codservicio);
                                puntosProcessados++;
                            }

                            Console.WriteLine($"[CreateServicios] {puntosProcessados} preplanes cerrados para servicio {sur.Codservicio}");
                            serviciosValidosProcesados.Add(sur);
                        }
                        else
                        {
                            Console.WriteLine($"[CreateServicios] ERROR: Servicio {su.Numero} no pudo ser grabado");
                        }
                    }
                }

                await NumeracionMovil(fecini, fecfin, usu);

                return serviciosValidosProcesados;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en CreateServicios", ex);
            }
        }

        private bool ValidarFechasServicio(Servicio servicio)
        {
            try
            {
                //Validar que la fecha principal del servicio no sea nula o vacía
                if (string.IsNullOrWhiteSpace(servicio.Fecpreplan))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<int> NumeracionMovil(string fecini, string fecfin, string usuario)
        {
            try
            {

                var parameters = new DynamicParameters();
                parameters.Add("fecini", fecini, DbType.String);
                parameters.Add("fecfin", fecfin, DbType.String);
                parameters.Add("usuario", usuario, DbType.String);

                int result = await _defaultConnection.ExecuteAsync(
                    "numserviciomovil",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: _defaultTransaction
                );

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> CerrarPreplan(string usu, int? codigo, string codservicio)
        {
            string sql = @"Update preplan set cerrado='1', codservicio = @Codservicio where usuario = @Usuario and codigo = @Codigo";

            var parameters = new
            {
                CodServicio = codservicio,
                Usuario = usu,
                Codigo = codigo
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        private async Task<Servicio?> GrabarServicios(Servicio su, string usuario)
        {
            Console.WriteLine($"[GrabarServicios] Iniciando grabación - Servicio: {su.Numero}, Usuario: {usuario}, Tipo: {su.Tipo}");

            int r = 0;
            List<Pedido> listapuntos = su.Listapuntos ?? new List<Pedido>();
            //su.Numpax = listapuntos.Count.ToString();

            Console.WriteLine($"[GrabarServicios] Puntos a procesar: {listapuntos.Count}");
            Console.WriteLine($"[GrabarServicios] Servicio configurado - Empresa: {su.Empresa}, Zona: {su.Zona}, Destino: {su.Destino}");

            Console.WriteLine("[GrabarServicios] Creando nuevo servicio...");
            Servicio sur = await NuevoServicio(su, usuario);

            if (sur != null)
            {
                Console.WriteLine($"[GrabarServicios] Servicio creado exitosamente con ID: {sur.Codservicio}");
            }
            else
            {
                Console.WriteLine("[GrabarServicios] ERROR: No se pudo crear el servicio");
                return null;
            }

            Console.WriteLine($"[GrabarServicios] Procesando {listapuntos.Count} puntos del servicio...");

            for (int b = 0; b < listapuntos.Count; b++)
            {
                Console.WriteLine($"[GrabarServicios] Procesando punto {b + 1}/{listapuntos.Count}...");

                Pedido pe = listapuntos[b];

                if (pe.Pasajero == null)
                {
                    Console.WriteLine($"[GrabarServicios] ERROR: El pedido en la posición {b} no tiene pasajero asociado");
                    throw new Exception($"El pedido en la posición {b} no tiene pasajero asociado.");
                }

                Console.WriteLine($"[GrabarServicios] Pedido {b} - Pasajero: {pe.Pasajero.Codigo}, Código pedido: {pe.Codigo}");

                Console.WriteLine($"[GrabarServicios] Buscando datos del pasajero {pe.Pasajero.Codigo}...");
                Usuario us = await LugarPasajero(pe.Pasajero);

                if (us == null)
                {
                    Console.WriteLine($"[GrabarServicios] ERROR: No se encontró el usuario para el pasajero con ID {pe.Pasajero?.Codigo}");
                    throw new Exception($"No se encontró el usuario para el pasajero con ID {pe.Pasajero?.Codigo}");
                }

                Console.WriteLine($"[GrabarServicios] Pasajero encontrado - Nombre: {us.Nombre}, Lugar: {us.Lugar?.Direccion}");

                if (pe.Lugar?.Codlugar != null)
                {
                    Console.WriteLine($"[GrabarServicios] Actualizando lugar del pasajero de {us.Lugar?.Codlugar} a {pe.Lugar.Codlugar}");
                    us.Lugar.Codlugar = pe.Lugar.Codlugar;
                }
                else
                {
                    Console.WriteLine($"[GrabarServicios] Manteniendo lugar original del pasajero: {us.Lugar?.Codlugar}");
                }

                // Configurar fecha según el tipo de servicio
                string fechaAnterior = pe.Fecha;
                pe.Fecha = (su.Tipo == "S") ? su.Fecha : pe.Fechaini;

                if (fechaAnterior != pe.Fecha)
                {
                    Console.WriteLine($"[GrabarServicios] Fecha actualizada de '{fechaAnterior}' a '{pe.Fecha}' (Tipo servicio: {su.Tipo})");
                }

                pe.Pasajero = us;
                pe.Servicio = sur;
                pe.Lugar = us.Lugar;
                pe.Orden = b.ToString();

                Console.WriteLine($"[GrabarServicios] Configurando subservicio - Orden: {pe.Orden}, Fecha: {pe.Fecha}");
                Console.WriteLine($"[GrabarServicios] Grabando subservicio para pedido {pe.Codigo}...");

                r = await NuevoSubServicio(pe);

                if (r > 0)
                {
                    Console.WriteLine($"[GrabarServicios] Subservicio {b + 1} grabado exitosamente (ID: {r})");
                }
                else
                {
                    Console.WriteLine($"[GrabarServicios] WARNING: Subservicio {b + 1} retornó valor {r}");
                }
            }

            Console.WriteLine($"[GrabarServicios] Grabación completada - Servicio: {sur.Codservicio}, Puntos procesados: {listapuntos.Count}");
            Console.WriteLine($"[GrabarServicios] Último resultado de subservicio: {r}");

            return sur;
        }

        private async Task<int> NuevoSubServicio(Pedido pedido)
        {
            string sql = @"INSERT INTO subservicio (codubicli, fecha, estado, codcliente, numero, codservicio, distancia, categorialan, arealan, vuelo, orden, centrocosto, costo, observacion) 
            VALUES (@Codubicli, @Fecha, 'P', @Codcliente, @Numero, @Codservicio, @Distancia, @Categorialan, @Arealan, @Vuelo, @Orden, @Centrocosto, @Costo, @Observacion)";

            var parameters = new
            {
                Codubicli = pedido.Lugar?.Codlugar,
                Fecha = pedido.Fecha,
                Codcliente = pedido.Pasajero?.Codigo,
                Numero = pedido.Numero,
                Codservicio = pedido.Servicio?.Codservicio,
                Distancia = pedido.Distancia,
                Categorialan = pedido.Categorialan,
                Arealan = pedido.Arealan,
                Vuelo = pedido.Vuelo,
                Orden = pedido.Orden,
                Centrocosto = pedido.Centrocosto,
                Costo = pedido.Tarifa,
                Observacion = pedido.Observacion
            };

            int filasAfectadas = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);

            //INCREMENTAR TOTALPAX SI SE INSERTÓ EXITOSAMENTE
            if (filasAfectadas > 0 && !string.IsNullOrEmpty(pedido.Servicio?.Codservicio))
            {
                await IncrementarTotalPax(pedido.Servicio.Codservicio);
            }

            return filasAfectadas;
        }

        private async Task<int> IncrementarTotalPax(string codservicio)
        {
            // Convertir el string a int de forma segura
            if (!int.TryParse(codservicio, out int codservicioInt))
            {
                throw new ArgumentException("El código de servicio no es válido. Debe ser un número entero.");
            }

            string sql = @"UPDATE servicio SET totalpax = CAST(CAST(totalpax AS UNSIGNED) + 1 AS CHAR) WHERE codservicio = @Codservicio";

            return await _doConnection.ExecuteAsync(sql,
                new
                {
                    Codservicio = codservicioInt
                },
                transaction: _doTransaction
            );
        }


        private async Task<Usuario> LugarPasajero(Usuario pasajero)
        {
            string sql = @"Select l.codlugar, l.wy, l.wx, c.codcliente from cliente c, lugarcliente l where l.codcli = c.codlugar and c.codlan = @Codlan and c.estadocuenta = 'A' and l.estado = 'A'";

            var parameters = new { Codlan = pasajero.Codlan };

            var result = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!result.Any())
            {
                return null;
            }

            var row = result.First();

            return new Usuario
            {
                Codigo = row.codcliente.ToString(),
                Lugar = new LugarCliente
                {
                    Codlugar = row.codlugar,
                    Wy = row.wy,
                    Wx = row.wx
                }
            };
        }

        private async Task<Servicio> NuevoServicio(Servicio servicio, string usuario)
        {
            string sql = @"INSERT INTO servicio (numero, tipo, codusuario, estado, fecha, grupo, empresa, totalpax, unidad, codconductor, codzona, fecplan, destino, owner, costototal, numeromovil) 
                   VALUES (@Numero, @Tipo, @CodUsuario, 'P', @Fecha, @Grupo, @Empresa, @TotalPax, @CodUnidad, @CodConductor, @CodZona, @FecPlan, @Destino, @Owner, '0', @NumeroMovil)";

            var parameters = new
            {
                Numero = servicio.Numero,
                Tipo = servicio.Tipo,
                CodUsuario = usuario,
                Fecha = servicio.Fecha,
                Grupo = servicio.Grupo,
                Empresa = servicio.Empresa,
                TotalPax = "0",
                CodUnidad = servicio.Unidad.Codunidad,
                CodConductor = servicio.Conductor.Codigo,
                CodZona = servicio.Zona.Codigo,
                FecPlan = servicio.Fecpreplan,
                Destino = servicio.Destino,
                Owner = usuario,
                NumeroMovil = servicio.Numero
            };

            await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);

            string sqlSelect = "SELECT codservicio FROM servicio ORDER BY codservicio DESC LIMIT 1";

            var result = await _doConnection.QueryFirstOrDefaultAsync<Servicio>(sqlSelect, transaction: _doTransaction);

            if (result == null)
            {
                throw new Exception("Error al recuperar el servicio insertado.");
            }

            return result;
        }

        private async Task<Usuario?> BuscarDestino(string codcliente)
        {
            // Convertir string codcliente a int
            if (!int.TryParse(codcliente, out int codclienteInt))
            {
                return null;
            }

            string sql = "Select codcliente, nombres, apellidos, login, clave, codlugar, codlan, sexo, empresa from cliente where codcliente = @Codcliente and estadocuenta = 'A'";

            var parameters = new
            {
                Codcliente = codclienteInt,
            };

            var result = await _doConnection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters, transaction: _doTransaction);

            if (result == null)
            {
                return null;
            }

            var usuario = new Usuario
            {
                Codigo = result.codcliente.ToString(),
                Nombre = result.nombres?.ToString(),
                Codlan = result.codlan?.ToString(),
                Apepate = result.apellidos?.ToString(),
                Login = result.login?.ToString(),
                Clave = result.clave?.ToString(),
                Sexo = result.sexo?.ToString(),
                Empresa = result.empresa?.ToString(),
                Lugar = new LugarCliente
                {
                    Codlugar = (result.codlugar is int) ? result.codlugar :
                       (int.TryParse(result.codlugar?.ToString(), out int codlugarInt) ? codlugarInt : 0)
                }
            };

            return usuario;
        }

        private async Task<List<Pedido>> Listacierre(string usu, string fecini, string fecfin, string empresa)
        {
            int contador = 1;

            string sql = @"select p.destinocodigo, p.codigo, p.codcliente, p.nombre, p.rol, p.fecha, p.area, p.horaprog, p.orden, p.numero, p.lastnumero, p.codtarifa, p.codconductor, p.codunidad, p.tipo, p.empresa, STR_TO_DATE(p.fecha,'%d/%m/%Y %H:%i') as formato, p.destinocodlugar, l.codlugar, l.direccion, l.wx, l.wy, l.distrito, l.zona, p.eliminado from preplan p, lugarcliente l where p.codcliente=l.codcli and STR_TO_DATE(p.fecha,'%d/%m/%Y %H:%i')>=STR_TO_DATE(@Fecini,'%d/%m/%Y %H:%i') and STR_TO_DATE(p.fecha,'%d/%m/%Y %H:%i')<=STR_TO_DATE(@Fecfin,'%d/%m/%Y %H:%i') and l.estado='A' and cerrado='0' and eliminado='0' and borrado='0' and usuario = @Usuario and empresa=@Empresa and horaprog is not null and numero is not null order by empresa, eliminado, formato, numero, orden * 1";

            var parameters = new { Usuario = usu, Fecini = fecini, Fecfin = fecfin, Empresa = empresa };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            var listaPedidos = results.Select(row => new Pedido
            {
                Id = contador++,
                Codigo = row.codigo,
                Fecha = row.fecha,
                Rol = row.rol,
                Arealan = row.area,
                Fechaini = row.horaprog,
                Orden = row.orden,
                Numero = row.numero,
                Lastnumero = row.lastnumero,
                Empresa = row.empresa,
                Eliminado = row.eliminado,
                Servicio = new Servicio
                {
                    Usuario = usu,
                    Destino = row.destinocodigo,
                    Tipo = row.tipo,
                    Conductor = new Usuario
                    {
                        Codigo = row.codconductor,
                    },
                    Zona = new Zona
                    {
                        Codigo = row.codtarifa
                    },
                    Unidad = new Unidad
                    {
                        Codunidad = row.codunidad,
                    }
                },
                Destinocodigo = row.destinocodigo,
                Pasajero = new Usuario
                {
                    Codigo = row.codcliente,     // ← Agregar esta línea
                    Apepate = row.nombre,
                    Codlan = row.codcliente,
                    Empresa = row.empresa,

                },
                Lugar = new LugarCliente
                {
                    Codlugar = !string.IsNullOrEmpty(row.destinocodlugar) ? int.Parse(row.destinocodlugar) : row.codlugar,
                    Direccion = row.direccion,
                    Wx = row.wx,
                    Wy = row.wy,
                    Distrito = row.distrito,
                    Zona = row.zona
                }
            }).ToList();

            return listaPedidos;
        }

        public async Task<List<Servicio>> GetServicios(string fecha, string usuario)
        {
            // Parsear la fecha
            DateTime fec1;
            if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fec1))
            {
                throw new FormatException("Formato de fecha incorrecto");
            }

            string fechaini = fec1.ToString("dd/MM/yyyy") + " 00:00";
            string fechafin = fec1.ToString("dd/MM/yyyy") + " 23:59";

            List<Servicio> lista = await ControlServiciosMovil(fechaini, fechafin, usuario);

            if (lista == null)
            {
                return new List<Servicio>();
            }

            if (lista.Any())
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    Servicio s = lista[i];

                    if (!string.IsNullOrEmpty(s.Conductor?.Codigo))
                    {
                        Usuario conductor = await GetDetalleConductor(s.Conductor.Codigo);
                        s.Conductor = conductor;
                    }

                    if (!string.IsNullOrEmpty(s.Owner?.Codigo))
                    {
                        Usuario owner = await DetallePasajero(s.Owner);
                        s.Owner = owner;
                    }

                    lista[i] = s;
                }
            }

            return lista;
        }

        private async Task<Usuario> DetallePasajero(Usuario owner)
        {
            string sql = @"select codcliente, nombres, apellidos, login, clave, codlugar, codlan, sexo, empresa, telefono from cliente where codcliente=@Codcliente";

            var parameters = new { Codcliente = owner.Codigo };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!results.Any())
            {
                return null;
            }

            var row = results.First();

            var pasajero = new Usuario
            {
                Codigo = row.codcliente,
                Nombre = row.nombres,
                Apepate = row.apellidos,
                Login = row.login,
                Clave = row.clave,
                Codlan = row.codlan,
                Sexo = row.sexo,
                Empresa = row.empresa,
                Telefono = row.telefono,
                Lugar = new LugarCliente
                {
                    Codlugar = int.TryParse(row.codlugar, out int codLugar) ? codLugar : 0
                }
            };
            return pasajero;
        }

        private async Task<List<Servicio>> ControlServiciosMovil(string fechaini, string fechafin, string usu)
        {
            string sql = @"Select s.costototal, s.owner, s.numero, s.codservicio, s.tipoarea, s.tipo, s.totalpax, s.numeromovil, s.empresa, s.grupo, s.fecha, s.unidad, s.fechainifin, s.fechaini, s.fechafin, s.fecplan, s.atolatam, s.gourmetlatam, s.parqueolatam, s.lcclatam, s.codusuario, STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i') as formato, s.codconductor, s.estado as estadoservicio, s.tipoturismo, s.grupoturismo, s.destino from servicio s where STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i')>=STR_TO_DATE(@Fecini,'%d/%m/%Y %H:%i') and STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i')<=STR_TO_DATE(@Fecfin,'%d/%m/%Y %H:%i') and s.codusuario=@Usuario order by formato, codservicio";

            var parameters = new { Fecini = fechaini, Fecfin = fechafin, Usuario = usu };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (results == null)
            {
                return new List<Servicio>();
            }

            var listaServicios = new List<Servicio>();

            foreach (var row in results)
            {
                try
                {
                    string nombreDestino = string.Empty;
                    if (!string.IsNullOrEmpty(row.destino))
                    {
                        var usuarioDestino = await BuscarDestino(row.destino.ToString());
                        if (usuarioDestino != null)
                        {
                            nombreDestino = usuarioDestino.Nombre ?? usuarioDestino.Apepate ?? string.Empty;
                        }
                        else
                        {
                            nombreDestino = "Destino Aeropuerto Jorge Chávez";
                        }
                    }

                    var servicio = new Servicio
                    {
                        Codservicio = row.codservicio?.ToString() ?? string.Empty,
                        Numero = row.numeromovil ?? string.Empty,
                        Numeromovil = row.numero ?? string.Empty,
                        Grupo = row.grupo ?? string.Empty,
                        Fecha = row.fecha ?? string.Empty,
                        Fecatoavianca = row.fechainifin ?? string.Empty,
                        Fecparqueolatam = row.parqueolatam ?? string.Empty,
                        Fecatolatam = row.atolatam ?? string.Empty,
                        Fecgourmetlatam = row.gourmetlatam ?? string.Empty,
                        Feclcclatam = row.lcclatam ?? string.Empty,
                        Empresa = row.empresa ?? string.Empty,
                        Numpax = row.totalpax ?? "0",
                        Usuario = row.codusuario ?? string.Empty,
                        Fecplan = row.fecplan ?? string.Empty,
                        Newfechaini = row.fechaini ?? string.Empty,
                        Newfechafni = row.fechafin ?? string.Empty,
                        Estado = row.estadoservicio ?? string.Empty,
                        Area = row.tipoarea != null ? "TURISMO" : "TEP",
                        Tipo = row.tipoarea != null ? (row.tipoturismo ?? string.Empty) : (row.tipo ?? string.Empty),
                        Nomgrupo = row.grupoturismo ?? string.Empty,
                        Costototal = row.costototal ?? "0",
                        Destino = row.destino?.ToString() ?? string.Empty,
                        NomDestino = nombreDestino,
                        Owner = new Usuario
                        {
                            Codigo = row.owner ?? string.Empty
                        },

                        Conductor = new Usuario
                        {
                            Codigo = row.codconductor ?? string.Empty
                        },

                        Unidad = new Unidad
                        {
                            Codunidad = row.unidad ?? string.Empty
                        }
                    };

                    listaServicios.Add(servicio);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ControlServiciosMovil] ⚠️ ERROR al mapear servicio. Codservicio: {row.codservicio ?? "null"} - Error: {ex.Message}");
                }
            }

            return listaServicios;
        }

        //Agregar filtro por codusuario movilbus o gacela
        public async Task<List<Usuario>> GetPasajeros(string palabra, string codusuario)
        {
            string sql = @"SELECT DISTINCT l.codlugar, c.codcliente, c.nombres, c.apellidos, c.codlan, l.wy, l.wx, l.direccion, l.distrito, l.zona from cliente c, lugarcliente l where l.codcli=c.codlugar and c.estadocuenta='A' and l.estado='A' and c.apellidos like @Palabra and c.destino='0' LIMIT 10";

            var parameters = new { Palabra = $"%{palabra}%"}; // ✅ Aquí se añade el %

            var pasajeros = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            List<Usuario> listaPasajeros = pasajeros.Select(row => new Usuario
            {
                Codigo = row.codcliente.ToString(),
                Codlan = row.codlan,
                Nombre = row.nombres,
                Apepate = row.apellidos,
                Lugar = new LugarCliente
                {
                    Codlugar = row.codlugar,
                    Direccion = row.direccion,
                    Distrito = row.distrito,
                    Wx = row.wx,
                    Wy = row.wy,
                    Zona = row.zona
                }
            }).ToList();

            return listaPasajeros;
        }

        public async Task<List<Servicio>> GetServicioPasajero(string usuario, string fec, string codcliente)
        {
            // Parsear la fecha
            DateTime fecF;
            if (!DateTime.TryParseExact(fec, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecF))
            {
                throw new FormatException("Formato de fecha incorrecto");
            }

            string fechaini = fecF.ToString("dd/MM/yyyy") + " 00:00";
            string fechafin = fecF.ToString("dd/MM/yyyy") + " 23:59";

            string sql = @"Select s.numeroguia, s.codconductor, s.codservicio, s.tipo, s.numero, s.fecha, s.grupo, s.unidad, s.fechainifin from servicio s, subservicio b where b.codservicio = s.codservicio and b.codcliente = @Codcliente and s.codusuario = @Usuario and s.fecha >= @Fechaini and s.fecha <= @Fechafin and s.estado<>'C' order by s.fecha";

            var parameters = new { Codcliente = codcliente, Usuario = usuario, Fechaini = fechaini, Fechafin = fechafin };

            var pasajeros = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            List<Servicio> listaPasajerosOb = pasajeros.Select(row => new Servicio
            {
                Codservicio = row.codservicio.ToString(),
                Fecfin = row.fechainifin,
                Numguia = row.numeroguia,
                Tipo = row.tipo,
                Numero = row.numero,
                Fecha = row.fecha,
                Grupo = row.grupo,
                Unidad = new Unidad
                {
                    Codunidad = row.unidad
                },
                Conductor = new Usuario
                {
                    Codigo = row.codconductor
                }

            }).ToList();

            // Obtener servicios móviles     
            List<Servicio> serviciosMovil = await ControlServiciosMovil(fechaini, fechafin, usuario);

            // Filtrar serviciosMovil dejando solo los que tienen un Codservicio válido
            var codigosValidos = listaPasajerosOb.Select(s => s.Codservicio).ToHashSet(); // HashSet mejora el rendimiento
            serviciosMovil = serviciosMovil.Where(s => codigosValidos.Contains(s.Codservicio)).ToList();

            var tareasConductores = serviciosMovil
            .Where(s => !string.IsNullOrEmpty(s.Conductor?.Codigo))
            .Select(async s =>
            {
                var detallesConductor = await GetDetalleConductor(s.Conductor?.Codigo);
                if (detallesConductor != null)
                {
                    s.Conductor = detallesConductor;
                }
            });

            await Task.WhenAll(tareasConductores);

            return serviciosMovil;
        }

        public async Task<string> AsignacionServicio(List<Servicio> listaServicios)
        {

            if (listaServicios == null || !listaServicios.Any())
            {
                return "Lista de servicios vacía o nula.";
            }

            string resultado = "";
            int resultadoServicio = 0;

            foreach (var servicio in listaServicios)
            {

                if (servicio.Unidad == null)
                {
                    return "Unidad no válida.";
                }

                if (string.IsNullOrEmpty(servicio.Unidad.Codunidad))
                {
                    return "Código de unidad no válido.";
                }

                Gps gps = await DatosCoordenadas(servicio.Unidad.Codunidad);

                if (gps != null)
                {
                    string fechaAsignacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                    servicio.Fecasignacion = fechaAsignacion;

                    resultadoServicio = await AsignarServicio(servicio);

                    var resultadoUnidad = await ActualizarUnidadConductorPreplan(servicio);
                }
                else
                {
                    resultado = "Unidad no encontrada o registrada.";
                }
            }

            if (resultadoServicio == 1)
            {
                resultado = "Servicio Asignado";
            }
            else
            {
                Console.WriteLine("Error en la asignación del servicio.");
            }

            return resultado;
        }

        private async Task<int> AsignarServicio(Servicio servicio)
        {
            string sql = @"Update servicio set unidad = @Unidad, codconductor = @Codconductor, fechainifin = @Fechainifin, fecasignacion = @Fecasignacion where codservicio = @Codservicio";

            var parameters = new
            {
                Unidad = servicio.Unidad?.Codunidad,
                Codconductor = servicio.Conductor?.Codigo,
                Fechainifin = servicio.Fecfin,
                Fecasignacion = servicio.Fecasignacion,
                Codservicio = servicio.Codservicio
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        private async Task<int> ActualizarUnidadConductorPreplan(Servicio servicio)
        {
            string sql = @"Update preplan set codunidad = @Codunidad, codconductor = @Codconductor where codservicio = @Codservicio";

            var parameters = new
            {
                Codunidad = servicio.Unidad?.Codunidad,
                Codconductor = servicio.Conductor?.Codigo,
                Codservicio = servicio.Codservicio
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        private async Task<Gps> DatosCoordenadas(string codunidad)
        {
            Console.WriteLine($"🔍 Buscando coordenadas para la unidad: {codunidad}");

            string sql = @"SELECT deviceID, statuscode, lastValidLongitude, rutaout, timerutaout, lastValidLatitude, lastValidSpeed, lastValidHeading, lastGPSTimestamp, direccion, codgeoact, timegeoact, uniprox, alarmacontrol, servicio, rutaact, panico, lastdespacho FROM device WHERE deviceID = @Codunidad";

            var parameters = new
            {
                Codunidad = codunidad
            };

            var row = await _defaultConnection.QueryFirstOrDefaultAsync(sql, parameters, transaction: _defaultTransaction);


            if (row == null)
            {
                Console.WriteLine("⚠️ No se encontraron datos en la BD para esta unidad.");

                return null;
            }

            Console.WriteLine($"Fila devuelta: {JsonConvert.SerializeObject(row)}");


            var unidad = new Gps
            {
                Numequipo = row.deviceID,
                Posx = row.lastValidLongitude != null ? Math.Round((double)row.lastValidLongitude, 5) : null,
                Posy = row.lastValidLatitude != null ? Math.Round((double)row.lastValidLatitude, 5) : null,
                Ubicacion = new Ubicacion
                {
                    Dircompleta = row.direccion ?? "Desconocido"
                },
                Velocidad = row.lastValidSpeed ?? 0,
                Fecha = row.lastGPSTimestamp != null ? row.lastGPSTimestamp.ToString() : "",
                Io = row.rutaout != null ? row.rutaout.ToString() : "0",
                Timerutaout = row.timerutaout ?? "",
                Direccion = row.lastValidHeading != null ? row.lastValidHeading.ToString() : "0",
                Geocerca = new Geocerca
                {
                    Codgeocerca = row.codgeoact ?? "N/A",
                    Tiempogeo = row.timegeoact != null ? row.timegeoact.ToString() : "0"
                },
                Uniprox = row.uniprox ?? "N/A",
                Alarmacontrol = row.alarmacontrol ?? "N/A",
                Evento = row.statuscode != null ? row.statuscode.ToString() : "0",
                Ultservicio = row.servicio != null ? new Servicio { Codservicio = row.servicio } : null,
                Rutaact = row.rutaact ?? "N/A",
                Botonpanico = row.panico ?? "N/A",
                Lastdespacho = row.lastdespacho != null ? row.lastdespacho.ToString() : "N/A"
            };

            Console.WriteLine($"✅ Objeto GPS creado: {unidad.Numequipo}");

            return unidad;
        }

        public async Task<int> EliminacionMultiple(List<Servicio> listaServicios)
        {
            int rs = 0;
            foreach (var servicio in listaServicios)
            {
                rs = await EliminarServicio(servicio);
            }
            return rs;
        }

        private async Task<int> EliminarServicio(Servicio servicio)
        {
            string sql = @"UPDATE servicio SET estado = 'C' WHERE codservicio = @Codservicio";

            var parameters = new
            {
                Codservicio = servicio.Codservicio
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        //public async Task<Unidad> PlaybackAsync(Unidad carro, string fechaini, string fechafin)
        //{
        //    bool gt = true;
        //    List<Gps> lista;
        //    string placa = carro.Gps?.Numequipo;

        //    if (string.IsNullOrEmpty(placa))
        //    {
        //        throw new ArgumentException("El número de equipo del GPS no puede ser nulo o vacío.");
        //    }

        //    int numero = await CambioHoraAsync(placa);

        //    DateTime fec1 = DateTime.ParseExact(fechaini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        //    DateTime fecFin = DateTime.ParseExact(fechafin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        //    // Convertir a timestamp UNIX correctamente (en segundos)
        //    long fec2 = new DateTimeOffset(fec1).ToUnixTimeSeconds();
        //    long fec3 = new DateTimeOffset(fecFin).ToUnixTimeSeconds();

        //    fechaini = fec2.ToString();
        //    fechafin = fec3.ToString();

        //    var listaDespachos = await ListaVueltasPorUnidadAsync(fechaini, fechafin, placa);

        //    carro.Listadespachos = listaDespachos;

        //    var listaHistoricos = await BaseHistoricosAsync();

        //    Historicos consulta = null;
        //    foreach (var u in listaHistoricos)
        //    {
        //        if (u.Timeini <= fec2 && fec2 <= u.Timefin)
        //        {
        //            consulta = u;
        //        }
        //    }
        //    consulta ??= new Historicos();

        //    if (consulta.Tabla == null)
        //    {
        //        lista = await RegistroFecha(placa, fechaini, fechafin);
        //    }
        //    else
        //    {
        //        lista = await RegistroFechaHist(consulta.Tabla, placa, fechaini, fechafin);
        //    }

        //    if (lista.Count > 0)
        //    {
        //        for (int x = 0; x < lista.Count; x++)
        //        {
        //            Gps gp = lista[x];

        //            if (!gt)
        //            {
        //                gp.Fecha = await ObtenerHoraActual(gp.Fecha, "gx", "");
        //            }
        //            else
        //            {
        //                gp.Fecha = ObtenerHoraActualRpt(gp.Fecha, "gt", numero);
        //            }

        //            gp.Fecform = FormatoFecha(gp.Fecha, "gx");
        //            gp.Horform = FormatoHora(gp.Fecha);

        //            lista[x] = gp;
        //        }
        //    }

        //    carro.Historico = lista;
        //    return carro;
        //}

        //private async Task<List<Gps>> RegistroFechaHist(string tabla, string placa, string fechaini, string fechafin)
        //{
        //    string sql = $@"SELECT deviceID, longitude, latitude, speedKPH, timestamp, heading, address FROM {tabla} WHERE deviceID = @Placa and timestamp BETWEEN @Fechaini AND @Fechafin and latitude<-2 and latitude>-18 and longitude<-70 and longitude>-82 ORDER BY timestamp";

        //    var parameters = new
        //    {
        //        Placa = placa,
        //        Fechaini = fechaini,
        //        Fechafin = fechafin
        //    };

        //    var row = await _secondConnection.QueryAsync(sql, parameters);

        //    if (row == null || !row.Any())
        //    {
        //        return new List<Gps>(); // Retorna lista vacía si no hay datos
        //    }

        //    var registros = row.Select(row => new Gps
        //    {
        //        Numequipo = row.deviceID,
        //        Posx = Math.Round(row.longitude, 5),
        //        Posy = Math.Round(row.latitude, 5),
        //        Velocidad = row.speedKPH,
        //        Fecha = row.timestamp.ToString(),
        //        Direccion = row.heading.ToString(),
        //        Ubicacion = new Ubicacion
        //        {
        //            Dircompleta = row.address
        //        }

        //    }).ToList();

        //    return registros;
        //}

        //private async Task<List<Gps>> RegistroFecha(string placa, string fechaini, string fechafin)
        //{
        //    string sql = @"Select deviceID, longitude, latitude, speedKPH, timestamp, heading, address FROM eventdata WHERE deviceID = @Placa and timestamp BETWEEN @Fechaini AND @Fechafin and latitude<-2 and latitude>-18 and longitude<-70 and longitude>-82 ORDER BY timestamp";

        //    var parameters = new
        //    {
        //        Placa = placa,
        //        Fechaini = fechaini,
        //        Fechafin = fechafin
        //    };

        //    var row = await _defaultConnection.QueryAsync(sql, parameters);

        //    if (row == null || !row.Any())
        //    {
        //        return new List<Gps>(); // Retorna lista vacía si no hay datos
        //    }

        //    var registros = row.Select(row => new Gps
        //    {
        //        Numequipo = row.deviceID,
        //        Posx = Math.Round(row.longitude, 5),
        //        Posy = Math.Round(row.latitude, 5),
        //        Velocidad = row.speedKPH,
        //        Fecha = row.timestamp.ToString(),
        //        Direccion = row.heading.ToString(),
        //        Ubicacion = new Ubicacion
        //        {
        //            Dircompleta = row.address
        //        }                

        //    }).ToList();

        //    return registros;
        //}

        //private async Task<List<Historicos>> BaseHistoricosAsync()
        //{
        //    string sql = @"Select tabla, timeini, timefin from historicos";

        //    var resultado = await _defaultConnection.QueryAsync(sql);

        //    var unidad = resultado.Select(row => new Historicos
        //    {
        //        Tabla = row.tabla,
        //        Timeini = row.timeini,
        //        Timefin = row.timefin

        //    }).ToList();

        //    return unidad;
        //}

        //private async Task<List<Despacho>> ListaVueltasPorUnidadAsync(string fechaini, string fechafin, string placa)
        //{
        //    string sql = @"SELECT a.deviceID, r.nombre as ruta, r.codigo as codruta, t.apellidos, a.codcobrador, a.eliminado, FROM_UNIXTIME(a.fechaini,'%H:%i') as fechainicio, FROM_UNIXTIME(a.fechafin,'%H:%i') as fechafin FROM urbano_asigna a, taxi t, rutaurbano r where a.codruta=r.codigo and a.codconductor = t.codtaxi and a.deviceid = @DeviceID and a.fechaini >= @Fechaini and fechafin <= @Fechafin order by a.fechaini";

        //    var parameters = new
        //    {
        //        Fechaini = fechaini,
        //        Fechafin = fechafin,
        //        DeviceID = placa
        //    };

        //    var row = await _defaultConnection.QueryAsync(sql, parameters);

        //    if (row == null || !row.Any())
        //    {
        //        return new List<Despacho>(); // Retorna lista vacía si no hay datos
        //    }

        //    var unidades = row.Select(row => new Despacho
        //    {
        //        Carro = new Unidad
        //        {
        //            Codunidad = row.deviceID
        //        },
        //        Fecini = row.fechainicio,
        //        Fecfin = row.fechafin,
        //        Ruta = new Ruta
        //        {
        //            Nombre = row.ruta,
        //            Codigo = row.codruta
        //        },
        //        Conductor = new Usuario
        //        {
        //            Nombre = row.apellidos
        //        },
        //        Cobrador = new Usuario
        //        {
        //            Codigo = row.codcobrador
        //        }

        //    }).ToList();

        //    return unidades;
        //}

        //private async Task<int> CambioHoraAsync(string placa)
        //{
        //    int rs = 28800000;

        //    string sql = @"Select DeviceId from cambiohora where DeviceId = @placa";

        //    var resultado = await _defaultConnection.QueryAsync(sql, new { placa });

        //    var unidad = resultado.Select(row => new Gps
        //    {
        //        Numequipo = row.DeviceId
        //    }).ToList();

        //    if (unidad.Any())
        //    {
        //        rs = 0;
        //    }

        //    return rs;
        //}

        //private string ObtenerHoraActualRpt(string fecha, string tipo, int difer)
        //{
        //    string? fecreg = null;
        //    if (tipo == "gx")
        //    {
        //        DateTime fec1;
        //        if (DateTime.TryParseExact(fecha, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out fec1))
        //        {
        //            fec1 = fec1.AddMilliseconds(-46800000);
        //            fecreg = fec1.ToString("dd/MM/yyyy HH:mm:ss");
        //        }
        //    }
        //    else if (tipo == "gt")
        //    {
        //        if (long.TryParse(fecha, out long timestamp))
        //        {
        //            DateTime lon = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.AddMilliseconds(-difer);
        //            fecreg = lon.ToString("dd/MM/yyyy HH:mm:ss");
        //        }
        //    }
        //    return fecreg ?? string.Empty;
        //}

        //private async Task<string> ObtenerHoraActual(string fecha, string tipo, string placa)
        //{
        //    string? fecreg = null;
        //    if (tipo == "gx")
        //    {
        //        DateTime fec1;
        //        if (DateTime.TryParseExact(fecha, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out fec1))
        //        {
        //            fec1 = fec1.AddMilliseconds(-46800000);
        //            fecreg = fec1.ToString("dd/MM/yyyy HH:mm:ss");
        //        }
        //    }
        //    else if (tipo == "gt")
        //    {
        //        int difer = await CambioHoraAsync(placa);
        //        if (long.TryParse(fecha, out long timestamp))
        //        {
        //            DateTime lon = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.AddMilliseconds(-difer);
        //            fecreg = lon.ToString("dd/MM/yyyy HH:mm:ss");
        //        }
        //    }
        //    return fecreg ?? string.Empty;
        //}

        //private string FormatoHora(string fecha)
        //{
        //    string[] fec2 = fecha.Split(' ');
        //    if (fec2.Length < 2) return string.Empty;
        //    string horaAct = fec2[1];
        //    string[] exHora = horaAct.Split(':');
        //    if (exHora.Length < 2) return string.Empty;
        //    string hora = exHora[0];
        //    string minutos = exHora[1];
        //    return hora + ":" + minutos;
        //}

        //private string? FormatoFecha(string fecha, string tipo)
        //{
        //    string[] fec1 = fecha.Split(' ');
        //    if (fec1.Length == 0) return string.Empty;
        //    string fec = fec1[0];
        //    string[]? exFecAc = null;
        //    if (tipo == "gx") exFecAc = fec.Split('/');
        //    if (tipo == "gt") exFecAc = fec.Split('-');
        //    if (exFecAc == null || exFecAc.Length < 3) return string.Empty;
        //    string dia = exFecAc[0];
        //    string mes = exFecAc[1];
        //    string ano = exFecAc[2];
        //    return dia + "/" + mes + "/" + ano;
        //}

        public async Task<List<Usuario>> GetConductorDetalle(string usuario)
        {
            List<Usuario> listaConductores = new List<Usuario>();

            var conductor = await GetDetalleConductor(usuario);

            if (conductor != null)
            {
                listaConductores.Add(conductor);
            }

            return listaConductores;
        }

        public async Task<List<Pedido>> ListaPasajeroServicio(string codservicio)
        {
            string sql = @"Select su.observacion, su.costo, su.codpedido, su.estado, su.fecha as fecpedido, su.vuelo, su.arealan, su.codcliente, su.fechafin as feclectura, su.feccancelpas, su.orden, c.apellidos, c.codlugar, l.wx, l.wy, l.direccion, l.distrito from subservicio su, cliente c, lugarcliente l where su.codcliente = c.codcliente and su.codubicli = l.codlugar and su.orden != '0' and su.estado != 'C' and su.codservicio = @Codservicio order by orden";

            var parameters = new
            {
                Codservicio = codservicio,
            };

            var row = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (row == null || !row.Any())
            {
                Console.WriteLine("No se encontraron resultados.");

                return new List<Pedido>(); // Retorna lista vacía si no hay datos
            }
            Console.WriteLine($"Registros obtenidos: {row.Count()}");

            var pasajeros = row.Select(row => new Pedido
            {
                Codigo = row.codpedido,
                Vuelo = row.vuelo,
                Arealan = row.arealan,
                Fecha = row.fecpedido,
                Estado = row.estado,
                Fechafin = row.feclectura,
                Orden = row.orden,
                Feccancelpas = row.feccancelpas,
                Codlugar = row.codlugar,
                Pasajero = new Usuario
                {
                    Codigo = row.codcliente,
                    Nombre = row.apellidos
                },
                Lugar = new LugarCliente
                {
                    Wx = row.wx,
                    Wy = row.wy,
                    Direccion = row.direccion,
                    Distrito = row.distrito
                },
                Tarifa = row.costo?.ToString(),
                Observacion = row.observacion

            }).ToList();

            return pasajeros;
        }

        public async Task<int> UpdateControlServicio(Servicio servicio)
        {
            Console.WriteLine($"[UpdateControlServicio] Iniciando actualización para servicio: {servicio.Codservicio}");
            int resultado = 0;

            if (servicio.Listapuntos != null)
            {
                Console.WriteLine($"[UpdateControlServicio] Procesando {servicio.Listapuntos.Count} puntos");
                List<Pedido> listaPasajeros = servicio.Listapuntos;

                foreach (var pedido in listaPasajeros)
                {
                    Console.WriteLine($"[UpdateControlServicio] Procesando pedido {pedido.Codigo} con estado: {pedido.Estado}");

                    // Insertar nuevos pasajeros
                    if (pedido.Estado == "NW")
                    {
                        Console.WriteLine($"[UpdateControlServicio] Insertando nuevo subservicio para pedido: {pedido.Codigo}");
                        resultado = await NuevoSubServicio(pedido);
                        Console.WriteLine($"[UpdateControlServicio] Resultado inserción: {resultado}");
                    }
                    // Actualizar orden
                    else
                    {
                        Console.WriteLine($"[UpdateControlServicio] Actualizando orden para pedido: {pedido.Codigo}");
                        resultado = await ActualizarOrden(pedido);
                        Console.WriteLine($"[UpdateControlServicio] Resultado actualización orden: {resultado}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[UpdateControlServicio] No hay puntos para procesar (Listapuntos es null)");
            }

            Console.WriteLine("[UpdateControlServicio] Modificando hora del servicio");
            resultado = await ModificarHoraServicio(servicio);
            Console.WriteLine($"[UpdateControlServicio] Finalizado con resultado: {resultado}");

            return resultado;
        }

        private async Task<int> ModificarHoraServicio(Servicio servicio)
        {
            Console.WriteLine($"[ModificarHoraServicio] Actualizando servicio {servicio.Codservicio}");
            Console.WriteLine($"[ModificarHoraServicio] Fecha: {servicio.Fecha}, CostoTotal: {servicio.Costototal}");

            string sql = "UPDATE servicio SET fecha = @Fecha, costototal = @Costototal WHERE codservicio = @Codservicio";
            var parameters = new
            {
                Fecha = servicio.Fecha,
                Costototal = servicio.Costototal,
                Codservicio = servicio.Codservicio
            };

            int resultado = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
            Console.WriteLine($"[ModificarHoraServicio] Filas afectadas: {resultado}");

            return resultado;
        }

        private async Task<int> ActualizarOrden(Pedido pedido)
        {
            Console.WriteLine($"[ActualizarOrden] Actualizando pedido {pedido.Codigo} con orden: {pedido.Orden}");

            string sql = "UPDATE subservicio SET orden = @Orden WHERE codpedido = @Codpedido";
            var parameters = new
            {
                Codpedido = pedido.Codigo,
                Orden = pedido.Orden
            };

            int resultado = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
            Console.WriteLine($"[ActualizarOrden] Filas afectadas: {resultado}");

            return resultado;
        }

        public async Task<int> CancelarAsignacion(string codservicio)
        {
            Console.WriteLine($"[CancelarAsignacion] Cancelando asignación para servicio: {codservicio}");

            string sql = "UPDATE servicio SET codconductor = NULL, unidad = NULL WHERE codservicio = @Codservicio";
            var parameters = new { Codservicio = codservicio };

            int resultado = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
            Console.WriteLine($"[CancelarAsignacion] Filas afectadas: {resultado}");

            return resultado;
        }

        public async Task<int> CancelarServicio(Servicio servicio)
        {
            List<Pedido> listaSubServicios = await ListaSubServicio(servicio.Codservicio);

            // Eliminar cada subservicio
            foreach (var pedido in listaSubServicios)
            {
                await EliminarSubServicio(pedido);
            }

            return await DeleteServicio(servicio.Codservicio);
        }

        private async Task<int> DeleteServicio(string codservicio)
        {
            string sql = "UPDATE servicio SET estado = 'C' WHERE codservicio = @Codservicio";

            var parameters = new { Codservicio = codservicio };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        private async Task<int> EliminarSubServicio(Pedido pedido)
        {
            string sql = "UPDATE subservicio SET estado = 'C' WHERE codpedido = @Codpedido";

            var parameters = new { Codpedido = pedido.Codigo };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }


        private async Task<List<Pedido>> ListaSubServicio(string codservicio)
        {
            string sql = @"Select codpedido from subservicio s where codservicio = @Codservicio and estado<>'C' order by fecha";

            var parameters = new
            {
                Codservicio = codservicio,
            };

            var row = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (row == null || !row.Any())
            {
                return new List<Pedido>();
            }

            var pasajeros = row.Select(row => new Pedido
            {
                Codigo = row.codpedido

            }).ToList();

            return pasajeros;
        }

        public async Task<string> NewServicio(Servicio servicio, string usuario)
        {
            string resultado = "";

            servicio.Usuario = usuario;
            servicio.Zona = new Zona();
            if (string.IsNullOrEmpty(servicio.Numero))
            {
                servicio.Numero = "0";
            }


            Servicio srv = await NuevoServicio(servicio, usuario);

            if (servicio.Listapuntos != null)
            {
                foreach (var pedido in servicio.Listapuntos)
                {
                    pedido.Servicio = srv;
                    await NuevoSubServicio(pedido);
                }
            }

            resultado = "Servicio agregado correctamente";

            // Obtener fecha actual en formato "dd/MM/yyyy 00:00" y "dd/MM/yyyy 23:59"
            string fecini = DateTime.Now.ToString("dd/MM/yyyy") + " 00:00";
            string fecfin = DateTime.Now.ToString("dd/MM/yyyy") + " 23:59";

            string uscode;
            var usuariosEspeciales = new List<string> { "dramirez", "mnazario", "pruebas", "joalbarracin", "wilbarrientos" };

            uscode = usuariosEspeciales.Contains(usuario) ? "movilbus" : usuario;

            await NumeracionMovil(fecini, fecfin, uscode);

            return resultado;
        }

        public async Task<int> UpdateEstadoServicio(Pedido pedido)
        {
            string fechareg = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            pedido.Feccancelpas = fechareg;

            //SIEMPRE obtener codservicio de la base de datos
            string codservicio = await ObtenerCodServicioPorPedido(pedido.Codigo);

            int rs = await EliminarPedido(pedido);

            // DECREMENTAR TOTALPAX SI LA CANCELACIÓN FUE EXITOSA
            if (rs > 0 && !string.IsNullOrEmpty(codservicio))
            {
                await DecrementarTotalPax(codservicio);
            }

            return rs;
        }

        private async Task<int> EliminarPedido(Pedido pedido)
        {
            string sql = "UPDATE subservicio SET estado = 'C', feccancelcentral = @Feccancelpas WHERE codpedido = @Codigo";
            var parameters = new
            {
                Feccancelpas = pedido.Feccancelpas,
                Codigo = pedido.Codigo
            };
            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        private async Task<string> ObtenerCodServicioPorPedido(int codpedido)
        {
            string sql = "SELECT codservicio FROM subservicio WHERE codpedido = @Codpedido";
            return await _doConnection.QueryFirstOrDefaultAsync<string>(sql,
                new { Codpedido = codpedido },
                transaction: _doTransaction);
        }

        private async Task<int> DecrementarTotalPax(string codservicio)
        {
            if (!int.TryParse(codservicio, out int codservicioInt))
            {
                throw new ArgumentException("El código de servicio no es válido. Debe ser un número entero.");
            }

            string sql = @"UPDATE servicio 
                   SET totalpax = CAST(CAST(totalpax AS UNSIGNED) - 1 AS CHAR) 
                   WHERE codservicio = @Codservicio 
                   AND CAST(totalpax AS UNSIGNED) > 0";

            return await _doConnection.ExecuteAsync(sql,
                new { Codservicio = codservicioInt },
                transaction: _doTransaction
            );
        }


        //REPORTE SOLICITADO GODOY
        public async Task<List<Pedido>> ReporteDiferencia(string fecini, string fecfin, string aerolinea, string usuario, string tipo)
        {
            var lista = await ListaServicios(fecini, fecfin, aerolinea, usuario, tipo);

            if (lista != null && lista.Count > 0)
            {
                Console.WriteLine($"[INFO] Lista obtenida con {lista.Count} elementos");

                string numact = "";
                int orden = 0;

                for (int x = 0; x < lista.Count; x++)
                {
                    var p = lista[x];
                    var s = p.Servicio;
                    Console.WriteLine($"[INFO] Procesando servicio {s.Numero}");
                    Console.WriteLine($"[DEBUG] Codconductor: '{p.Codconductor}'");

                    var conductor = s.Conductor ?? new Usuario();
                    var ca = s.Unidad;

                    s.Numero = s.Numeromovil;

                    if (!string.IsNullOrEmpty(p.Codconductor) && p.Codconductor != "0")
                    {
                        var conductorDetalle = await GetDetalleConductor(p.Codconductor);
                        if (conductorDetalle != null)
                        {
                            conductor = conductorDetalle;
                            Console.WriteLine($"[INFO] Conductor asignado: {conductor.Apepate}");
                        }
                        else
                        {
                            // GetDetalleConductor retornó null (no encontrado)
                            conductor.Apepate = "NO ASIGNADO";
                            Console.WriteLine($"[WARNING] Conductor no encontrado para código: {p.Codconductor}");
                        }
                    }

                    else
                    {
                        // codconductor es null, vacío, o "0"
                        conductor.Apepate = "NO ASIGNADO";
                        Console.WriteLine($"[INFO] Codconductor inválido o cero ({p.Codconductor ?? "null"}), asignando 'No Asignado'");
                    }

                    if (int.TryParse(s.Numero, out int nwn) && nwn > 10000 && s.Empresa == "AVIANCA")
                    {
                        s.Empresa = "ADM AVIANCA";
                    }

                    s.Conductor = conductor;

                    if (string.IsNullOrEmpty(ca?.Codunidad))
                    {
                        ca.Codunidad = "NO ASIGNADO";
                        s.Unidad = ca;
                    }

                    if (!string.IsNullOrEmpty(p.Fecha))
                    {
                        var fechapedido = p.Fecha.Split(' ');
                        p.Formathorarec = fechapedido[1];
                    }

                    if (!string.IsNullOrEmpty(p.Fecaten))
                    {
                        var fechapedidoaten = p.Fecaten.Split(' ');
                        p.Formatfecrec = fechapedidoaten[1];
                    }

                    if (!string.IsNullOrEmpty(s.Fecfin))
                    {
                        var fecfinserv = s.Fecfin.Split(' ');
                        s.Formathorarec = fecfinserv[1];
                    }

                    if (s.Tipo == "I") s.Tipo = "Recojo";
                    if (s.Tipo == "S") s.Tipo = "Reparto";

                    if (numact != s.Numero)
                    {
                        orden = 1;
                        numact = s.Numero;
                        p.Orden = orden.ToString();
                    }
                    else
                    {
                        orden += 1;
                        p.Orden = orden.ToString();
                    }

                    Console.WriteLine($"[INFO] Orden asignado: {p.Orden} para servicio {s.Numero}");
                    lista[x] = p;
                }
            }
            else
            {
                Console.WriteLine("[WARNING] Lista vacía o nula");
            }

            return lista;
        }

        private async Task<List<Pedido>> ListaServicios(string fecini, string fecfin, string aerolinea, string usuario, string tipo)
        {
            // 🔹 Paso 1: Obtener los codservicio válidos
            string subquery = @"SELECT a.codservicio FROM gts.servicio a WHERE STR_TO_DATE(a.fecha,'%d/%m/%Y %H:%i') >= STR_TO_DATE(@Fechaini,'%d/%m/%Y %H:%i') AND STR_TO_DATE(a.fecha,'%d/%m/%Y %H:%i') <= STR_TO_DATE(@Fechafin,'%d/%m/%Y %H:%i') AND a.codusuario = @Codusuario AND a.estado <> 'C' AND a.empresa = @Empresa AND tipo = @Tipo";

            var subqueryParams = new
            {
                Codusuario = usuario,
                Fechaini = fecini,
                Fechafin = fecfin,
                Empresa = aerolinea,
                Tipo = tipo
            };

            var codservicios = (await _doConnection.QueryAsync<int>(subquery, subqueryParams, transaction: _doTransaction)).ToList();

            if (!codservicios.Any())
            {
                return new List<Pedido>();
            }

            // 🔹 Paso 2: Ejecutar la consulta principal usando los codservicio obtenidos
            string sqlQuery = $@"
        SELECT 
            su.codpedido as codpedido, 
            s.fecha as fechaservicio, 
            s.fechafin as fecfinservicio, 
            s.empresa, 
            s.tipo, 
            su.fecha, 
            su.fechafin as fechaaten, 
            c.apellidos,  
            l.direccion, 
            l.distrito, 
            s.unidad, 
            s.codconductor,
            s.numeromovil
        FROM 
            gts.subservicio su, 
            gts.cliente c, 
            gts.servicio s, 
            gts.lugarcliente l 
        WHERE 
            su.estado <> 'C' AND 
            su.codcliente = c.codcliente AND 
            su.codservicio = s.codservicio AND 
            su.codubicli = l.codlugar AND 
            c.codlan <> '4175' AND 
            su.codservicio IN @Codservicios AND su.orden <> '0'
        ORDER BY 
            codpedido";

            var mainQueryParams = new { Codservicios = codservicios };

            var results = (await _doConnection.QueryAsync(sqlQuery, mainQueryParams, transaction: _doTransaction)).ToList();

            if (results.Count == 0)
            {
                return new List<Pedido>();
            }

            // 🔹 Mapeo de resultados
            var resumen = results.Select((row, index) => new Pedido
            {
                Id = index + 1,
                Codigo = row.codpedido,
                Codconductor = row.codconductor,
                Servicio = new Servicio
                {
                    Numero = row.numero,
                    Fecha = row.fechaservicio,
                    Fecfin = row.fecfinservicio,
                    Empresa = row.empresa,
                    Tipo = row.tipo,
                    Numeromovil = row.numeromovil,
                    Unidad = new Unidad { Codunidad = row.unidad },
                    Conductor = new Usuario { Codigo = row.codconductor }
                },
                Pasajero = new Usuario
                {
                    Nombre = row.apellidos,
                },
                Fecaten = row.fechaaten,
                Fecha = row.fecha,
                Lugar = new LugarCliente
                {
                    Direccion = row.direccion,
                    Distrito = row.distrito
                }
            }).ToList();

            return resumen;
        }


        //REPORTE EXCEL Control servicios
        public async Task<List<Pedido>> ReporteFormatoAvianca(string fecini, string fecfin, string aerolinea, string usuario)
        {
            string usu = usuario == "jperiche" ? "movilbus" : usuario;
            Console.WriteLine($"[INFO] Usuario asignado: {usu}");

            var lista = await ListaServFrmtAvianca(fecini, fecfin, aerolinea, usu);

            if (lista != null && lista.Count > 0)
            {
                Console.WriteLine($"[INFO] Lista obtenida con {lista.Count} elementos");

                string numact = "";
                int orden = 0;

                for (int x = 0; x < lista.Count; x++)
                {
                    var p = lista[x];
                    var s = p.Servicio;
                    Console.WriteLine($"[INFO] Procesando servicio {s.Numero}");
                    Console.WriteLine($"[DEBUG] Codconductor: '{p.Codconductor}'");

                    var conductor = s.Conductor ?? new Usuario();
                    var ca = s.Unidad;

                    if (usu == "movilbus")
                    {
                        s.Numero = s.Numeromovil;
                    }

                    if (p.Estado == "P") p.Estado = "PENDIENTE";
                    if (p.Estado == "A") p.Estado = "ATENDIDO";

                    if (!string.IsNullOrEmpty(p.Codconductor) && p.Codconductor != "0")
                    {
                        var conductorDetalle = await GetDetalleConductor(p.Codconductor);
                        if (conductorDetalle != null)
                        {
                            conductor = conductorDetalle;
                            Console.WriteLine($"[INFO] Conductor asignado: {conductor.Apepate}");
                        }
                        else
                        {
                            // GetDetalleConductor retornó null (no encontrado)
                            conductor.Apepate = "No Asignado";
                            Console.WriteLine($"[WARNING] Conductor no encontrado para código: {p.Codconductor}");
                        }
                    }

                    else
                    {
                        // codconductor es null, vacío, o "0"
                        conductor.Apepate = "No Asignado";
                        Console.WriteLine($"[INFO] Codconductor inválido o cero ({p.Codconductor ?? "null"}), asignando 'No Asignado'");
                    }

                    if (int.TryParse(s.Numero, out int nwn) && nwn > 10000 && s.Empresa == "AVIANCA")
                    {
                        s.Empresa = "ADM AVIANCA";
                    }

                    if (!string.IsNullOrEmpty(s.Zona?.Codigo))
                    {
                        Zona z = await DetalleZona(s.Zona);
                        s.Zona = z;
                    }

                    s.Conductor = conductor;

                    if (string.IsNullOrEmpty(ca?.Codunidad))
                    {
                        ca.Codunidad = "No Asignado";
                        s.Unidad = ca;
                    }

                    if (!string.IsNullOrEmpty(p.Fecha))
                    {
                        var fechapedido = p.Fecha.Split(' ');
                        p.Formathorarec = fechapedido[1];
                    }

                    if (!string.IsNullOrEmpty(p.Fecaten))
                    {
                        var fechapedidoaten = p.Fecaten.Split(' ');
                        p.Formatfecrec = fechapedidoaten[1];
                    }

                    if (!string.IsNullOrEmpty(s.Fecfin))
                    {
                        var fecfinserv = s.Fecfin.Split(' ');
                        s.Formathorarec = fecfinserv[1];
                    }

                    if (s.Tipo == "I") s.Tipo = "Recojo";
                    if (s.Tipo == "S") s.Tipo = "Reparto";

                    if (numact != s.Numero)
                    {
                        orden = 1;
                        numact = s.Numero;
                        p.Orden = orden.ToString();
                    }
                    else
                    {
                        orden += 1;
                        p.Orden = orden.ToString();
                    }

                    Console.WriteLine($"[INFO] Orden asignado: {p.Orden} para servicio {s.Numero}");
                    lista[x] = p;
                }
            }
            else
            {
                Console.WriteLine("[WARNING] Lista vacía o nula");
            }

            return lista;
        }

        private async Task<Zona> DetalleZona(Zona zona)
        {
            if (!int.TryParse(zona.Codigo, out int code))
            {
                return null; // Retornamos null si la conversión falla
            }

            string sql = "select * from zonificacion where codigo = @Codigo";

            var parameters = new
            {
                Codigo = code
            };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!results.Any())
            {
                return null;
            }

            var row = results.First();

            var zonificacion = new Zona
            {
                Codigo = row.codigo.ToString(),
                Precio = row.precio,
                Numerozona = row.zona,
                Descripcion = row.descripcion
            };

            return zonificacion;
        }

        private async Task<List<Pedido>> ListaServFrmtAvianca(string fecini, string fecfin, string aerolinea, string usuario)
        {
            // 🔹 Paso 1: Obtener los codservicio válidos
            string subquery = @"
        SELECT a.codservicio 
        FROM gts.servicio a 
        WHERE 
            STR_TO_DATE(a.fecha,'%d/%m/%Y %H:%i') >= STR_TO_DATE(@Fechaini,'%d/%m/%Y %H:%i') AND 
            STR_TO_DATE(a.fecha,'%d/%m/%Y %H:%i') <= STR_TO_DATE(@Fechafin,'%d/%m/%Y %H:%i') AND 
            a.codusuario = @Codusuario AND 
            a.estado <> 'C' AND 
            a.empresa = @Empresa";

            var subqueryParams = new
            {
                Codusuario = usuario,
                Fechaini = fecini,
                Fechafin = fecfin,
                Empresa = aerolinea
            };

            var codservicios = (await _doConnection.QueryAsync<int>(subquery, subqueryParams, transaction: _doTransaction)).ToList();

            if (!codservicios.Any())
            {
                return new List<Pedido>();
            }

            // 🔹 Paso 2: Ejecutar la consulta principal usando los codservicio obtenidos
            string sqlQuery = $@"
        SELECT 
            su.codpedido as codpedido, 
            s.fecha as fechaservicio, 
            s.fechafin as fecfinservicio, 
            s.empresa, 
            c.area, 
            s.tipo, 
            s.numero, 
            FROM_UNIXTIME(s.fechainifin,'%H:%i') as fecgeo, 
            su.fecha, 
            su.fechafin as fechaaten, 
            c.codlan, 
            c.apellidos, 
            c.telefono, 
            c.cargo, 
            c.cuenta, 
            l.direccion, 
            l.distrito, 
            su.estado, 
            s.unidad, 
            s.codconductor, 
            s.codzona, 
            s.totalpax, 
            s.codservicio, 
            s.numeromovil, 
            su.centrocosto 
        FROM 
            gts.subservicio su, 
            gts.cliente c, 
            gts.servicio s, 
            gts.lugarcliente l 
        WHERE 
            su.estado <> 'C' AND 
            su.codcliente = c.codcliente AND 
            su.codservicio = s.codservicio AND 
            su.codubicli = l.codlugar AND 
            c.codlan <> '4175' AND 
            su.codservicio IN @Codservicios AND su.orden <> '0'
        ORDER BY 
            codpedido";

            var mainQueryParams = new { Codservicios = codservicios };

            var results = (await _doConnection.QueryAsync(sqlQuery, mainQueryParams, transaction: _doTransaction)).ToList();

            if (results.Count == 0)
            {
                return new List<Pedido>();
            }

            // 🔹 Mapeo de resultados
            var resumen = results.Select((row, index) => new Pedido
            {
                Id = index + 1,
                Codigo = row.codpedido,
                Codconductor = row.codconductor,
                Estado = row.estado,
                Servicio = new Servicio
                {
                    Numero = row.numero,
                    Fecha = row.fechaservicio,
                    Fecfin = row.fecfinservicio,
                    Empresa = row.empresa,
                    Tipo = row.tipo,
                    Numeromovil = row.numeromovil,
                    Numpax = row.totalpax,
                    Codservicio = row.codservicio.ToString(),
                    Gps = new Gps
                    {
                        Fecha = string.IsNullOrEmpty(row.fecgeo) ? "NO ATENDIDO" : row.fecgeo
                    },
                    Estado = string.IsNullOrEmpty(row.fecgeo) ? "P" : "A",
                    Zona = new Zona { Codigo = row.codzona },
                    Unidad = new Unidad { Codunidad = row.unidad },
                    Conductor = new Usuario { Codigo = row.codconductor }
                },
                Pasajero = new Usuario
                {
                    Codlan = row.codlan,
                    Nombre = row.apellidos,
                    Telefono = row.telefono
                },
                Arealan = row.area,
                Cargo = row.cargo,
                Cuenta = row.cuenta,
                Fecaten = row.fechaaten,
                Fecha = row.fecha,
                Lugar = new LugarCliente
                {
                    Direccion = row.direccion,
                    Distrito = row.distrito
                },
                Centrocosto = row.centrocosto
            }).ToList();

            return resumen;
        }


        //EXCEL PARA AREMYS
        public async Task<List<Pedido>> ReporteFormatoAremys(string fecini, string fecfin, string aerolinea)
        {
            string usu = "aremys";

            var lista = await ListaServFrmtAremys(fecini, fecfin, aerolinea, usu);

            if (lista != null && lista.Count > 0)
            {
                Console.WriteLine($"[INFO] Lista obtenida con {lista.Count} elementos");

                string numact = "";
                int orden = 0;

                for (int x = 0; x < lista.Count; x++)
                {
                    var p = lista[x];
                    var s = p.Servicio;
                    Console.WriteLine($"[INFO] Procesando servicio {s.Numero}");
                    Console.WriteLine($"[DEBUG] Codconductor: '{p.Codconductor}'");

                    var conductor = s.Conductor;
                    var ca = s.Unidad;

                    if (!string.IsNullOrEmpty(p.Codconductor))
                    {
                        conductor = await GetDetalleConductor(p.Codconductor);
                        Console.WriteLine($"[INFO] Conductor asignado: {conductor.Apepate}");
                    }
                    else
                    {
                        conductor.Apepate = "No Asignado";
                    }

                    s.Conductor = conductor;

                    if (string.IsNullOrEmpty(ca?.Codunidad))
                    {
                        ca.Codunidad = "No Asignado";
                        s.Unidad = ca;
                    }

                    if (s.Tipo == "I") s.Tipo = "INGRESO";
                    if (s.Tipo == "S") s.Tipo = "SALIDA";

                    lista[x] = p;
                }
            }
            else
            {
                Console.WriteLine("[WARNING] Lista vacía o nula");
            }

            return lista;
        }

        private async Task<List<Pedido>> ListaServFrmtAremys(string fecini, string fecfin, string aerolinea, string usuario)
        {
            // 🔹 Paso 1: Obtener los codservicio válidos
            string subquery = @"
        SELECT a.codservicio 
        FROM gts.servicio a 
        WHERE 
            STR_TO_DATE(a.fecha,'%d/%m/%Y %H:%i') >= STR_TO_DATE(@Fechaini,'%d/%m/%Y %H:%i') AND 
            STR_TO_DATE(a.fecha,'%d/%m/%Y %H:%i') <= STR_TO_DATE(@Fechafin,'%d/%m/%Y %H:%i') AND 
            a.codusuario = @Codusuario AND 
            a.estado <> 'C' AND 
            a.empresa = @Empresa";

            var subqueryParams = new
            {
                Codusuario = usuario,
                Fechaini = fecini,
                Fechafin = fecfin,
                Empresa = aerolinea
            };

            var codservicios = (await _doConnection.QueryAsync<int>(subquery, subqueryParams, transaction: _doTransaction)).ToList();

            if (!codservicios.Any())
            {
                return new List<Pedido>();
            }

            // 🔹 Paso 2: Ejecutar la consulta principal usando los codservicio obtenidos
            string sqlQuery = $@"
        SELECT 
            su.codpedido as codpedido, 
            s.fecplan as horaprogramada,
            s.fecha as horallegada, 
            s.fechafin as horapvI,
            s.fechaini as horapvS, 
            s.empresa, 
            s.tipo, 
            su.fecha as horasky, 
            su.fechafin as horainicio, 
            su.orden,
            c.codlan, 
            c.apellidos, 
            c.telefono,
            l.direccion, 
            l.distrito, 
            s.unidad, 
            s.codconductor, 
            s.codservicio
        FROM 
            gts.subservicio su, 
            gts.cliente c, 
            gts.servicio s, 
            gts.lugarcliente l 
        WHERE
            su.codcliente = c.codcliente AND 
            su.codservicio = s.codservicio AND 
            su.codubicli = l.codlugar AND 
            c.codlan <> '4175' AND 
            su.codservicio IN @Codservicios
        ORDER BY 
            codpedido";

            var mainQueryParams = new { Codservicios = codservicios };

            var results = (await _doConnection.QueryAsync(sqlQuery, mainQueryParams, transaction: _doTransaction)).ToList();

            if (results.Count == 0)
            {
                return new List<Pedido>();
            }

            // 🔹 Mapeo de resultados
            var resumen = results.Select((row, index) => new Pedido
            {
                Id = index + 1,
                Codigo = row.codpedido,
                Fecplan = row.horaprogramada,
                Codconductor = row.codconductor,
                Orden = row.orden,
                Servicio = new Servicio
                {
                    Fecha = row.horallegada,
                    Fecfin = row.tipo == "I" ? row.horapvI : row.horapvS,
                    Empresa = row.empresa,
                    Tipo = row.tipo,
                    Codservicio = row.codservicio.ToString(),
                    Unidad = new Unidad { Codunidad = row.unidad },
                    Conductor = new Usuario { Codigo = row.codconductor }
                },
                Pasajero = new Usuario
                {
                    Codlan = row.codlan,
                    Nombre = row.apellidos,
                    Telefono = row.telefono
                },
                Fecaten = row.horainicio,
                Fecha = row.horasky,
                Lugar = new LugarCliente
                {
                    Direccion = row.direccion,
                    Distrito = row.distrito
                }
            }).ToList();

            return resumen;
        }
        //FIN EXCEL AREMYS


        public async Task<int> RegistrarPasajeroGrupo(Pedido pedido, string usuario)
        {
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");

            pedido.Usuario = usuario;
            pedido.Fecreg = fechaActual;

            return await RegistroPreplandos(pedido);
        }

        private async Task<int> RegistroPreplandos(Pedido pedido)
        {
            var sql = @"INSERT INTO preplan (codcliente, nombre, rol, fecha, horaprog, tipo, usuario, area, distancia, fecreg, lastorden, lastnumero, empresa, arealatam, destinocodlugar, numero, orden, replannumero, replanorden) VALUES (@Codcliente, @Nombre, @Rol, @Fecha, @Horaprog, @Tipo, @Usuario, @Area, @Distancia, @Fecreg, @Lastorden, @Lastnumero, @Empresa, @Arealatam, @Destinocodigo, @Numero, @Orden, @Replannumero, @Replanorden)";

            var parameters = new
            {
                Codcliente = pedido.Pasajero?.Codlan,
                Nombre = pedido.Pasajero?.Nombre,
                Rol = pedido.Rol,
                Fecha = pedido.Fecha,
                Horaprog = pedido.Horaprog,
                Tipo = pedido.Tipo,
                Usuario = pedido.Usuario,
                Area = pedido.Arealan,
                Distancia = pedido.Distancia,
                Fecreg = pedido.Fecreg,
                Lastorden = pedido.Lastorden,
                Lastnumero = pedido.Lastnumero,
                Empresa = pedido.Empresa,
                Arealatam = pedido.Arealatam,
                Destinocodigo = pedido.Destinocodlugar,
                Numero = pedido.Numero,
                Orden = pedido.Orden,
                Replannumero = pedido.Numero,
                Replanorden = pedido.Orden
            };

            var result = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
            return result;
        }

        public async Task<int> UpdateHorasServicio(string codservicio, string fecha, string fecplan)
        {
            string sql = "UPDATE servicio SET fecha = @Fecha, fecplan = @Fecplan WHERE codservicio = @Codservicio";
            string sqlSubservicio = "UPDATE subservicio SET fecha = @Fecplan WHERE codservicio = @Codservicio";


            var parameters = new { Codservicio = codservicio, Fecha = fecha, Fecplan = fecplan };

            var result = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
            var resultSubservicio = await _doConnection.ExecuteAsync(sqlSubservicio, parameters, transaction: _doTransaction);

            return result + resultSubservicio;
        }

        public async Task<int> UpdateDestinoServicio(string codservicio, string newcoddestino, string newcodubicli)
        {
            string sql1 = "UPDATE servicio SET destino = @Newcoddestino WHERE codservicio = @Codservicio";
            var parameters1 = new { Codservicio = int.Parse(codservicio), Newcoddestino = newcoddestino };
            var result1 = await _doConnection.ExecuteAsync(sql1, parameters1, transaction: _doTransaction);

            string sql2 = "UPDATE subservicio SET codcliente = @Newcoddestino, codubicli = @Newcodubicli WHERE codservicio = @Codservicio and orden = '0'";
            var parameters2 = new { Codservicio = codservicio, Newcoddestino = newcoddestino, Newcodubicli = newcodubicli };
            var result2 = await _doConnection.ExecuteAsync(sql2, parameters2, transaction: _doTransaction);

            return result1 + result2;
        }

        public async Task<List<Usuario>> GetDestinos(string palabra)
        {
            string sql = @"Select l.codlugar, c.codcliente, c.nombres, c.apellidos, c.codlan, l.wy, l.wx, l.direccion, l.distrito from cliente c, lugarcliente l where l.codcli=c.codlugar and c.estadocuenta='A' and l.estado='A' and apellidos like @Palabra and destino='1' LIMIT 10";

            var parameters = new { Palabra = $"%{palabra}%" }; // ✅ Aquí se añade el %

            var pasajeros = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            List<Usuario> listaPasajeros = pasajeros.Select(row => new Usuario
            {
                Codigo = row.codcliente.ToString(),
                Codlan = row.codlan,
                Nombre = row.nombres,
                Apepate = row.apellidos,
                Lugar = new LugarCliente
                {
                    Codlugar = row.codlugar,
                    Direccion = row.direccion,
                    Distrito = row.distrito,
                    Wx = row.wx,
                    Wy = row.wy,
                }
            }).ToList();

            return listaPasajeros;
        }

        public async Task<int> EliminarGrupoCero(string usuario)
        {
            string sql = "Update preplan set cerrado = '1' where eliminado = '1' and usuario = @Usuario";

            var parameters = new { Usuario = usuario };

            var result = await _doConnection.ExecuteAsync(sql, parameters, _doTransaction);

            return result;
        }

        public async Task<List<Conductor>> GetConductoresxUsuario(string usuario)
        {
            var lista = new List<Conductor>();

            if (usuario == "dramirez" || usuario == "mnazario" || usuario == "pruebas")
            {
                // Obtener conductores de "movilbus"
                lista = await ListaConductorAsync("movilbus");

                // Obtener conductores de "dramirez"
                var listaduno = await ListaConductorAsync("dramirez");

                // Obtener conductores de "mnazario"
                var listaddos = await ListaConductorAsync("mnazario");

                // Agregar las listas adicionales si tienen elementos
                if (listaduno?.Any() == true)
                {
                    lista.AddRange(listaduno);
                }
                if (listaddos?.Any() == true)
                {
                    lista.AddRange(listaddos);
                }
            }
            else
            {
                lista = await ListaConductorAsync(usuario);
            }

            return lista;
        }

        private async Task<List<Conductor>> ListaConductorAsync(string codusuario)
        {
            string sql = @"SELECT codtaxi as Codigo, 
                  nombres as Nombres, 
                  apellidos as Apellidos, 
                  login as Login, 
                  clave as Clave, 
                  telefono as Telefono, 
                  dni as Dni, 
                  email as Email, 
                  brevete as Brevete, 
                  sctr as Sctr, 
                  direccion as Direccion, 
                  imagen as Imagen, 
                  catbrevete as CatBrevete, 
                  fecvalidbrevete as FecValidBrevete, 
                  estbrevete as EstBrevete, 
                  sexo as Sexo, 
                  habilitado as Habilitado 
           FROM taxi 
           WHERE codusuario = @Codusuario AND estado = 'A' 
           ORDER BY habilitado DESC, apellidos";

            var parameters = new { Codusuario = codusuario };
            var conductores = await _doConnection.QueryAsync<Conductor>(sql, parameters, transaction: _doTransaction);

            return conductores.ToList();
        }

        public async Task<List<Carro>> GetUnidadesxUsuario(string usuario)
        {
            var totalCarros = new List<Carro>();

            // Primera consulta - Tabla device
            var carrosDevice = await GetCarrosFromDeviceAsync(usuario);
            if (carrosDevice?.Any() == true)
            {
                totalCarros.AddRange(carrosDevice);
            }

            // Segunda consulta - Tabla DeviceUser
            var carrosDeviceUser = await GetCarrosFromDeviceUserAsync(usuario);
            if (carrosDeviceUser?.Any() == true)
            {
                totalCarros.AddRange(carrosDeviceUser);
            }

            // Ordenar por codunidad
            totalCarros = totalCarros.OrderBy(c => c.Codunidad).ToList();

            return totalCarros;
        }

        private async Task<List<Carro>> GetCarrosFromDeviceAsync(string accountId)
        {
            string sql = @"SELECT deviceID, habilitada, rutadefault FROM device WHERE accountID = @AccountId ORDER BY deviceID";

            var parameters = new { AccountId = accountId };
            var resultado = await _defaultConnection.QueryAsync(sql, parameters, transaction: _defaultTransaction);

            var carros = new List<Carro>();

            foreach (var row in resultado)
            {
                var carro = new Carro
                {
                    Codunidad = row.deviceID?.ToString()?.ToUpper(),
                    Tipo = "2",
                    Habilitado = row.habilitada?.ToString(),
                    RutaDefault = row.rutadefault?.ToString()
                };

                carros.Add(carro);
            }

            return carros;
        }

        private async Task<List<Carro>> GetCarrosFromDeviceUserAsync(string userId)
        {
            string sql = @"SELECT DeviceName, DeviceID FROM DeviceUser WHERE UserID = @UserId AND Status = '1' ORDER BY DeviceName";

            var parameters = new { UserId = userId };
            var resultado = await _defaultConnection.QueryAsync(sql, parameters, transaction: _defaultTransaction);

            var carros = new List<Carro>();

            foreach (var row in resultado)
            {
                var carro = new Carro
                {
                    Codunidad = row.DeviceName?.ToString()?.ToUpper(),
                    Tipo = "2",
                };

                carros.Add(carro);
            }

            return carros;
        }

        public async Task<int> GuardarConductorAsync(Conductor conductor, string usuario)
        {
            int rs = 0;

            // Verificar si usuario existe en totalserver
            if (usuario != "realstar")
            {
                var usuarioExistente = await BuscarTotalServerAsync(conductor);
                if (usuarioExistente == null)
                {
                    rs = await NuevoConductorAsync(conductor, usuario);
                    if (rs == 1 && !string.IsNullOrWhiteSpace(conductor.UnidadActual))
                    {
                        // Asignar el conductor a la unidad
                        var ultimoConductor = await UltimoRegistroConductorAsync(usuario);
                        if (ultimoConductor != null)
                        {
                            rs = await AsignarConductorUnidadAsync(conductor.UnidadActual, ultimoConductor.Codigo);
                        }
                    }
                }
                else
                {
                    rs = 2; // Usuario ya existe
                }
            }
            else
            {
                rs = await NuevoConductorAsync(conductor, usuario);
            }

            return rs;
        }

        private async Task<Usuario> BuscarTotalServerAsync(Conductor conductor)
        {
            string sql = "SELECT loginusu FROM serverprueba WHERE loginusu = @Login";
            var parameters = new { Login = conductor.Login };

            var result = await _defaultConnection.QueryFirstOrDefaultAsync<string>(sql, parameters, transaction: _defaultTransaction);

            if (result != null)
            {
                return new Usuario { Login = result };
            }

            return null;
        }

        private async Task<int> NuevoConductorAsync(Conductor conductor, string usuario)
        {
            string sqlTaxi = @"INSERT INTO taxi (nombres, apellidos, login, clave, estado, codusuario, telefono, email, brevete, dni, direccion, sctr, catbrevete, estbrevete, fecvalidbrevete) 
                      VALUES (@Nombres, @Apellidos, @Login, @Clave, 'A', @Codusuario, @Telefono, @Email, @Brevete, @Dni, @Direccion, @Sctr, @Catbrevete, @Estbrevete, @Fecvalidbrevete)";

            var parametersEdriver = new
            {
                Nombres = conductor.Nombres,
                Apellidos = conductor.Apellidos,
                Login = conductor.Login,
                Clave = conductor.Clave,
                Codusuario = usuario,
                Telefono = conductor.Telefono,
                Email = conductor.Email,
                Brevete = conductor.Brevete,
                Dni = conductor.Dni,
                Direccion = conductor.Direccion,
                Sctr = conductor.Sctr,
                Catbrevete = conductor.CatBrevete,
                Estbrevete = conductor.EstBrevete,
                Fecvalidbrevete = conductor.FecValidBrevete
            };

            // Insertar en tabla taxi
            await _doConnection.ExecuteAsync(sqlTaxi, parametersEdriver, transaction: _doTransaction);

            string sqlTotalServer = @"INSERT INTO servermobile (loginusu, servidor, tipo) VALUES (@Login, 'https://do.velsat.pe:2053','c')";

            var parametersTotalServer = new
            {
                Login = conductor.Login
            };

            // Insertar en totalserver
            int filasAfectadas = await _defaultConnection.ExecuteAsync(sqlTotalServer, parametersTotalServer, transaction: _defaultTransaction);

            return filasAfectadas;
        }

        private async Task<Usuario> UltimoRegistroConductorAsync(string codusuario)
        {
            string sql = @"SELECT codtaxi FROM taxi WHERE codusuario = @Codusuario ORDER BY codtaxi DESC LIMIT 1";

            var parameters = new { Codusuario = codusuario };
            var codigo = await _doConnection.QueryFirstOrDefaultAsync<string>(sql, parameters, transaction: _doTransaction);

            if (codigo != null)
            {
                return new Usuario { Codigo = codigo };
            }

            return null;
        }

        private async Task<int> AsignarConductorUnidadAsync(string unidad, string codconductor)
        {
            string sql = "UPDATE device SET codconductoract = @Codconductor WHERE deviceid = @Unidad";
            var parameters = new
            {
                Codconductor = codconductor,
                Unidad = unidad
            };

            return await _defaultConnection.ExecuteAsync(sql, parameters, transaction: _defaultTransaction);
        }

        public async Task<int> ModificarConductorAsync(Conductor conductor)
        {
            // ✅ Validar que el conductor no sea null
            if (conductor == null)
                throw new ArgumentNullException(nameof(conductor), "El conductor no puede ser null");

            // ✅ Validar que tenga un código válido
            if (conductor.Codigo <= 0)
                throw new ArgumentException("El código del conductor es inválido", nameof(conductor.Codigo));

            // Primera actualización en la tabla taxi
            string sql = @"
UPDATE taxi 
SET nombres = @Nombres, 
    apellidos = @Apellidos, 
    login = @Login, 
    clave = @Clave, 
    telefono = @Telefono, 
    email = @Email, 
    brevete = @Brevete, 
    dni = @Dni, 
    direccion = @Direccion, 
    sctr = @Sctr, 
    catbrevete = @CatBrevete, 
    estbrevete = @EstBrevete, 
    fecvalidbrevete = @FecValidBrevete 
WHERE codtaxi = @Codigo";

            var parameters = new
            {
                Nombres = conductor.Nombres,
                Apellidos = conductor.Apellidos,
                Login = conductor.Login,
                Clave = conductor.Clave,
                Telefono = conductor.Telefono,
                Email = conductor.Email,
                Brevete = conductor.Brevete,
                Dni = conductor.Dni,
                Direccion = conductor.Direccion,
                Sctr = conductor.Sctr,
                CatBrevete = conductor.CatBrevete,
                EstBrevete = conductor.EstBrevete,
                FecValidBrevete = conductor.FecValidBrevete,
                Codigo = conductor.Codigo
            };

            // Ejecutar primera actualización
            int rowsAffected = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);

            // ✅ Buscar usuario existente
            var usuarioExistente = await BuscarTotalServerAsync(conductor);

            // ✅ Si existe, actualizar. Si no existe, crear
            if (usuarioExistente != null && !string.IsNullOrEmpty(usuarioExistente.Login))
            {
                // 📝 ACTUALIZAR registro existente
                string sql2 = @"UPDATE servermobile SET loginusu = @Login WHERE loginusu = @Loginusu";

                var parameters2 = new
                {
                    Login = conductor.Login,
                    Loginusu = usuarioExistente.Login
                };

                int rowsAffected2 = await _defaultConnection.ExecuteAsync(sql2, parameters2, transaction: _defaultTransaction);
                rowsAffected += rowsAffected2;
            }
            else
            {
                // ➕ CREAR nuevo registro
                string sqlInsert = @"INSERT INTO servermobile (loginusu, servidor, tipo) VALUES (@Login, @Servidor, @Tipo)";

                var parametersInsert = new
                {
                    Login = conductor.Login,
                    Servidor = "https://do.velsat.pe:2053",
                    Tipo = "c"
                };

                int rowsInserted = await _defaultConnection.ExecuteAsync(sqlInsert, parametersInsert, transaction: _defaultTransaction);
                rowsAffected += rowsInserted;
            }

            return rowsAffected;
        }

        public async Task<int> HabilitarConductorAsync(int codigoConductor)
        {
            string sql = "UPDATE taxi SET habilitado = '1' WHERE codtaxi = @Codigo";
            var parameters = new { Codigo = codigoConductor };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> DeshabilitarConductorAsync(int codigoConductor)
        {
            string sql = "UPDATE taxi SET habilitado = '0' WHERE codtaxi = @Codigo";
            var parameters = new { Codigo = codigoConductor };

            return await _defaultConnection.ExecuteAsync(sql, parameters, transaction: _defaultTransaction);
        }

        public async Task<int> LiberarConductorAsync(int codigoConductor)
        {
            string sql = "UPDATE taxi SET servicioactual = NULL WHERE codtaxi = @Codigo";
            var parameters = new { Codigo = codigoConductor };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> EliminarConductorAsync(int codigoConductor)
        {
            string sql = "UPDATE taxi SET estado = 'E' WHERE codtaxi = @Codigo";
            var parameters = new { Codigo = codigoConductor };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> HabilitarUnidadAsync(string placa)
        {
            string sql = "UPDATE device SET habilitada = '1' WHERE deviceid = @Placa";
            var parameters = new { Placa = placa };

            return await _defaultConnection.ExecuteAsync(sql, parameters, transaction: _defaultTransaction);
        }

        public async Task<int> DeshabilitarUnidadAsync(string placa)
        {
            string sql = "UPDATE device SET habilitada = '0' WHERE deviceid = @Placa";
            var parameters = new { Placa = placa };

            return await _defaultConnection.ExecuteAsync(sql, parameters, transaction: _defaultTransaction);
        }

        public async Task<int> LiberarUnidadAsync(string placa)
        {
            string sql = "UPDATE device SET rutaact = 0, feciniruta = 0, origen = null, destino = null WHERE deviceID = @DeviceID";
            var parameters = new { DeviceID = placa };

            return await _defaultConnection.ExecuteAsync(sql, parameters, transaction: _defaultTransaction);
        }

        public async Task<int> UpdDirPasServicio(int codpedido, string codubicli)
        {
            string sql = "UPDATE subservicio SET codubicli = @Codubicli WHERE codpedido = @Codpedido";
            var parameters = new { Codpedido = codpedido, Codubicli = codubicli };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> NuevoLugarCliente(LugarCliente lugarCliente)
        {
            string sql = @"INSERT INTO lugarcliente (codcli, direccion, distrito, wy, wx, estado) VALUES (@Codcli, @Direccion, @Distrito, @Wy, @Wx, 'E')";

            var parameters = new
            {
                Codcli = lugarCliente.Codcli,
                Direccion = lugarCliente.Direccion,
                Distrito = lugarCliente.Distrito,
                Wy = lugarCliente.Wy,
                Wx = lugarCliente.Wx
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> EliminarLugarCliente(int codlugar)
        {
            string sql = @"DELETE FROM lugarcliente WHERE codlugar = @Codlugar";
            var parameters = new { Codlugar = codlugar };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }
    }
}