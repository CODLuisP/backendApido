using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Repositories
{
    public class PasajerosRepository : IPasajerosRepository
    {
        private readonly IDbConnection _defaultConnection;
        private readonly IDbTransaction _defaultTransaction;

        public PasajerosRepository(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
        {
            _defaultConnection = defaultConnection;
            _defaultTransaction = defaultTransaction;
        }

        public async Task<IEnumerable<CodNomPas>> GetCodigo()
        {
            const string sql = "SELECT codcliente, apellidos FROM cliente WHERE estadocuenta = 'A'";
            return await _defaultConnection.QueryAsync<CodNomPas>(sql, transaction: _defaultTransaction);
        }

        public async Task<IEnumerable<Pasajero>> GetPasajero(int codcliente)
        {
            const string sql = @"
                SELECT c.codlan, c.apellidos, c.telefono, c.sexo, c.empresa, c.codusuario, 
                       z.zona, l.direccion, l.distrito, l.wy, l.wx 
                FROM cliente c 
                JOIN lugarcliente l ON c.codlugar = l.codcli 
                LEFT JOIN zonificacion z ON l.zona = z.codigo 
                WHERE c.codcliente = @Codcliente AND l.estado = 'A' 
                ORDER BY l.codlugar DESC 
                LIMIT 1";

            return await _defaultConnection.QueryAsync<Pasajero>(
                sql,
                new { Codcliente = codcliente },
                transaction: _defaultTransaction);
        }

        public async Task<IEnumerable<Tarifa>> GetTarifa(string codusuario)
        {
            const string sql = "SELECT codigo, zona FROM zonificacion WHERE codusuario = @Codusuario";

            string targetUser = (codusuario == "movilbus" || codusuario == "cgodoys" || codusuario == "wilbarrientos")
                ? "jperiche"
                : codusuario;

            return await _defaultConnection.QueryAsync<Tarifa>(
                sql,
                new { Codusuario = targetUser },
                transaction: _defaultTransaction);
        }

        public async Task<string> InsertPasajero(Pasajero pasajero, string codusuario)
        {
            const string sqlUno = @"
                INSERT INTO cliente(codlugar, apellidos, clave, estadocuenta, codusuario, codlan, sexo, empresa, telefono) 
                VALUES (@Codlugar, @Apellidos, 123, 'A', @Codusuario, @Codlan, @Sexo, @Empresa, @Telefono)";

            const string sqlDos = @"
                INSERT INTO serverprueba(loginusu, servidor) 
                VALUES (@Loginusu, 'https://velsat.pe:8586')";

            const string sqlTres = @"
                INSERT INTO lugarcliente(codcli, direccion, distrito, wy, wx, estado, zona) 
                VALUES (@Codcli, @Direccion, @Distrito, @Wy, @Wx, 'A', @Zona)";

            var parametersUno = new
            {
                Codlugar = pasajero.Codlan,
                Apellidos = pasajero.Apellidos,
                Codusuario = codusuario,
                Codlan = pasajero.Codlan,
                Sexo = pasajero.Sexo,
                Empresa = pasajero.Empresa,
                Telefono = pasajero.Telefono
            };

            var parametersDos = new { Loginusu = pasajero.Codlan };

            var parametersTres = new
            {
                Codcli = pasajero.Codlan,
                Direccion = pasajero.Direccion,
                Distrito = pasajero.Distrito,
                Wy = pasajero.Wy,
                Wx = pasajero.Wx,
                Zona = pasajero.Zona
            };

            // ✅ Ejecutar las 3 queries - NO hacer commit aquí
            await _defaultConnection.ExecuteAsync(sqlUno, parametersUno, _defaultTransaction);
            await _defaultConnection.ExecuteAsync(sqlDos, parametersDos, _defaultTransaction);
            await _defaultConnection.ExecuteAsync(sqlTres, parametersTres, _defaultTransaction);

            return "Success insertion";
        }

        public async Task<string> UpdatePasajero(Pasajero pasajero, string codusuario, int codcliente, string codlan)
        {
            const string sqlUno = @"
                UPDATE cliente 
                SET apellidos = @Apellidos, codusuario = @Codusuario, sexo = @Sexo, 
                    empresa = @Empresa, telefono = @Telefono 
                WHERE codcliente = @Codcliente";

            const string sqlDos = @"
                UPDATE lugarcliente 
                SET direccion = @Direccion, distrito = @Distrito, wy = @Wy, wx = @Wx, zona = @Zona 
                WHERE codcli = @Codcli AND estado = 'A'";

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

            // ✅ Ejecutar las 2 queries - NO hacer commit aquí
            await _defaultConnection.ExecuteAsync(sqlUno, parametersUno, _defaultTransaction);
            await _defaultConnection.ExecuteAsync(sqlDos, parametersDos, _defaultTransaction);

            return "Success update";
        }

        public async Task<string> DeletePasajero(int codcliente, string codusuario)
        {
            string fechaelim = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            const string sql = @"
                UPDATE cliente 
                SET estadocuenta = 'E', codelimina = @Codelimina, fechaelim = @Fechaelim 
                WHERE codcliente = @Codcliente";

            // ✅ NO hacer commit aquí
            await _defaultConnection.ExecuteAsync(
                sql,
                new { Codcliente = codcliente, Codelimina = codusuario, Fechaelim = fechaelim },
                _defaultTransaction);

            return "Success delete";
        }

        public async Task<List<Usuario>> GetPasajerosCodigo(string codlan)
        {
            const string sql = @"
                SELECT l.codlugar, c.codcliente, c.nombres, c.apellidos, c.codlan, 
                       l.wy, l.wx, l.direccion, l.distrito 
                FROM cliente c, lugarcliente l 
                WHERE l.codcli = c.codlugar 
                  AND c.estadocuenta = 'A' 
                  AND l.estado = 'A' 
                  AND codlan LIKE @Codlan 
                  AND destino = '0' 
                LIMIT 10";

            var parameters = new { Codlan = $"%{codlan}%" };

            var pasajeros = await _defaultConnection.QueryAsync(sql, parameters, transaction: _defaultTransaction);

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
            const string sqlUno = @"
                INSERT INTO cliente(codlugar, apellidos, estadocuenta, codusuario, codlan, destino) 
                VALUES (@Codlugar, @Apellidos, 'A', @Codusuario, @Codlan, '1')";

            const string sqlTres = @"
                INSERT INTO lugarcliente(codcli, direccion, distrito, wy, wx, estado) 
                VALUES (@Codcli, @Direccion, @Distrito, @Wy, @Wx, 'A')";

            var parametersUno = new
            {
                Codlugar = pasajero.Codlan,
                Apellidos = pasajero.Apellidos,
                Codusuario = codusuario,
                Codlan = pasajero.Codlan
            };

            var parametersTres = new
            {
                Codcli = pasajero.Codlan,
                Direccion = pasajero.Direccion,
                Distrito = pasajero.Distrito,
                Wy = pasajero.Wy,
                Wx = pasajero.Wx
            };

            // ✅ NO hacer commit aquí
            await _defaultConnection.ExecuteAsync(sqlUno, parametersUno, _defaultTransaction);
            await _defaultConnection.ExecuteAsync(sqlTres, parametersTres, _defaultTransaction);

            return "Success insertion";
        }
    }
}