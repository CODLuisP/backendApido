using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using VelsatBackendAPI.Model.Documentacion;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Repositories
{
    public class DocRepository : IDocRepository
    {
        private readonly IDbConnection _doConnection;
        private readonly IDbTransaction _doTransaction;

        public DocRepository(IDbConnection doConnection, IDbTransaction doTransaction)
        {
            _doConnection = doConnection;
            _doTransaction = doTransaction;
        }

        //----------------------------------UNIDAD--------------------------------------------------//
        public async Task<List<Docunidad>> GetByDeviceID(string deviceID)
        {
            const string sql = @"SELECT * FROM docunidad WHERE DeviceID = @DeviceID ORDER BY Fecha_vencimiento DESC";
            try
            {
                var documentos = await _doConnection.QueryAsync<Docunidad>(sql, new { DeviceID = deviceID }, transaction: _doTransaction);
                return documentos.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener documentos de unidad por DeviceID");
                throw;
            }
        }

        public async Task<int> Create(Docunidad docunidad)
        {
            const string sql = @"INSERT INTO docunidad (DeviceID, Tipo_documento, Archivo_url, Fecha_vencimiento, Observaciones, Usuario) VALUES (@DeviceID, @Tipo_documento, @Archivo_url, @Fecha_vencimiento, @Observaciones, @Usuario); SELECT LAST_INSERT_ID();";
            try
            {
                var id = await _doConnection.ExecuteScalarAsync<int>(sql, new
                {
                    docunidad.DeviceID,
                    docunidad.Tipo_documento,
                    docunidad.Archivo_url,
                    docunidad.Fecha_vencimiento,
                    docunidad.Observaciones,
                    docunidad.Usuario
                }, transaction: _doTransaction);
                return id;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                Console.WriteLine("Error de duplicado al insertar en docunidad");
                throw new Exception("El documento de unidad ya existe en el sistema", ex);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error de MySQL al insertar en docunidad");
                throw new Exception($"Error de base de datos MySQL: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al crear documento de unidad");
                throw;
            }
        }

        public async Task<bool> DeleteUnidad(int id)
        {
            const string sql = @"DELETE FROM docunidad WHERE Id = @Id";
            try
            {
                var affectedRows = await _doConnection.ExecuteAsync(sql, new { Id = id }, transaction: _doTransaction);
                return affectedRows > 0;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error de MySQL al eliminar en docunidad");
                throw new Exception($"Error de base de datos MySQL: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al eliminar documento de unidad");
                throw;
            }
        }

        public async Task<List<Docunidad>> GetDocumentosUnidadProximosVencer(string usuario)
        {
            var fechaActual = DateTime.UtcNow.AddHours(-5).Date;
            var fechaLimite = fechaActual.AddDays(30);

            const string sql = @"SELECT * FROM docunidad WHERE Fecha_vencimiento IS NOT NULL AND Fecha_vencimiento <= @FechaLimite AND usuario = @Usuario ORDER BY Fecha_vencimiento ASC";
            try
            {
                var documentos = await _doConnection.QueryAsync<Docunidad>(sql, new
                {
                    FechaLimite = fechaLimite,
                    Usuario = usuario
                }, transaction: _doTransaction);
                return documentos.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener documentos de unidad próximos a vencer");
                return new List<Docunidad>();
            }
        }

        //----------------------------------CONDUCTOR--------------------------------------------------//
        public async Task<List<Docconductor>> GetByCodtaxi(int codtaxi)
        {
            const string sql = @"SELECT * FROM docconductor WHERE Codtaxi = @Codtaxi ORDER BY Fecha_vencimiento DESC";
            try
            {
                var documentos = await _doConnection.QueryAsync<Docconductor>(sql, new { Codtaxi = codtaxi }, transaction: _doTransaction);
                return documentos.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener documentos de conductor por Codtaxi");
                throw;
            }
        }

        public async Task<int> Create(Docconductor docconductor)
        {
            const string sql = @"INSERT INTO docconductor (Codtaxi, Tipo_documento, Archivo_url, Fecha_vencimiento, Observaciones, Usuario) VALUES (@Codtaxi, @Tipo_documento, @Archivo_url, @Fecha_vencimiento, @Observaciones, @Usuario); SELECT LAST_INSERT_ID();";
            try
            {
                var id = await _doConnection.ExecuteScalarAsync<int>(sql, new
                {
                    docconductor.Codtaxi,
                    docconductor.Tipo_documento,
                    docconductor.Archivo_url,
                    docconductor.Fecha_vencimiento,
                    docconductor.Observaciones,
                    docconductor.Usuario
                }, transaction: _doTransaction);
                return id;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                Console.WriteLine("Error de duplicado al insertar en docconductor");
                throw new Exception("El documento de conductor ya existe en el sistema", ex);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error de MySQL al insertar en docconductor");
                throw new Exception($"Error de base de datos MySQL: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al crear documento de conductor");
                throw;
            }
        }

        public async Task<bool> DeleteConductor(int id)
        {
            const string sql = @"DELETE FROM docconductor WHERE Id = @Id";
            try
            {
                var affectedRows = await _doConnection.ExecuteAsync(sql, new { Id = id }, transaction: _doTransaction);
                return affectedRows > 0;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error de MySQL al eliminar en docconductor");
                throw new Exception($"Error de base de datos MySQL: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al eliminar documento de conductor");
                throw;
            }
        }

        public async Task<List<Docconductor>> GetDocumentosConductorProximosVencer(string usuario)
        {
            var fechaActual = DateTime.UtcNow.AddHours(-5).Date;
            var fechaLimite = fechaActual.AddDays(30);

            const string sql = @"SELECT * FROM docconductor WHERE Fecha_vencimiento IS NOT NULL AND Fecha_vencimiento <= @FechaLimite AND usuario = @Usuario ORDER BY Fecha_vencimiento ASC";
            try
            {
                var documentos = await _doConnection.QueryAsync<Docconductor>(sql, new
                {
                    FechaLimite = fechaLimite,
                    Usuario = usuario
                }, transaction: _doTransaction);
                return documentos.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener documentos de conductor próximos a vencer");
                return new List<Docconductor>();
            }
        }

        public async Task<Usuario> GetDetalleConductor(string codtaxi)
        {
            if (!int.TryParse(codtaxi, out int codtaxiInt))
            {
                return null;
            }

            const string sql = @"SELECT codtaxi, nombres, apellidos, sexo, dni, telefono 
                         FROM taxi 
                         WHERE estado = 'A' AND codtaxi = @Codtaxi";
            try
            {
                var results = await _doConnection.QueryAsync<dynamic>(sql, new
                {
                    Codtaxi = codtaxiInt
                }, transaction: _doTransaction);

                var row = results.FirstOrDefault();
                if (row == null)
                {
                    return null;
                }

                return new Usuario
                {
                    Codigo = row.codtaxi.ToString(),
                    Nombre = row.nombres,
                    Apepate = row.apellidos,
                    Sexo = row.sexo,
                    Dni = row.dni,
                    Telefono = row.telefono
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener detalle del conductor");
                return null;
            }
        }
    }
}
