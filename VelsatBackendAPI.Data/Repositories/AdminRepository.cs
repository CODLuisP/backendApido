using Dapper;
using System.Data;
using VelsatBackendAPI.Model.Administracion;

namespace VelsatBackendAPI.Data.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly IDbConnection _defaultConnection; IDbTransaction _defaultTransaction;

        public AdminRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
        {
            _defaultConnection = defaultconnection;
            _defaultTransaction = defaulttransaction;
        }

        public async Task<IEnumerable<Usuarioadmin>> GetAllUsers()
        {
            var sql = @"SELECT accountID, password, contactPhone, contactEmail, description, creationTime, isActive, ruc from usuarios";

            var resultado = await _defaultConnection.QueryAsync<Usuarioadmin>(sql, transaction: _defaultTransaction);

            return resultado;
        }

        public async Task<int> UpdateUser(Usuarioadmin usuario)
        {
            var sql = @"UPDATE usuarios SET password = @Password, contactPhone = @ContactPhone, contactEmail = @ContactEmail, description = @Description, ruc = @Ruc WHERE accountID = @AccountID";

            var resultado = await _defaultConnection.ExecuteAsync(sql, usuario, transaction: _defaultTransaction);

            return resultado;
        }

        public async Task<int> DeleteUser(string accountID)
        {
            var sql = @"UPDATE usuarios SET isActive = 0 WHERE accountID = @AccountID";

            var parametros = new { AccountID = accountID };

            var resultado = await _defaultConnection.ExecuteAsync(sql, parametros, transaction: _defaultTransaction);
            return resultado;
        }

        public async Task<int> InsertUser(Usuarioadmin usuario)
        {
            // Obtener timestamp Unix en hora de Perú (UTC-5)
            var peruTime = DateTime.UtcNow.AddHours(-5);
            var unixTimestamp = ((DateTimeOffset)peruTime).ToUnixTimeSeconds();

            // Asignar valores por defecto
            usuario.CreationTime = (int)unixTimestamp;
            usuario.IsActive = true; // Dapper lo convertirá a 1 en MySQL

            // 1. Insertar usuario
            var sqlUsuario = @"INSERT INTO usuarios 
                (accountID, password, contactPhone, contactEmail, description, creationTime, isActive, ruc) 
                VALUES 
                (@AccountID, @Password, @ContactPhone, @ContactEmail, @Description, @CreationTime, @IsActive, @Ruc)";

            var resultado = await _defaultConnection.ExecuteAsync(sqlUsuario, usuario, transaction: _defaultTransaction);

            // 2. Insertar en serverprueba
            var sqlServerPrueba = @"INSERT INTO serverprueba (loginusu, servidor, tipo) 
                            VALUES (@AccountID, 'https://do.velsat.pe:2083', 'n')";

            await _defaultConnection.ExecuteAsync(sqlServerPrueba, new { AccountID = usuario.AccountID }, transaction: _defaultTransaction);

            // 3. Insertar en servermobile
            var sqlServerMobile = @"INSERT INTO servermobile (loginusu, servidor, tipo) 
                            VALUES (@AccountID, 'https://velsat.pe:2087', 'n')";

            await _defaultConnection.ExecuteAsync(sqlServerMobile, new { AccountID = usuario.AccountID }, transaction: _defaultTransaction);

            return resultado;
        }

        public async Task<IEnumerable<Deviceuser>> GetSubUsers()
        {
            var sql = @"SELECT id, UserId, DeviceName, Status, DeviceID from deviceuser";

            var resultado = await _defaultConnection.QueryAsync<Deviceuser>(sql, transaction: _defaultTransaction);

            return resultado;
        }

        public async Task<int> InsertSubUser(Deviceuser usuario)
        {
            var sql = @"INSERT INTO deviceuser (id, UserId, DeviceName, Status, DeviceID) VALUES (@Id, @UserId, @DeviceName, '1', @DeviceID)";

            var resultado = await _defaultConnection.ExecuteAsync(sql, usuario, transaction: _defaultTransaction);
            return resultado;
        }

        public async Task<int> UpdateSubUser(Deviceuser usuario)
        {
            var sql = @"UPDATE deviceuser SET UserID = @UserId, DeviceName = @DeviceName, deviceID = @DeviceID WHERE id = @Id";

            var resultado = await _defaultConnection.ExecuteAsync(sql, usuario, transaction: _defaultTransaction);
            return resultado;
        }

        public async Task<int> DeleteSubUser(string id)
        {
            var sql = @"UPDATE deviceuser SET status = '0' WHERE id = @Id";

            var parametros = new { Id = id };

            var resultado = await _defaultConnection.ExecuteAsync(sql, parametros, transaction: _defaultTransaction);
            return resultado;
        }

        public async Task<IEnumerable<DeviceAdmin>> GetDevices()
        {
            var sql = @"SELECT deviceID, accountID, equipmentType, uniqueID, deviceCode, simPhoneNumber, imeiNumber, habilitada from device order by accountID";

            var resultado = await _defaultConnection.QueryAsync<DeviceAdmin>(sql, transaction: _defaultTransaction);

            return resultado;
        }

        public async Task<int> UpdateDevice(DeviceAdmin device, string oldDeviceID, string oldAccountID)
        {
            var sql = @"UPDATE device SET deviceID = @DeviceID, accountID = @AccountID, equipmentType = @EquipmentType, uniqueID = @UniqueID, deviceCode = @DeviceCode, simPhoneNumber = @SimPhoneNumber, imeiNumber = @ImeiNumber WHERE deviceID = @OldDeviceID AND accountID = @OldAccountID";

            var resultado = await _defaultConnection.ExecuteAsync(sql, new
            {
                device.DeviceID,
                device.AccountID,
                device.EquipmentType,
                device.UniqueID,
                device.DeviceCode,
                device.SimPhoneNumber,
                device.ImeiNumber,
                OldDeviceID = oldDeviceID,
                OldAccountID = oldAccountID
            }, transaction: _defaultTransaction);

            return resultado;
        }

        public async Task<int> InsertDevice(DeviceAdmin device)
        {
            var sql = @"INSERT INTO device (deviceID, accountID, equipmentType, uniqueID, deviceCode, simPhoneNumber, imeiNumber, habilitada) 
                VALUES (@DeviceID, @AccountID, @EquipmentType, @UniqueID, @DeviceCode, @SimPhoneNumber, @ImeiNumber, '1')";

            var resultado = await _defaultConnection.ExecuteAsync(sql, device, transaction: _defaultTransaction);
            return resultado;
        }
    }
}
