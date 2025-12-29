using Dapper;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Cgcela;
using VelsatBackendAPI.Model.GestionPasajeros;

namespace VelsatBackendAPI.Data.Repositories
{
    public class GacelaRepository : IGacelaRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public GacelaRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction, IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        public async Task<IEnumerable<GPedido>> GetDetalleServicios(string usuario, string fechaini, string fechafin)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Iniciando GetReporteServiciosAtencionAsync");
                Console.WriteLine($"[DEBUG] Parámetros - Usuario: {usuario}, FechaIni: {fechaini}, FechaFin: {fechafin}");

                var pedidos = await ObtenerReporteServiciosAsync(usuario, fechaini, fechafin);

                Console.WriteLine($"[DEBUG] Consulta completada. Registros obtenidos: {pedidos?.Count() ?? 0}");

                if (pedidos != null && pedidos.Any())
                {
                    Console.WriteLine($"[DEBUG] Iniciando procesamiento de {pedidos.Count()} pedidos");
                    var resultado = ProcesarPedidos(pedidos.ToList());
                    Console.WriteLine($"[DEBUG] Procesamiento completado. Pedidos procesados: {resultado?.Count ?? 0}");
                    return resultado;
                }

                Console.WriteLine($"[DEBUG] No se encontraron pedidos. Retornando lista vacía.");
                return new List<GPedido>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en GetReporteServiciosAtencionAsync: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                throw new Exception($"Error al obtener reporte de servicios de atención: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<GPedido>> ObtenerReporteServiciosAsync(string usuario, string fechaini, string fechafin)
        {
            var sql = @"SELECT u.codpedido, s.fecha as fechaservicio, u.fecha as fechapedido, u.fechainicio as feciniped, u.fechafin as fecfinped, u.numero, s.tipo, s.grupo, s.empresa, c.apellidos as pasajero, t.nombres as nomtaxi, t.apellidos as apetaxi, s.unidad, s.fechaini, s.fechafin, l.direccion, l.distrito, u.calificacion from gts.servicio as s inner join gts.subservicio as u on s.codservicio=u.codservicio inner join gts.cliente as c on u.codcliente=c.codcliente inner join gts.lugarcliente as l on u.codubicli=l.codlugar inner join gts.taxi as t on s.codconductor= t.codtaxi where s.codusuario=@Usuario and s.estado<>'C' and STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i')>=STR_TO_DATE(@Fechaini,'%d/%m/%Y %H:%i') and STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i')<=STR_TO_DATE(@Fechafin,'%d/%m/%Y %H:%i') and u.codcliente<>'4175'";

            var parametros = new
            {
                Usuario = usuario,
                Fechaini = fechaini,
                Fechafin = fechafin
            };

            var resultado = await _doConnection.QueryAsync(sql, parametros, transaction: _doTransaction);

            return resultado.Select(MapearPedido).ToList();

        }

        private GPedido MapearPedido(dynamic row)
        {
            return new GPedido
            {
                Codigo = row.codpedido,
                Fecha = row.fechapedido?.ToString(),
                Fechaini = row.feciniped?.ToString(),
                Fechafin = row.fecfinped?.ToString(),
                Empresa = row.empresa?.ToString(),
                Numero = row.numero?.ToString(),
                Calificacion = row.calificacion?.ToString(),
                Pasajero = new GUsuario
                {
                    Nombre = row.pasajero?.ToString()
                },
                Lugar = new LugarCliente
                {
                    Direccion = row.direccion?.ToString(),
                    Distrito = row.distrito?.ToString()
                },
                Servicio = new GServicio
                {
                    Fecha = row.fechaservicio?.ToString(),
                    Tipo = row.tipo?.ToString(),
                    Grupo = row.grupo?.ToString(),
                    Newfechaini = row.fechaini?.ToString(),
                    Newfechafin = row.fechafin?.ToString(),
                    Unidad = new GUnidad
                    {
                        Codunidad = row.unidad?.ToString()
                    },
                    Conductor = new GUsuario
                    {
                        Nombre = row.nomtaxi?.ToString(),
                        Apepate = row.apetaxi?.ToString()
                    }
                }
            };
        }

        private List<GPedido> ProcesarPedidos(List<GPedido> listaPedidos)
        {

            foreach (var pedido in listaPedidos)
            {
                var servicio = pedido.Servicio;

                // Procesar Grupo
                if (string.IsNullOrEmpty(servicio.Grupo))
                {
                    servicio.Grupo = "NO DEFINIDO";
                }
                else if (servicio.Grupo == "A")
                {
                    servicio.Grupo = "AIRE";
                }
                else if (servicio.Grupo == "T")
                {
                    servicio.Grupo = "TIERRA";
                }

                // Procesar Tipo
                if (string.IsNullOrEmpty(servicio.Tipo))
                {
                    servicio.Tipo = "NO DEFINIDO";
                }
                else if (servicio.Tipo == "S")
                {
                    servicio.Tipo = "SALIDA";
                }
                else if (servicio.Tipo == "I")
                {
                    servicio.Tipo = "INGRESO";
                }

                // Procesar Calificación
                if (string.IsNullOrEmpty(pedido.Calificacion) || pedido.Calificacion == "0")
                {
                    pedido.Calificacion = "SIN CALIFICACION";
                }

                pedido.Servicio = servicio;
            }

            Console.WriteLine($"[DEBUG] ProcesarPedidos completado. Retornando {listaPedidos?.Count ?? 0} pedidos");
            return listaPedidos;
        }

        public async Task<IEnumerable<GServicio>> GetDuracionServicios(string usuario, string fechaini, string fechafin)
        {
            try
            {
                var servicios = await ReporteServiciosAtencionDuracion(usuario, fechaini, fechafin);

                if (servicios != null && servicios.Any())
                {
                    return ProcesarServicios(servicios.ToList());
                }

                return new List<GServicio>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener reporte de servicios de atención por duración: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<GServicio>> ReporteServiciosAtencionDuracion(string usuario, string fechaini, string fechafin)
        {
            var sql = @"
                SELECT 
                    s.fecha as fechaservicio, 
                    s.numero, 
                    s.tipo, 
                    s.grupo, 
                    s.empresa, 
                    t.nombres as nomtaxi, 
                    t.apellidos as apetaxi, 
                    s.unidad, 
                    s.fechaini, 
                    s.fechafin 
                FROM gts.servicio AS s 
                INNER JOIN gts.taxi AS t ON s.codconductor = t.codtaxi 
                WHERE s.codusuario = @CodigoUsuario 
                AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') >= STR_TO_DATE(@Fechaini, '%d/%m/%Y %H:%i') 
                AND STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') <= STR_TO_DATE(@Fechafin, '%d/%m/%Y %H:%i') 
                AND s.estado <> 'C'
                ORDER BY STR_TO_DATE(s.fecha, '%d/%m/%Y %H:%i') DESC";

            var parametros = new
            {
                CodigoUsuario = usuario,
                Fechaini = fechaini,
                Fechafin = fechafin
            };

            var resultado = await _doConnection.QueryAsync(sql, parametros, transaction: _doTransaction);
            return resultado.Select(MapearServicio);
        }

        private GServicio MapearServicio(dynamic row)
        {
            var servicio = new GServicio
            {
                Fecha = row.fechaservicio?.ToString(),
                Tipo = row.tipo?.ToString(),
                Grupo = row.grupo?.ToString(),
                Newfechaini = row.fechaini?.ToString(),
                Newfechafin = row.fechafin?.ToString(),
                Empresa = row.empresa?.ToString(),
                Numero = row.numero?.ToString(),
                Unidad = new GUnidad
                {
                    Codunidad = row.unidad?.ToString()
                },
                Conductor = new GUsuario
                {
                    Nombre = row.nomtaxi?.ToString(),
                    Apepate = row.apetaxi?.ToString()
                }
            };

            return servicio;
        }

        private List<GServicio> ProcesarServicios(List<GServicio> listaServicios)
        {
            foreach (var servicio in listaServicios)
            {
                // Procesar Grupo
                if (string.IsNullOrEmpty(servicio.Grupo))
                {
                    servicio.Grupo = "NO DEFINIDO";
                }
                else if (servicio.Grupo == "A")
                {
                    servicio.Grupo = "AIRE";
                }
                else if (servicio.Grupo == "T")
                {
                    servicio.Grupo = "TIERRA";
                }

                // Procesar Tipo
                if (string.IsNullOrEmpty(servicio.Tipo))
                {
                    servicio.Tipo = "NO DEFINIDO";
                }
                else if (servicio.Tipo == "S")
                {
                    servicio.Tipo = "SALIDA";
                }
                else if (servicio.Tipo == "I")
                {
                    servicio.Tipo = "INGRESO";
                }
            }

            return listaServicios;
        }

        public async Task<IEnumerable<GCarro>> GetUnidadesCercanas(double km, GCarro carroBase, string usuario)
        {
            try
            {
                // 1. Obtener coordenadas del vehículo base
                var gpsBase = await ObtenerCoordenadasVehiculo(carroBase.Codunidad);

                if (gpsBase == null || gpsBase.Posx == 0 || gpsBase.Posy == 0)
                {
                    return new List<GCarro>();
                }

                carroBase.Gps = gpsBase;
                carroBase.Distancia = 0.0; // La unidad base tiene distancia 0
                carroBase.EsUnidadBase = true; // Propiedad para identificar la unidad base

                var posYbase = ConvertirARadianes(gpsBase.Posx);
                var posXbase = ConvertirARadianes(gpsBase.Posy);

                // 2. OPTIMIZACIÓN PRINCIPAL: Obtener TODOS los vehículos con coordenadas en UNA SOLA consulta
                var vehiculosConGps = await ObtenerTodosVehiculosConGpsOptimizado(usuario);

                if (!vehiculosConGps.Any())
                {
                    return new List<GCarro> { carroBase };
                }

                // 3. Calcular distancias usando fórmula de Haversine
                var vehiculosConDistancia = CalcularDistanciasVehiculos(vehiculosConGps.ToList(), posXbase, posYbase);

                // 4. Filtrar por radio de búsqueda y ordenar
                var vehiculosCercanos = vehiculosConDistancia
            .Where(v => v.Distancia > 0.0 && v.Distancia <= km && !v.Codunidad.Equals(carroBase.Codunidad, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.Distancia)
            .ToList();

                // 5. Crear la lista resultado con la unidad base al inicio
                var resultado = new List<GCarro> { carroBase };
                resultado.AddRange(vehiculosCercanos);

                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener unidades cercanas: {ex.Message}", ex);
            }
        }

        // MÉTODO OPTIMIZADO: UNA SOLA CONSULTA SQL CON JOIN
        private async Task<IEnumerable<GCarro>> ObtenerTodosVehiculosConGpsOptimizado(string usuario)
        {
            var sql = @"SELECT DISTINCT vehiculos.Codunidad, vehiculos.Habilitado, d.deviceID as NumequipoGps, d.lastValidLongitude, d.lastValidLatitude, d.direccion FROM (SELECT deviceID as Codunidad, habilitada as Habilitado FROM device WHERE accountID = @CodUsuario UNION ALL SELECT DeviceName as Codunidad, '1' as Habilitado FROM DeviceUser WHERE UserID = @CodUsuario AND Status = '1') vehiculos INNER JOIN device d ON d.deviceID = vehiculos.Codunidad ORDER BY vehiculos.Codunidad";

            var parametros = new { CodUsuario = usuario };
            var resultado = await _defaultConnection.QueryAsync(sql, parametros, transaction: _defaultTransaction);

            return resultado.Select(MapearCarroConGpsOptimizado);
        }

        // MANTENER TUS MÉTODOS EXISTENTES (sin cambios)
        private async Task<Ggps> ObtenerCoordenadasVehiculo(string codunidad)
        {
            var sql = @"SELECT deviceID, lastValidLongitude, lastValidLatitude, direccion FROM device WHERE deviceID = @Codunidad";

            var parametros = new { Codunidad = codunidad };
            var resultado = await _defaultConnection.QueryFirstOrDefaultAsync(sql, parametros, transaction: _defaultTransaction);

            return resultado != null ? MapearGps(resultado) : null;
        }

        private List<GCarro> CalcularDistanciasVehiculos(List<GCarro> vehiculos, double posXbase, double posYbase)
        {
            foreach (var vehiculo in vehiculos)
            {
                if (vehiculo.Gps?.Posx == null || vehiculo.Gps?.Posy == null)
                    continue;

                var posY = ConvertirARadianes(vehiculo.Gps.Posx);
                var posX = ConvertirARadianes(vehiculo.Gps.Posy);

                // Fórmula de Haversine para calcular distancia entre dos puntos en la Tierra
                var sec1 = Math.Sin(posXbase) * Math.Sin(posX);
                var dl = Math.Abs(posYbase - posY);
                var sec2 = Math.Cos(posXbase) * Math.Cos(posX);
                var centralAngle = Math.Acos(sec1 + sec2 * Math.Cos(dl));
                var distancia = centralAngle * 6378.1; // Radio de la Tierra en km

                vehiculo.Distancia = Math.Round(distancia * 100) / 100.0; // Redondear a 2 decimales
            }

            return vehiculos;
        }

        // NUEVO MAPEO PARA LA CONSULTA OPTIMIZADA
        private GCarro MapearCarroConGpsOptimizado(dynamic row)
        {
            return new GCarro
            {
                Codunidad = row.Codunidad?.ToString()?.ToUpper(),
                Tipo = "2",
                Habilitado = row.Habilitado?.ToString(),
                EsUnidadBase = false, // Por defecto no es unidad base
                Gps = new Ggps
                {
                    Numequipo = row.NumequipoGps?.ToString(),
                    Posx = Math.Round(Convert.ToDouble(row.lastValidLongitude ?? 0), 5),
                    Posy = Math.Round(Convert.ToDouble(row.lastValidLatitude ?? 0), 5),
                    Ubicacion = new GUbicacion
                    {
                        Dircompleta = row.direccion?.ToString()
                    }
                }
            };
        }

        // MANTENER TUS MÉTODOS DE MAPEO EXISTENTES (sin cambios)
        private Ggps MapearGps(dynamic row)
        {
            var gps = new Ggps
            {
                Numequipo = row.deviceID?.ToString(),
                Posx = Math.Round(Convert.ToDouble(row.lastValidLongitude ?? 0), 5),
                Posy = Math.Round(Convert.ToDouble(row.lastValidLatitude ?? 0), 5),
                Ubicacion = new GUbicacion
                {
                    Dircompleta = row.direccion?.ToString()
                }
            };

            return gps;
        }

        private GCarro MapearCarroPrincipal(dynamic row)
        {
            return new GCarro
            {
                Codunidad = row.deviceID?.ToString()?.ToUpper(),
                Tipo = "2",
                Habilitado = row.habilitada?.ToString(),
            };
        }

        private GCarro MapearCarroUsuario(dynamic row)
        {
            return new GCarro
            {
                Codunidad = row.DeviceName?.ToString()?.ToUpper(),
                Tipo = "2",
            };
        }

        // MANTENER TU MÉTODO UTILITARIO (sin cambios)
        private static double ConvertirARadianes(double grados)
        {
            return grados * Math.PI / 180.0;
        }

        public async Task<List<GServicio>> GetServicios(string fechaini, string fechafin, string usuario)
        {
            // Parsear fecha inicial
            DateTime fecInicial;
            if (!DateTime.TryParseExact(fechaini, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecInicial))
            {
                throw new FormatException("Formato de fecha inicial incorrecto. Use yyyy-MM-dd HH:mm");
            }

            // Parsear fecha final
            DateTime fecFinal;
            if (!DateTime.TryParseExact(fechafin, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecFinal))
            {
                throw new FormatException("Formato de fecha final incorrecto. Use yyyy-MM-dd HH:mm");
            }

            string fechaIniFormateada = fecInicial.ToString("dd/MM/yyyy HH:mm");
            string fechaFinFormateada = fecFinal.ToString("dd/MM/yyyy HH:mm");

            List<GServicio> lista = await ControlServiciosMovil(fechaIniFormateada, fechaFinFormateada, usuario);

            if (lista == null)
            {
                return new List<GServicio>();
            }

            if (lista.Any())
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    GServicio s = lista[i];

                    if (!string.IsNullOrEmpty(s.Conductor?.Codigo))
                    {
                        GUsuario conductor = await GetDetalleConductor(s.Conductor.Codigo);
                        s.Conductor = conductor;
                    }

                    if (!string.IsNullOrEmpty(s.Owner?.Codigo))
                    {
                        GUsuario owner = await DetallePasajero(s.Owner);
                        s.Owner = owner;
                    }

                    lista[i] = s;
                }
            }

            return lista;
        }

        private async Task<GUsuario> DetallePasajero(GUsuario owner)
        {
            string sql = @"select codcliente, nombres, apellidos, login, clave, codlugar, codlan, sexo, empresa, telefono from cliente where codcliente=@Codcliente";

            var parameters = new { Codcliente = owner.Codigo };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (!results.Any())
            {
                return null;
            }

            var row = results.First();

            var pasajero = new GUsuario
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

        private async Task<List<GServicio>> ControlServiciosMovil(string fechaini, string fechafin, string usu)
        {
            string sql = @"Select s.costototal, s.owner, s.numero, s.codservicio, s.tipoarea, s.tipo, s.totalpax, s.numeromovil, s.empresa, s.grupo, s.fecha, s.unidad, s.fechainifin, s.fechaini, s.fechafin, s.fecplan, s.atolatam, s.gourmetlatam, s.parqueolatam, s.lcclatam, s.codusuario, STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i') as formato, s.codconductor, s.estado as estadoservicio, s.tipoturismo, s.grupoturismo, s.destino from servicio s where STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i')>=STR_TO_DATE(@Fecini,'%d/%m/%Y %H:%i') and STR_TO_DATE(s.fecha,'%d/%m/%Y %H:%i')<=STR_TO_DATE(@Fecfin,'%d/%m/%Y %H:%i') and s.codusuario=@Usuario and s.estado != 'C' order by formato, codservicio";

            var parameters = new { Fecini = fechaini, Fecfin = fechafin, Usuario = usu };

            var results = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (results == null)
            {
                return new List<GServicio>();
            }

            var listaServicios = new List<GServicio>();

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

                    var servicio = new GServicio
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
                        Newfechafin = row.fechafin ?? string.Empty,
                        Estado = row.estadoservicio ?? string.Empty,
                        Area = row.tipoarea != null ? "TURISMO" : "TEP",
                        Tipo = row.tipoarea != null ? (row.tipoturismo ?? string.Empty) : (row.tipo ?? string.Empty),
                        Nomgrupo = row.grupoturismo ?? string.Empty,
                        Costototal = row.costototal ?? "0",
                        Destino = row.destino?.ToString() ?? string.Empty,
                        NomDestino = nombreDestino,
                        Owner = new GUsuario
                        {
                            Codigo = row.owner ?? string.Empty
                        },

                        Conductor = new GUsuario
                        {
                            Codigo = row.codconductor ?? string.Empty
                        },

                        Unidad = new GUnidad
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

        private async Task<GUsuario> GetDetalleConductor(string codtaxi)
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

            var conductor = new GUsuario
            {
                Codigo = row.codtaxi.ToString(),
                Nombre = row.nombres,
                Apepate = row.apellidos,
                Login = row.login,
                Clave = row.clave,
                Telefono = row.telefono,
                Servicioactual = new GServicio
                {
                    Codservicio = row.servicioactual
                }
            };

            return conductor;
        }

        private async Task<GUsuario?> BuscarDestino(string codcliente)
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

            var usuario = new GUsuario
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

        public async Task<List<GPedido>> ListaPasajeroServicio(string codservicio)
        {
            string sql = @"Select su.observacion, su.costo, su.codpedido, su.estado, su.fecha as fecpedido, su.vuelo, su.arealan, su.codcliente, su.fechafin as feclectura, su.feccancelpas, su.orden, c.apellidos, c.codlugar, l.wx, l.wy, l.direccion, l.distrito FROM subservicio su, cliente c, lugarcliente l WHERE su.codcliente = c.codcliente and su.codubicli = l.codlugar and su.codservicio = @Codservicio and su.estado != 'C' order by orden";

            var parameters = new
            {
                Codservicio = codservicio,
            };

            var row = await _doConnection.QueryAsync(sql, parameters, transaction: _doTransaction);

            if (row == null || !row.Any())
            {
                Console.WriteLine("No se encontraron resultados.");

                return new List<GPedido>(); // Retorna lista vacía si no hay datos
            }
            Console.WriteLine($"Registros obtenidos: {row.Count()}");

            var pasajeros = row.Select(row => new GPedido
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
                Pasajero = new GUsuario
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

        public async Task<int> UpdateEstadoServicio(GPedido pedido)
        {
            string fechareg = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            pedido.Feccancelpas = fechareg;

            // Validar que Codigo no sea null
            if (!pedido.Codigo.HasValue)
            {
                throw new ArgumentException("El código del pedido es requerido.");
            }

            // SIEMPRE obtener codservicio de la base de datos
            string codservicio = await ObtenerCodServicioPorPedido(pedido.Codigo.Value);

            int rs = await EliminarPedido(pedido);

            // DECREMENTAR TOTALPAX SI LA CANCELACIÓN FUE EXITOSA
            if (rs > 0 && !string.IsNullOrEmpty(codservicio))
            {
                await DecrementarTotalPax(codservicio);
            }

            return rs;
        }

        private async Task<int> EliminarPedido(GPedido pedido)
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

        public async Task<int> NuevoSubServicioPasajero(GPedido pedido)
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
            if (!int.TryParse(codservicio, out int codservicioInt))
            {
                throw new ArgumentException("El código de servicio no es válido. Debe ser un número entero.");
            }

            string sql = @"UPDATE servicio 
                   SET totalpax = CAST(CAST(totalpax AS UNSIGNED) + 1 AS CHAR) 
                   WHERE codservicio = @Codservicio";

            return await _doConnection.ExecuteAsync(sql,
                new { Codservicio = codservicioInt },
                transaction: _doTransaction
            );
        }

        public async Task<int> ReiniciarServicio(int codservicio)
        {
            string sql = "Update servicio set fechaini = NULL, fechafin = NULL where codservicio = @Codservicio";

            var parameters = new
            {
                Codservicio = codservicio
            };

            return await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);
        }

        public async Task<int> GuardarServicio(GPedido pedido)
        {
            string sql = @"Update subservicio set fecha = @Fecha, distancia = @Distancia, orden = @Orden WHERE codpedido = @Codpedido";

            var parameters = new
            {
                Codpedido = pedido.Codigo,
                Fecha = pedido.Fecha,
                Distancia = pedido.Distancia,
                Orden = pedido.Orden,
            };

            int filasAfectadas = await _doConnection.ExecuteAsync(sql, parameters, transaction: _doTransaction);

            return filasAfectadas;
        }

        public async Task<List<GServicio>> ProcessExcel(string filePath, string tipoGrupo, string usuario)
        {
            var lista = new List<GServicio>();
            var listaPedidos = new List<GPedido>();
            int numServ = 0;

            try
            {
                // Archivo de tierra
                if (tipoGrupo == "T")
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var workbook = new HSSFWorkbook(fileStream);
                        var worksheet = workbook.GetSheet("Reporte");

                        if (worksheet == null)
                        {
                            throw new InvalidOperationException("No se encontró la hoja 'Reporte'");
                        }

                        DateTime fecha;
                        string tipo = null;
                        DateTime hora;
                        string fechaFormateada = null;
                        string horaFormateada = null;
                        int rowCount = 0;

                        foreach (IRow row in worksheet)
                        {
                            if (row == null) continue;
                            rowCount++;

                            var pedido = new GPedido();
                            var lugar = new LugarCliente();

                            foreach (ICell cell in row.Cells)
                            {
                                if (cell == null) continue;

                                switch (cell.ColumnIndex)
                                {
                                    case 2: // Tipo
                                        tipo = cell.StringCellValue;
                                        if (tipo == "E") tipo = "I";
                                        break;

                                    case 1: // Fecha
                                        if (cell.DateCellValue.HasValue)
                                        {
                                            fecha = cell.DateCellValue.Value;
                                            fechaFormateada = fecha.ToString("dd/MM/yyyy");
                                        }
                                        break;

                                    case 6: // Hora
                                        if (cell.DateCellValue.HasValue)
                                        {
                                            hora = cell.DateCellValue.Value;
                                            horaFormateada = hora.ToString("HH:mm");
                                        }
                                        break;

                                    case 8: // Área LAN
                                        pedido.Arealan = cell.StringCellValue;
                                        break;

                                    case 7: // Código y nombre del pasajero
                                        var usuarioPasajero = new GUsuario();
                                        string texto = cell.StringCellValue;

                                        string[] partes = texto.Split('-');

                                        if (partes.Length >= 2)
                                        {
                                            usuarioPasajero.Codigo = partes[0].Trim();
                                            usuarioPasajero.Codlan = partes[0].Trim();
                                            usuarioPasajero.Nombre = partes[1].Trim();
                                        }
                                        pedido.Pasajero = usuarioPasajero;
                                        break;

                                    case 9: // Dirección
                                        lugar.Direccion = cell.StringCellValue;
                                        break;

                                    case 10: // Distrito
                                        lugar.Distrito = cell.StringCellValue;
                                        break;

                                    case 14: // Coordenada X (longitud)
                                        double x = cell.NumericCellValue;
                                        if (x == 0.0)
                                        {
                                            x = -77.109134;
                                        }

                                        lugar.Wx = x.ToString();
                                        pedido.Lugar = lugar;
                                        break;

                                    case 13: // Coordenada Y (latitud)
                                        double y = cell.NumericCellValue;
                                        if (y == 0.0)
                                        {
                                            y = -12.019819;
                                        }

                                        lugar.Wy = y.ToString();
                                        break;

                                    case 15: // Número de servicio
                                        int servActual = (int)cell.NumericCellValue;
                                        string numLan = servActual.ToString();
                                        pedido.Numero = numLan;

                                        if (servActual != numServ)
                                        {
                                            numServ = servActual;

                                            // Crear punto de aeropuerto
                                            var pedidoAeropuerto = new GPedido();
                                            var lugarAeropuerto = new LugarCliente();
                                            lugarAeropuerto.Wy = "-12.019819";
                                            lugarAeropuerto.Wx = "-77.109134";
                                            pedidoAeropuerto.Lugar = lugarAeropuerto;

                                            string fechaHora = $"{fechaFormateada} {horaFormateada}";

                                            var usuarioAereo = new GUsuario
                                            {
                                                Codigo = "4175",
                                                Codlan = "4175"
                                            };
                                            pedidoAeropuerto.Fecha = fechaHora;
                                            pedidoAeropuerto.Pasajero = usuarioAereo;
                                            pedidoAeropuerto.Numero = numLan;

                                            listaPedidos.Add(pedidoAeropuerto);
                                        }
                                        break;
                                }
                            }

                            listaPedidos.Add(pedido);
                        }

                        // Procesar servicios
                        lista = ProcessServices(listaPedidos, usuario, tipoGrupo, tipo);

                        // Registrar servicios
                        foreach (var servicio in lista)
                        {
                            await RegistrarServicio(servicio);
                        }
                        return lista;
                    }
                }

                // Archivo de aire
                if (tipoGrupo == "A")
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var workbook = new HSSFWorkbook(fileStream);
                        var worksheet = workbook.GetSheet("Reporte");

                        if (worksheet == null)
                        {
                            throw new InvalidOperationException("No se encontró la hoja 'Reporte'");
                        }

                        DateTime fecha;
                        string tipo = null;
                        DateTime hora, horaRecojo;
                        string fechaFormateada = null;
                        string horaFormateada = null;
                        string horaRecojoFormateada = null;
                        int rowCount = 0;

                        foreach (IRow row in worksheet)
                        {
                            if (row == null) continue;
                            rowCount++;

                            var pedido = new GPedido();
                            var lugar = new LugarCliente();

                            foreach (ICell cell in row.Cells)
                            {
                                if (cell == null) continue;

                                switch (cell.ColumnIndex)
                                {
                                    case 2: // Tipo
                                        tipo = cell.StringCellValue;
                                        if (tipo == "E") tipo = "I";
                                        break;

                                    case 1: // Fecha
                                        if (cell.DateCellValue.HasValue)
                                        {
                                            fecha = cell.DateCellValue.Value;
                                            fechaFormateada = fecha.ToString("dd/MM/yyyy");
                                        }
                                        break;

                                    case 6: // Vuelo
                                        pedido.Vuelo = cell.StringCellValue;
                                        break;

                                    case 7: // Hora de vuelo
                                        if (cell.DateCellValue.HasValue)
                                        {
                                            hora = cell.DateCellValue.Value;
                                            horaFormateada = hora.ToString("HH:mm");
                                        }

                                        break;

                                    case 8: // Hora de recojo
                                        if (cell.DateCellValue.HasValue)
                                        {
                                            horaRecojo = cell.DateCellValue.Value;
                                            horaRecojoFormateada = horaRecojo.ToString("HH:mm");
                                            string fechaHoraRecojo = $"{fechaFormateada} {horaRecojoFormateada}";
                                            pedido.Fecha = fechaHoraRecojo;
                                        }

                                        break;

                                    case 9: // Categoría LAN
                                        pedido.Categorialan = cell.StringCellValue;
                                        break;

                                    case 10: // Código y nombre del pasajero
                                        var usuarioPasajero = new GUsuario();
                                        string texto = cell.StringCellValue;

                                        string[] partes = texto.Split('-');

                                        if (partes.Length >= 2)
                                        {
                                            usuarioPasajero.Codigo = partes[0].Trim();
                                            usuarioPasajero.Codlan = partes[0].Trim();
                                            usuarioPasajero.Nombre = partes[1].Trim();
                                        }

                                        pedido.Pasajero = usuarioPasajero;
                                        break;

                                    case 11: // Dirección
                                        lugar.Direccion = cell.StringCellValue;
                                        break;

                                    case 12: // Distrito
                                        lugar.Distrito = cell.StringCellValue;
                                        break;

                                    case 16: // Coordenada X (longitud)
                                        double x = cell.NumericCellValue;
                                        if (x == 0.0)
                                        {
                                            x = -77.109134;
                                        }

                                        lugar.Wx = x.ToString();
                                        pedido.Lugar = lugar;
                                        break;

                                    case 15: // Coordenada Y (latitud)
                                        double y = cell.NumericCellValue;
                                        if (y == 0.0)
                                        {
                                            y = -12.019819;
                                        }

                                        lugar.Wy = y.ToString();
                                        break;

                                    case 17: // Número de servicio
                                        int servActual = (int)cell.NumericCellValue;
                                        string numLan = servActual.ToString();
                                        pedido.Numero = numLan;

                                        if (servActual != numServ)
                                        {
                                            numServ = servActual;

                                            // Crear punto de empresa/aeropuerto
                                            var pedidoEmpresa = CreateCompanyPoint(usuario,
                                                fechaFormateada, horaFormateada, numLan);

                                            listaPedidos.Add(pedidoEmpresa);
                                        }
                                        break;
                                }
                            }
                            listaPedidos.Add(pedido);
                        }

                        // Procesar servicios
                        lista = ProcessServices(listaPedidos, usuario, tipoGrupo, tipo);

                        // Registrar servicios
                        foreach (var servicio in lista)
                        {
                            if (usuario == "aloremisse" ||
                                usuario == "movilbusavianca")
                            {
                                await RegistrarServicioAvianca(servicio);
                            }
                            else
                            {
                                await RegistrarServicio(servicio);
                            }
                        }

                        // Limpiar referencias circulares antes de retornar
                        foreach (var servicio in lista)
                        {
                            if (servicio.Listapuntos != null)
                            {
                                foreach (var pedido in servicio.Listapuntos)
                                {
                                    // Romper la referencia circular
                                    pedido.Servicio = null;

                                    // También limpiar referencias en objetos anidados si las hay
                                    if (pedido.Pasajero?.Lugar != null)
                                    {
                                        // Si GUsuario.Lugar también tiene referencias circulares, limpiarlas
                                        // pedido.Pasajero.Lugar.SomeCircularReference = null;
                                    }
                                }
                            }
                        }
                        return lista;
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"❌ ERROR - Archivo no encontrado: {ex.Message}");
                Console.WriteLine($"❌ Ruta buscada: {filePath}");
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"❌ ERROR de E/S: {ex.Message}");
                Console.WriteLine($"❌ Archivo: {filePath}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR INESPERADO: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                throw;
            }

            return lista;
        }

        private GPedido CreateCompanyPoint(string codgt, string fecha, string hora, string numLan)
        {
            var pedido = new GPedido();
            var lugar = new LugarCliente();
            var usuario = new GUsuario();

            switch (codgt)
            {
                case "movilbus":
                    lugar.Wy = "-12.0282387";
                    lugar.Wx = "-76.968418";
                    usuario.Codigo = "11150";
                    usuario.Codlan = "OFNEXA";
                    break;
                case "movilbussiemens":
                    lugar.Wy = "-12.106211";
                    lugar.Wx = "-77.020124";
                    usuario.Codigo = "12033";
                    usuario.Codlan = "SIEMENS";
                    break;
                case "movilbusrep":
                    lugar.Wy = "-12.179379";
                    lugar.Wx = "-76.974955";
                    usuario.Codigo = "9355";
                    usuario.Codlan = "OREP";
                    break;
                case "movilbusocjm":
                    lugar.Wy = "-11.972211";
                    lugar.Wx = "-76.885378";
                    usuario.Codigo = "18537";
                    usuario.Codlan = "OCJM";
                    break;
                case "movilbusmkc":
                    lugar.Wy = "-12.099841";
                    lugar.Wx = "-76.976174";
                    usuario.Codigo = "10968";
                    usuario.Codlan = "OMKCOLLEGE";
                    break;
                case "movilbusmafre":
                    lugar.Wy = "-12.128339";
                    lugar.Wx = "-77.025130";
                    usuario.Codigo = "16784";
                    usuario.Codlan = "MAPFRE";
                    break;
                case "gacelaterpel":
                    lugar.Wy = "-12.043816";
                    lugar.Wx = "-77.134059";
                    usuario.Codigo = "26340";
                    usuario.Codlan = "TERPEL";
                    break;
                case "slesac":
                case "cgacela":
                case "movilbusavianca":
                default:
                    lugar.Wy = "-12.019819";
                    lugar.Wx = "-77.109134";
                    usuario.Codigo = "4175";
                    usuario.Codlan = "4175";
                    break;
            }

            pedido.Lugar = lugar;
            pedido.Fecha = $"{fecha} {hora}";
            pedido.Pasajero = usuario;
            pedido.Numero = numLan;

            return pedido;
        }

        private List<GServicio> ProcessServices(List<GPedido> listaPedidos, string usuario, string tipoGrupo, string tipo)
        {
            var lista = new List<GServicio>();

            if (listaPedidos.Count > 0)
            {
                string serv = "", servActual = "";
                GServicio servicio = null;
                List<GPedido> subServ = null;
                int numLista = listaPedidos.Count - 1;

                for (int x = 0; x < listaPedidos.Count; x++)
                {
                    var pedido = listaPedidos[x];
                    servActual = pedido.Numero;

                    if (!serv.Equals(servActual))
                    {
                        if (subServ != null)
                        {
                            servicio.Listapuntos = subServ;
                            lista.Add(servicio);
                        }

                        servicio = new GServicio();
                        subServ = new List<GPedido>();

                        // Crear objeto GUsuario internamente
                        servicio.Usuario = usuario;
                        servicio.Numero = pedido.Numero;
                        servicio.Tipo = tipo;
                        servicio.Grupo = tipoGrupo;
                        servicio.Fecha = pedido.Fecha;
                        servicio.Empresa = GetCompanyName(usuario);
                        serv = pedido.Numero;
                    }

                    subServ.Add(pedido);

                    if (x == numLista)
                    {
                        if (subServ != null)
                        {
                            servicio.Listapuntos = subServ;
                            lista.Add(servicio);
                        }
                    }
                }
            }

            for (int i = 0; i < lista.Count; i++)
            {
                var srv = lista[i];
            }

            return lista;
        }

        private string GetCompanyName(string codgt)
        {
            string companyName = codgt switch
            {
                "movilbus" => "NEXA",
                "movilbusavianca" => "AVIANCA",
                "movilbusrep" => "REP",
                "movilbussiemens" => "SIEMENS",
                "movilbusmkc" => "MKCOLLEGUE",
                "movilbusmafre" => "MAPFRE",
                "movilbusocjm" => "NEXA CJM",
                "gacelaterpel" => "TERPEL",
                "gaceladhl" => "DHL",
                "slesac" or "cgacela" => "LATAM",
                _ => "LATAM"
            };

            return companyName;
        }

        private async Task RegistrarServicio(GServicio servicio)
        {
            var listaPuntos = servicio.Listapuntos;

            servicio = await NuevoServicio(servicio);

            int puntoCount = 0;
            foreach (var pedido in listaPuntos)
            {
                pedido.Orden = puntoCount.ToString();
                puntoCount++;

                // Caso especial: codlan 72098 → 072098
                if ("72098".Equals(pedido.Pasajero.Codlan))
                {
                    var pa = pedido.Pasajero;
                    string codlan = "072098";
                    pa.Codlan = codlan;
                    pedido.Pasajero = pa;
                }

                // Buscar si el pasajero existe y su lugar de residencia
                var us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);

                // Si el pasajero existe
                if (us != null)
                {
                    double d = Distancia(
                        double.Parse(us.Lugar.Wx),
                        double.Parse(us.Lugar.Wy),
                        double.Parse(pedido.Lugar.Wx),
                        double.Parse(pedido.Lugar.Wy)
                    );

                    // Si cambió de lugar
                    if (d > 0.0)
                    {
                        // Eliminar lugar actual
                        await EliminarLugar(us.Lugar);

                        // Guardar nuevo lugar
                        var lu = pedido.Lugar;
                        lu.Codcli = pedido.Pasajero.Codlan; // 🔑 Aquí se asegura la FK
                        await GuardarLugar(lu);

                        // Volvemos a traer al pasajero con su lugar actualizado
                        us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);
                    }

                    // Asociamos pasajero, lugar y servicio al pedido
                    pedido.Pasajero = us;
                    pedido.Lugar = us.Lugar;
                    pedido.Servicio = servicio;

                    await NuevoSubservicio(pedido);
                }

                // Si el pasajero NO existe
                if (us == null)
                {
                    // Insertar pasajero
                    await NuevoPasajero(pedido.Pasajero);

                    // Insertar lugar
                    var ld = pedido.Lugar;
                    ld.Codcli = pedido.Pasajero.Codlan; // 🔑 FK correcta
                    await GuardarLugar(ld);

                    // Recuperar pasajero ya con lugar asociado
                    us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);

                    if (us != null)
                    {
                        pedido.Pasajero = us;
                        pedido.Lugar = us.Lugar;
                        pedido.Servicio = servicio;

                        await NuevoSubservicio(pedido);
                    }
                }
            }
        }

        private async Task RegistrarServicioAvianca(GServicio servicio)
        {
            var listaPuntos = servicio.Listapuntos;

            servicio = await NuevoServicio(servicio);

            int puntoCount = 0;
            foreach (var pedido in listaPuntos)
            {
                pedido.Orden = puntoCount.ToString();

                puntoCount++;

                // Registro de codln de usuario especial que se repite con avianca
                if ("72098".Equals(pedido.Pasajero.Codlan))
                {
                    var pa = pedido.Pasajero;
                    string codlan = "072098";
                    pa.Codlan = codlan;
                    pedido.Pasajero = pa;
                }

                // Buscar si el pasajero existe y su lugar de residencia
                var us = await LugarPasajeroAvianca(pedido.Pasajero);

                // Si el pasajero se encuentra en la base de datos
                if (us != null)
                {
                    double d = Distancia(
                        double.Parse(us.Lugar.Wx),
                        double.Parse(us.Lugar.Wy),
                        double.Parse(pedido.Lugar.Wx),
                        double.Parse(pedido.Lugar.Wy)
                    );

                    // Si la diferencia es mayor a cero es porque cambió de lugar
                    if (d > 0.0)
                    {
                        // Eliminar el actual lugar y guardar el nuevo lugar
                        await EliminarLugar(us.Lugar);

                        var lu = pedido.Lugar;
                        lu.Codcli = pedido.Pasajero.Codlan;
                        await GuardarLugar(lu);

                        us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);
                    }

                    pedido.Pasajero = us;
                    pedido.Lugar = us.Lugar;
                    pedido.Servicio = servicio;

                    await NuevoSubservicio(pedido);
                }

                // Si el pasajero NO se encuentra en la base de datos
                if (us == null)
                {
                    // Ingresar al nuevo pasajero
                    await NuevoPasajeroAvianca(pedido.Pasajero);

                    var ld = pedido.Lugar;
                    ld.Codcli = pedido.Pasajero.Codlan;

                    // Ingresar la nueva dirección
                    await GuardarLugar(pedido.Lugar);

                    // Grabar el pedido
                    us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);

                    if (us != null)
                    {
                        pedido.Pasajero = us;
                        pedido.Lugar = us.Lugar;
                        pedido.Servicio = servicio;

                        await NuevoSubservicio(pedido);
                    }
                }
            }
        }

        private double Distancia(double posxini, double posyini, double posx, double posy)
        {
            double posYbase = Math.PI * posxini / 180.0; // ToRadians
            double posXbase = Math.PI * posyini / 180.0;
            double posY = Math.PI * posx / 180.0;
            double posX = Math.PI * posy / 180.0;

            // Calculando la distancia
            double sec1 = Math.Sin(posXbase) * Math.Sin(posX);
            double dl = Math.Abs(posYbase - posY);
            double sec2 = Math.Cos(posXbase) * Math.Cos(posX);
            double centralAngle = Math.Acos(sec1 + sec2 * Math.Cos(dl));
            double distancia = centralAngle * 6378.1;
            double d = Math.Round(distancia * 100) / 100;

            return d;
        }

        private async Task<GServicio> NuevoServicio(GServicio servicio)
        {
            var sql = @"INSERT INTO servicio (numero, tipo, codusuario, estado, fecha, grupo, empresa) VALUES (@numero, @tipo, @codusuario, 'P', @fecha, @grupo, @empresa)";

            try
            {
                await _doConnection.ExecuteAsync(sql, new
                {
                    numero = servicio.Numero,
                    tipo = servicio.Tipo,
                    codusuario = servicio.Usuario,
                    fecha = servicio.Fecha,
                    grupo = servicio.Grupo,
                    empresa = servicio.Empresa
                }, transaction: _doTransaction);

                var selectSql = "SELECT codservicio from SERVICIO order by codservicio DESC LIMIT 1";
                var codservicio = await _doConnection.QueryFirstOrDefaultAsync<string>(selectSql, transaction: _doTransaction);
                servicio.Codservicio = codservicio;

                return servicio;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<GUsuario> LugarPasajeroAvianca(GUsuario pasajero)
        {
            var sql = @"SELECT l.codlugar, l.wy, l.wx, c.codcliente, c.apellidos, c.codlan FROM cliente c, lugarcliente l WHERE l.codcli = c.codlugar AND codlan = @Codlan AND c.empresa = 'AVIANCA' AND c.estadocuenta = 'A' AND l.estado = 'A'";

            try
            {
                var parametros = new { Codlan = pasajero.Codlan };
                var resultado = await _doConnection.QueryFirstOrDefaultAsync(sql, parametros, transaction: _doTransaction);

                if (resultado != null)
                {
                    var usuario = new GUsuario();
                    usuario.Codigo = resultado.codcliente.ToString();
                    usuario.Nombre = resultado.apellidos;
                    usuario.Codlan = resultado.codlan;

                    var lugar = new LugarCliente();
                    lugar.Codlugar = resultado.codlugar;
                    lugar.Wy = resultado.wy?.ToString();
                    lugar.Wx = resultado.wx?.ToString();
                    usuario.Lugar = lugar;

                    return usuario;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> EliminarLugar(LugarCliente lugar)
        {
            var sql = "UPDATE lugarcliente SET estado = 'E' WHERE codlugar = @codlugar";
            var parametros = new { codlugar = lugar.Codlugar };

            try
            {
                int result = await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> GuardarLugar(LugarCliente lugar)
        {
            var sql = @"INSERT INTO lugarcliente (direccion, wx, wy, codcli, distrito, estado) VALUES (@direccion, @wx, @wy, @codcli, @distrito, 'A')";

            var parametros = new
            {
                direccion = lugar.Direccion,
                wx = lugar.Wx,
                wy = lugar.Wy,
                codcli = lugar.Codcli,
                distrito = lugar.Distrito
            };

            try
            {
                int result = await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<GUsuario> LugarPasajero(GUsuario pasajero, string empresa)
        {
            var sql = @"SELECT l.codlugar, l.wy, l.wx, c.codcliente, c.apellidos, c.codlan FROM cliente c, lugarcliente l WHERE l.codcli = c.codlugar AND codlan = @Codlan AND c.empresa = @Empresa AND c.estadocuenta = 'A' AND l.estado = 'A'";

            try
            {
                var parametros = new { Codlan = pasajero.Codlan, Empresa = empresa};
                var resultado = await _doConnection.QueryFirstOrDefaultAsync(sql, parametros, transaction: _doTransaction);

                if (resultado != null)
                {
                    var usuario = new GUsuario();
                    usuario.Codigo = resultado.codcliente.ToString();
                    usuario.Nombre = resultado.apellidos; // ✅ AGREGAR NOMBRE
                    usuario.Codlan = resultado.codlan;     // ✅ AGREGAR CODLAN

                    var lugar = new LugarCliente();
                    lugar.Codlugar = resultado.codlugar;
                    lugar.Wy = resultado.wy?.ToString();
                    lugar.Wx = resultado.wx?.ToString();
                    usuario.Lugar = lugar;

                    return usuario;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> NuevoSubservicio(GPedido pedido)
        {
            var sql = @"INSERT INTO subservicio (codubicli, fecha, estado, codcliente, numero, codservicio, orden, distancia, categorialan, arealan, vuelo) VALUES (@codubicli, @fecha, 'P', @codcliente, @numero, @codservicio, @orden, @distancia, @categorialan, @arealan, @vuelo)";

            var parametros = new
            {
                codubicli = pedido.Lugar.Codlugar,
                fecha = pedido.Fecha,
                codcliente = pedido.Pasajero.Codigo,
                numero = pedido.Numero,
                codservicio = pedido.Servicio.Codservicio,
                orden = pedido.Orden,
                distancia = pedido.Distancia,
                categorialan = pedido.Categorialan,
                arealan = pedido.Arealan,
                vuelo = pedido.Vuelo
            };

            try
            {
                int result = await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> NuevoPasajeroAvianca(GUsuario pasajero)
        {
            var sql = @"INSERT INTO cliente (apellidos, sexo, codlan, estadocuenta, codlugar, clave, empresa) VALUES (@apellidos, @sexo, @codlan, 'A', @codlugar, '123', 'AVIANCA')";

            var parametros = new
            {
                apellidos = pasajero.Nombre,
                sexo = pasajero.Sexo,
                codlan = pasajero.Codlan,
                codlugar = pasajero.Codlan
            };

            try
            {
                int result = await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> NuevoPasajero(GUsuario pasajero)
        {
            var sql = @"INSERT INTO cliente (apellidos, sexo, codlan, estadocuenta, codlugar, clave) VALUES (@apellidos, @sexo, @codlan, 'A', @codlugar, '123')";

            var parametros = new
            {
                apellidos = pasajero.Nombre,
                sexo = pasajero.Sexo,
                codlan = pasajero.Codlan,
                codlugar = pasajero.Codlan
            };

            try
            {
                int result = await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        //////////////////////Externo para Gacela
        // Método público principal para registrar servicios externos
        public async Task<List<GServicio>> RegistrarServicioExterno(List<GServicio> listaServicios, string usuario)
        {
            var resultado = new List<GServicio>();

            foreach (var servicio in listaServicios)
            {
                string estadoRegistro = await NuevoServicioGacela(servicio, usuario);
                servicio.Estado = estadoRegistro;
                resultado.Add(servicio);
            }

            return resultado;
        }

        // Método principal de procesamiento (equivalente a nuevoserviciogacela)
        private async Task<string> NuevoServicioGacela(GServicio servicio, string usuario)
        {
            string respuesta = "Servicio Registrado Correctamente";

            // Verificar si contiene pasajeros
            if (servicio.Listapuntos == null || !servicio.Listapuntos.Any())
            {
                return "Error servicio sin pasajeros";
            }

            // Verificar valores de servicio
            if (VerificarNuloVacio(servicio.Numero) || VerificarNuloVacio(servicio.Fecha?.ToString()) ||
                VerificarNuloVacio(servicio.Tipo) || VerificarNuloVacio(servicio.Grupo) ||
                VerificarNuloVacio(servicio.Empresa) || VerificarNuloVacio(servicio.Codigoexterno))
            {
                return "Datos de servicio incompletos, verificar JSON";
            }

            // Verificar si el código externo existe y se encuentra activo
            var servicioActivo = await ServicioActivoExterno(servicio);
            if (servicioActivo != null)
            {
                return "Codigo de servicio Existe y se encuentra activo, debe cancelar servicio antes";
            }

            // VERIFICAR DETALLE DE SERVICIO
            foreach (var pedido in servicio.Listapuntos)
            {
                if (VerificarNuloVacio(pedido.Pasajero.Codlan) ||
                    VerificarNuloVacioDouble(pedido.Lugar.Wx) ||
                    VerificarNuloVacioDouble(pedido.Lugar.Wy) ||
                    VerificarNuloVacio(pedido.Lugar.Direccion) ||
                    VerificarNuloVacio(pedido.Pasajero.Nombre) ||
                    VerificarNuloVacio(pedido.Lugar.Distrito) ||
                    VerificarNuloVacio(pedido.Fecha?.ToString()) ||
                    VerificarNuloVacio(pedido.Orden))
                {
                    return "Datos de pedido incompleto, verificar JSON";
                }

                // Buscar si el pasajero existe y su lugar de residencia
                var us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);

                // Si el pasajero se encuentra en la base de datos
                if (us != null)
                {
                    double d = DistanciaExterno(
                        double.Parse(us.Lugar.Wx),
                        double.Parse(us.Lugar.Wy),
                        double.Parse(pedido.Lugar.Wx),
                        double.Parse(pedido.Lugar.Wy)
                    );

                    // Si la diferencia es mayor a cero es porque cambió de lugar
                    if (d > 0.0)
                    {
                        // Eliminar el actual lugar y guardar el nuevo lugar
                        await EliminarLugar(us.Lugar);

                        var lu = pedido.Lugar;
                        lu.Codcli = pedido.Pasajero.Codlan; // Asignar FK
                        await GuardarLugar(lu);

                        us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);
                    }

                    // Actualizar datos del pasajero
                    var pasajero = new GUsuario
                    {
                        Nombre = pedido.Pasajero.Nombre,
                        Empresa = servicio.Empresa,
                        Telefono = pedido.Pasajero.Telefono,
                        Codigo = us.Codigo,
                        Sexo = pedido.Pasajero.Sexo
                    };
                    await ActualizarPasajero(pasajero);
                }
                else
                {
                    // El pasajero NO se encuentra en la base de datos
                    pedido.Pasajero.Empresa = servicio.Empresa;
                    await NuevoPasajeroExterno(pedido.Pasajero, usuario);

                    var ld = pedido.Lugar;
                    ld.Codcli = pedido.Pasajero.Codlan; // Asignar FK

                    // Ingresar la nueva dirección
                    await GuardarLugar(pedido.Lugar);

                    // Obtener el pasajero recién creado
                    us = await LugarPasajero(pedido.Pasajero, servicio.Empresa);
                }

                pedido.Pasajero = us;
            }

            // REGISTRAR SERVICIO
            servicio.Numpax = servicio.Listapuntos.Count.ToString();
            var servicioRegistrado = await RegistrarServicioGacela(servicio, usuario);

            // Registrar cada pedido
            foreach (var pedido in servicio.Listapuntos)
            {
                pedido.Numero = servicio.Numero;
                pedido.Servicio = servicioRegistrado;
                await RegistrarPedidoGacela(pedido);
            }

            // REGISTRAR DESTINO FINAL
            var pedidoDestino = new GPedido
            {
                Lugar = new LugarCliente { Codlugar = 4175 },
                Pasajero = new GUsuario
                {
                    Codigo = "4175",
                    Lugar = new LugarCliente { Codlugar = 4175 }
                },
                Fecha = servicio.Fecha,
                Servicio = servicioRegistrado,
                Orden = "0",
                Numero = servicio.Numero
            };
            await RegistrarPedidoGacela(pedidoDestino);

            return respuesta;
        }

        // Métodos auxiliares de validación
        private bool VerificarNuloVacio(string valor)
        {
            return string.IsNullOrWhiteSpace(valor);
        }

        private bool VerificarNuloVacioDouble(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) || !double.TryParse(valor, out _);
        }

        // Método para calcular distancia externa (similar al que ya tienes pero con nombre diferente)
        private double DistanciaExterno(double posxini, double posyini, double posx, double posy)
        {
            double posYbase = Math.PI * posxini / 180.0;
            double posXbase = Math.PI * posyini / 180.0;
            double posY = Math.PI * posx / 180.0;
            double posX = Math.PI * posy / 180.0;

            double sec1 = Math.Sin(posXbase) * Math.Sin(posX);
            double dl = Math.Abs(posYbase - posY);
            double sec2 = Math.Cos(posXbase) * Math.Cos(posX);
            double centralAngle = Math.Acos(sec1 + sec2 * Math.Cos(dl));
            double distancia = centralAngle * 6378.1;
            double d = Math.Round(distancia * 100) / 100;

            return d;
        }

        // Verificar si existe servicio activo con código externo
        private async Task<GServicio> ServicioActivoExterno(GServicio servicio)
        {
            var sql = "SELECT codservicio FROM servicio WHERE codigoexterno = @codigoexterno AND estado = 'P'";

            try
            {
                var resultado = await _doConnection.QueryFirstOrDefaultAsync(sql,
                    new { codigoexterno = servicio.Codigoexterno }, transaction: _doTransaction);

                if (resultado != null)
                {
                    return new GServicio { Codservicio = resultado.codservicio?.ToString() };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // Actualizar datos de pasajero existente
        private async Task<int> ActualizarPasajero(GUsuario pasajero)
        {
            var sql = @"UPDATE cliente SET apellidos = @apellidos, empresa = @empresa, sexo = @sexo, telefono = @telefono WHERE codcliente = @codcliente";

            var parametros = new
            {
                apellidos = pasajero.Nombre,
                empresa = pasajero.Empresa,
                sexo = pasajero.Sexo,
                telefono = pasajero.Telefono,
                codcliente = pasajero.Codigo
            };

            try
            {
                return await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // Crear nuevo pasajero externo (con usuario que lo registra)
        private async Task<int> NuevoPasajeroExterno(GUsuario pasajero, string usuario)
        {
            var sql = @"INSERT INTO cliente (apellidos, sexo, codlan, estadocuenta, codlugar, clave, empresa, codusuario, telefono) 
                VALUES (@apellidos, @sexo, @codlan, 'A', @codlugar, '123', @empresa, @codusuario, @telefono)";

            var parametros = new
            {
                apellidos = pasajero.Nombre,
                sexo = pasajero.Sexo,
                codlan = pasajero.Codlan,
                codlugar = pasajero.Codlan,
                empresa = pasajero.Empresa,
                codusuario = usuario,
                telefono = pasajero.Telefono
            };

            try
            {
                await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);

                // Registro adicional en totalserver (como en el código Java)
                var sqlServer = "INSERT INTO servermobile (loginusu, servidor, tipo) VALUES (@loginusu, 'https://do.velsat.pe:2053', 'p')";
                return await _defaultConnection.ExecuteAsync(sqlServer,
                    new { loginusu = pasajero.Codlan }, transaction: _defaultTransaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // Registrar servicio principal para Gacela
        private async Task<GServicio> RegistrarServicioGacela(GServicio servicio, string usuario)
        {
            var sql = @"INSERT INTO servicio (numero, tipo, codusuario, estado, fecha, grupo, empresa, totalpax, 
                fecplan, numeromovil, codigoexterno, codconductor, unidad) 
                VALUES (@numero, @tipo, @codusuario, 'P', @fecha, @grupo, @empresa, @totalpax, 
                @fecplan, @numeromovil, @codigoexterno, @codconductor, @unidad)";

            try
            {
                await _doConnection.ExecuteAsync(sql, new
                {
                    numero = servicio.Numero,
                    tipo = servicio.Tipo,
                    codusuario = usuario, // Necesitarás esta propiedad
                    fecha = servicio.Fecha,
                    grupo = servicio.Grupo,
                    empresa = servicio.Empresa,
                    totalpax = servicio.Numpax,
                    fecplan = servicio.Fecpreplan, // Necesitarás estas propiedades
                    numeromovil = servicio.Numero,
                    codigoexterno = servicio.Codigoexterno,
                    codconductor = servicio.Conductor?.Codigo, // Necesitarás estas propiedades
                    unidad = servicio.Unidad?.Codunidad
                }, transaction: _doTransaction);

                var selectSql = "SELECT codservicio FROM servicio ORDER BY codservicio DESC LIMIT 1";
                var codservicio = await _doConnection.QueryFirstOrDefaultAsync<string>(selectSql, transaction: _doTransaction);
                servicio.Codservicio = codservicio;

                return servicio;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // Registrar pedido individual para Gacela
        private async Task<int> RegistrarPedidoGacela(GPedido pedido)
        {
            var sql = @"INSERT INTO subservicio (codubicli, fecha, estado, codcliente, numero, codservicio, 
                categorialan, arealan, vuelo, orden, observacion) 
                VALUES (@codubicli, @fecha, 'P', @codcliente, @numero, @codservicio, 
                @categorialan, @arealan, @vuelo, @orden, @observacion)";

            var parametros = new
            {
                codubicli = pedido.Pasajero.Lugar.Codlugar,
                fecha = pedido.Fecha,
                codcliente = pedido.Pasajero.Codigo,
                numero = pedido.Numero,
                codservicio = pedido.Servicio.Codservicio,
                categorialan = pedido.Categorialan,
                arealan = pedido.Arealan,
                vuelo = pedido.Vuelo,
                orden = pedido.Orden,
                observacion = pedido.Observacion // Necesitarás esta propiedad
            };

            try
            {
                return await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /////////////////////////////////////////////
        // Método público para cancelar servicios externos
        public async Task<List<GServicio>> CancelarServicioExterno(List<GServicio> listaServicios, string usuario)
        {
            var resultado = new List<GServicio>();

            foreach (var servicio in listaServicios)
            {
                string estadoRegistro = await CancelacionServiciosExternos(servicio);
                servicio.Estado = estadoRegistro;
                resultado.Add(servicio);
            }

            return resultado;
        }

        // Método principal de cancelación
        private async Task<string> CancelacionServiciosExternos(GServicio servicio)
        {
            if (VerificarNuloVacio(servicio.Codigoexterno))
            {
                return "Datos de servicio incompletos, verificar JSON";
            }

            servicio.Estado = "C"; // Estado Cancelado

            // OBTENER CODIGO DE SERVICIO
            var servicioActivo = await ServicioActivoExterno(servicio);

            if (servicioActivo != null)
            {
                servicio.Codservicio = servicioActivo.Codservicio;

                int resultado = await ActualizarServicioGacela(servicio);
                if (resultado == 1)
                {
                    return "Servicio Cancelado";
                }
                else
                {
                    return "Error al cancelar";
                }
            }
            else
            {
                return $"El servicio con código: {servicio.Codigoexterno} No existe";
            }
        }

        // Actualizar estado del servicio
        private async Task<int> ActualizarServicioGacela(GServicio servicio)
        {
            var sql = "UPDATE servicio SET estado = @estado WHERE codservicio = @codservicio";

            var parametros = new
            {
                estado = servicio.Estado,
                codservicio = servicio.Codservicio
            };

            try
            {
                return await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /////////////////////////////////////////////////
        // Método público para cancelar pasajero externo
        public async Task<string> CancelarPasajeroExterno(GPedido pedido, string usuario)
        {
            return await CancelarPasajeroExterno(pedido);
        }

        // Método principal de cancelación de pasajero
        private async Task<string> CancelarPasajeroExterno(GPedido pedido)
        {
            if (VerificarNuloVacio(pedido.Servicio.Codigoexterno) || VerificarNuloVacio(pedido.Pasajero.Codlan))
            {
                return "Datos de servicio incompletos, verificar JSON";
            }

            // OBTENER CODIGO DE SERVICIO POR CODIGO EXTERNO
            var servicioActivo = await ServicioActivoExterno(pedido.Servicio);

            if (servicioActivo != null)
            {
                pedido.Servicio.Codservicio = servicioActivo.Codservicio;
                pedido.Estado = "C"; // Estado Cancelado

                int resultado = await ActualizarPedidoGacela(pedido);

                if (resultado == 1)
                {
                    return "Cancelación de pasajero existosa";
                }
                else
                {
                    return "Error al cancelar pasajero";
                }
            }
            else
            {
                return $"El servicio con código: {pedido.Servicio.Codigoexterno} No existe";
            }
        }

        // Actualizar estado de pedido/subservicio específico
        private async Task<int> ActualizarPedidoGacela(GPedido pedido)
        {
            var sql = @"UPDATE subservicio SET estado = @estado 
                WHERE codservicio = @codservicio 
                AND codcliente IN (SELECT codcliente FROM cliente WHERE codlan = @codlan)";

            var parametros = new
            {
                estado = pedido.Estado,
                codservicio = pedido.Servicio.Codservicio,
                codlan = pedido.Pasajero.Codlan
            };

            try
            {
                return await _doConnection.ExecuteAsync(sql, parametros, transaction: _doTransaction);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /////////////////////////////////
        // Método público para agregar pasajero externo
        public async Task<string> AgregarPasajeroExterno(GPedido pedido, string usuario)
        {
            return await AgregarPasajeroExterno(pedido);
        }

        // Método principal para agregar pasajero
        private async Task<string> AgregarPasajeroExterno(GPedido pedido)
        {
            if (VerificarNuloVacio(pedido.Servicio.Codigoexterno) ||
                VerificarNuloVacio(pedido.Pasajero.Codlan) ||
                VerificarNuloVacioDouble(pedido.Lugar.Wx) ||
                VerificarNuloVacioDouble(pedido.Lugar.Wy) ||
                VerificarNuloVacio(pedido.Lugar.Direccion) ||
                VerificarNuloVacio(pedido.Pasajero.Nombre) ||
                VerificarNuloVacio(pedido.Lugar.Distrito) ||
                VerificarNuloVacio(pedido.Fecha?.ToString()) ||
                VerificarNuloVacio(pedido.Orden))
            {
                return "Datos de pedido incompleto, verificar JSON";
            }

            // Buscar si el pasajero existe y su lugar de residencia
            var us = await LugarPasajero(pedido.Pasajero, pedido.Servicio.Empresa);

            // Si el pasajero se encuentra en la base de datos
            if (us != null)
            {
                double d = DistanciaExterno(
                    double.Parse(us.Lugar.Wx),
                    double.Parse(us.Lugar.Wy),
                    double.Parse(pedido.Lugar.Wx),
                    double.Parse(pedido.Lugar.Wy)
                );

                // Si la diferencia es mayor a cero es porque cambió de lugar
                if (d > 0.0)
                {
                    // Eliminar el actual lugar y guardar el nuevo lugar
                    await EliminarLugar(us.Lugar);

                    var lu = pedido.Lugar;
                    lu.Codcli = pedido.Pasajero.Codlan; // Asignar FK
                    await GuardarLugar(lu);

                    us = await LugarPasajero(pedido.Pasajero, pedido.Servicio.Empresa);
                }
            }
            else
            {
                // El pasajero NO se encuentra en la base de datos
                pedido.Pasajero.Empresa = pedido.Servicio.Empresa;
                await NuevoPasajeroExterno(pedido.Pasajero, pedido.Servicio.Usuario);

                var ld = pedido.Lugar;
                ld.Codcli = pedido.Pasajero.Codlan; // Asignar FK

                // Ingresar la nueva dirección
                await GuardarLugar(pedido.Lugar);

                // Obtener el pasajero recién creado
                us = await LugarPasajero(pedido.Pasajero, pedido.Servicio.Empresa);
            }

            pedido.Pasajero = us;

            // Obtener el servicio activo
            var servicioActivo = await ServicioActivoExterno(pedido.Servicio);
            pedido.Servicio = servicioActivo;

            int resultado = await RegistrarPedidoGacela(pedido);

            if (resultado == 1)
            {
                return "Se agrego pasajero de forma existosa";
            }
            else
            {
                return "Error al agregar pasajero";
            }
        }

        /////////////////////////////////
        // Método público para actualizar/guardar pasajero externo
        public async Task<List<GUsuario>> ActualizarPasajeroExterno(List<GUsuario> listaClientes, string usuario)
        {
            var resultado = new List<GUsuario>();

            foreach (var cliente in listaClientes)
            {
                string estadoRegistro = await GuardarPasajeroExterno(cliente, usuario);
                cliente.Observacion = estadoRegistro;
                resultado.Add(cliente);
            }

            return resultado;
        }

        // Método principal para guardar/actualizar pasajero externo
        private async Task<string> GuardarPasajeroExterno(GUsuario us, string usuario)
        {
            // Validaciones
            if (VerificarNuloVacio(us.Nombre))
            {
                return "Error: Debe ingresar nombre";
            }

            if (VerificarNuloVacio(us.Codlan))
            {
                return "Error: Debe ingresar codigo";
            }

            if (VerificarNuloVacio(us.Empresa))
            {
                return "Error: Debe indicar la empresa del cliente";
            }

            if (us.Lugar == null)
            {
                return "Error: Debe indicar la dirección del cliente";
            }
            else
            {
                if (VerificarNuloVacio(us.Lugar.Direccion))
                {
                    return "Error: Debe indicar la dirección del cliente";
                }

                if (VerificarNuloVacioDouble(us.Lugar.Wx))
                {
                    return "Error: Debe indicar coordenadas de la direccion";
                }

                if (VerificarNuloVacioDouble(us.Lugar.Wy))
                {
                    return "Error: Debe indicar coordenadas de la direccion";
                }
            }

            // Verificar si el código existe
            var usuarioEncontrado = await BuscarPorCodlan(us);

            if (usuarioEncontrado == null)
            {
                // Guardar el lugar
                var lugar = us.Lugar;
                lugar.Codcli = us.Codlan;
                await GuardarLugar(lugar);

                // Guardar nuevo pasajero
                await NuevoPasajeroExterno(us, usuario);

                return "Cliente registrado";
            }
            else
            {
                return "Error: codigo de pasajero ya existe";
            }
        }

        // Buscar usuario por código LAN
        private async Task<GUsuario> BuscarPorCodlan(GUsuario usuario)
        {
            var sql = @"SELECT codcliente, nombres, apellidos, login, clave, codlan, sexo 
                FROM cliente 
                WHERE codlan = @Codlan AND estadocuenta = 'A'";

            try
            {
                var resultado = await _doConnection.QueryFirstOrDefaultAsync(sql,
                    new { Codlan = usuario.Codlan }, transaction: _doTransaction);

                if (resultado != null)
                {
                    return new GUsuario
                    {
                        Codigo = resultado.codcliente?.ToString(),
                        Nombre = resultado.nombres?.ToString(),
                        Apepate = resultado.apellidos?.ToString(),
                        Login = resultado.login?.ToString(),
                        Clave = resultado.clave?.ToString(),
                        Codlan = resultado.codlan?.ToString(),
                        Sexo = resultado.sexo?.ToString()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}