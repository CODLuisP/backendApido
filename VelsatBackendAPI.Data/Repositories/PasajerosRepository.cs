using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.MovilProgramacion;
using VelsatBackendAPI.Model.Turnos;

namespace VelsatBackendAPI.Data.Repositories
{
    public class PasajerosRepository : IPasajerosRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction;

        public PasajerosRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        public async Task<IEnumerable<CodNomPas>> GetCodigo()
        {
            return await _defaultConnection.QueryAsync<CodNomPas>("Select codcliente, apellidos FROM cliente where estadocuenta = 'A'", new { });
        }

        public async Task<IEnumerable<Pasajero>> GetPasajero(int codcliente)
        {
            const string sql = "SELECT c.codlan, c.apellidos, c.telefono, c.sexo, c.empresa, c.codusuario, z.zona, l.direccion, l.distrito, l.wy, l.wx FROM cliente c JOIN lugarcliente l ON c.codlugar = l.codcli LEFT JOIN zonificacion z ON l.zona = z.codigo WHERE c.codcliente = @Codcliente AND l.estado = 'A' ORDER BY l.codlugar DESC LIMIT 1";
            return await _defaultConnection.QueryAsync<Pasajero>(sql, new { Codcliente = codcliente });
        }

        public async Task<IEnumerable<Tarifa>> GetTarifa(string codusuario)
        {
            if (codusuario == "movilbus" || codusuario == "cgodoys" || codusuario == "wilbarrientos")
            {
                return await _defaultConnection.QueryAsync<Tarifa>("select codigo, zona from zonificacion where codusuario='jperiche'", new { });
            }
            else
            {
                return await _defaultConnection.QueryAsync<Tarifa>("select codigo, zona from zonificacion where codusuario=@Codusuario", new { Codusuario = codusuario });
            }
        }

        public async Task<string> InsertPasajero(Pasajero pasajero, string codusuario)
        {
            const string sqlUno = "insert into cliente(codlugar, apellidos, clave, estadocuenta, codusuario, codlan, sexo, empresa, telefono) values " +
                "(@Codlugar,@Apellidos,123,'A',@Codusuario,@Codlan,@Sexo,@Empresa,@Telefono)";

            const string sqlDos = "insert into serverprueba(loginusu, servidor) values (@Loginusu,'https://velsat.pe:8586')";

            const string sqlTres = "insert into lugarcliente(codcli, direccion, distrito, wy, wx, estado, zona) values " +
                "(@Codcli,@Direccion,@Distrito,@Wy,@Wx,'A',@Zona)";

            var parametersUno = new
            {
                Codlugar = pasajero.Codlan,
                Apellidos = pasajero.Apellidos,
                Codusuario = codusuario,
                Codlan = pasajero.Codlan,
                Sexo = pasajero.Sexo,
                Empresa = pasajero.Empresa,
                Telefono = pasajero.Telefono,
            };

            var parametersDos = new
            {
                LoginUsu = pasajero.Codlan
            };

            var parametersTres = new
            {
                Codcli = pasajero.Codlan,
                Direccion = pasajero.Direccion,
                Distrito = pasajero.Distrito,
                Wy = pasajero.Wy,
                Wx = pasajero.Wx,
                Zona = pasajero.Zona
            };

            try
            {
                await _defaultConnection.QueryAsync<Pasajero>(sqlUno, parametersUno, _defaultTransaction);

                await _defaultConnection.QueryAsync<Pasajero>(sqlDos, parametersDos, _defaultTransaction);

                await _defaultConnection.QueryAsync<Pasajero>(sqlTres, parametersTres, _defaultTransaction);

                _defaultTransaction.Commit();
            }
            catch (Exception ex)
            {
                _defaultTransaction.Rollback();
                return $"No se pudo insertar el pasajero: {ex.Message}";
            }

            return "Success insertion";
        }

        public async Task<string> UpdatePasajero(Pasajero pasajero, string codusuario, int codcliente, string codlan)
        {
            const string sqlUno = "update cliente set apellidos = @Apellidos, codusuario = @Codusuario, sexo = @Sexo, empresa = @Empresa, telefono = @Telefono where codcliente = @Codcliente";

            const string sqlDos = "update lugarcliente set direccion = @Direccion, distrito = @Distrito, wy = @Wy, wx = @Wx, zona = @Zona where codcli = @Codcli and estado = 'A'";

            var parametersUno = new
            {
                Apellidos = pasajero.Apellidos,
                Codusuario = codusuario,
                Sexo = pasajero.Sexo,
                Empresa = pasajero.Empresa,
                Telefono = pasajero.Telefono,
                Codcliente = codcliente
            };

            var parametersDos = new
            {
                Direccion = pasajero.Direccion,
                Distrito = pasajero.Distrito,
                Wy = pasajero.Wy,
                Wx = pasajero.Wx,
                Zona = pasajero.Zona,
                Codcli = codlan
            };

            try
            {
                await _defaultConnection.QueryAsync<Pasajero>(sqlUno, parametersUno, _defaultTransaction);

                await _defaultConnection.QueryAsync<Pasajero>(sqlDos, parametersDos, _defaultTransaction);


                _defaultTransaction.Commit();
            }
            catch (Exception ex)
            {
                _defaultTransaction.Rollback();
                return $"No se pudo actualizar el pasajero: {ex.Message}";
            }

            return "Success update";
        }

        public async Task<string> DeletePasajero(int codcliente, string codusuario)
        {
            string fechaelim = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            const string sql = @"Update cliente set estadocuenta = 'E', codelimina = @Codelimina, fechaelim = @Fechaelim where codcliente = @Codcliente";

            var result = await _defaultConnection.QueryAsync<Pasajero>(sql, new { Codcliente = codcliente, Codelimina = codusuario, Fechaelim = fechaelim}, _defaultTransaction);

            _defaultTransaction.Commit();

            return "Success delete";
        }

        public async Task<List<Usuario>> GetPasajerosCodigo(string codlan)
        {
            string sql = @"select l.codlugar, c.codcliente, c.nombres, c.apellidos, c.codlan, l.wy, l.wx, l.direccion, l.distrito from cliente c, lugarcliente l where l.codcli=c.codlugar and c.estadocuenta='A' and l.estado='A' and codlan like @Codlan and destino='0' LIMIT 10";

            var parameters = new { Codlan = $"%{codlan}%" }; // ✅ Aquí se añade el %

            var pasajeros = await _defaultConnection.QueryAsync(sql, parameters);

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

        public async Task<string> InsertDestino(Pasajero pasajero, string codusuario)
        {
            const string sqlUno = "insert into cliente(codlugar, apellidos, estadocuenta, codusuario, codlan, destino) values " +
                "(@Codlugar,@Apellidos,'A',@Codusuario,@Codlan,'1')";

            const string sqlTres = "insert into lugarcliente(codcli, direccion, distrito, wy, wx, estado) values " +
                "(@Codcli,@Direccion,@Distrito,@Wy,@Wx,'A')";

            var parametersUno = new
            {
                Codlugar = pasajero.Codlan,
                Apellidos = pasajero.Apellidos,
                Codusuario = codusuario,
                Codlan = pasajero.Codlan,
            };

            var parametersTres = new
            {
                Codcli = pasajero.Codlan,
                Direccion = pasajero.Direccion,
                Distrito = pasajero.Distrito,
                Wy = pasajero.Wy,
                Wx = pasajero.Wx,
            };

            try
            {
                await _defaultConnection.QueryAsync<Pasajero>(sqlUno, parametersUno, _defaultTransaction);

                await _defaultConnection.QueryAsync<Pasajero>(sqlTres, parametersTres, _defaultTransaction);

                _defaultTransaction.Commit();
            }
            catch (Exception ex)
            {
                _defaultTransaction.Rollback();
                return $"No se pudo insertar el pasajero: {ex.Message}";
            }

            return "Success insertion";
        }
    }
}
