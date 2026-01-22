using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using VelsatBackendAPI.Data.Services;

namespace VelsatBackendAPI.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly string _defaultConnectionString;
        private readonly string _secondConnectionString;
        private readonly string _doConnectionString;

        private MySqlConnection _defaultConnection;
        private MySqlTransaction _defaultTransaction;

        private MySqlConnection _secondConnection;
        private MySqlTransaction _secondTransaction;

        private MySqlConnection _doConnection;
        private MySqlTransaction _doTransaction;

        // ✅ Usar Lazy<T> para thread-safety sin locks manuales
        private readonly Lazy<IUserRepository> _userRepository;
        private readonly Lazy<IDatosCargainicialService> _datosCargaInicialService;
        private readonly Lazy<IHistoricosRepository> _historicosRepository;
        private readonly Lazy<IKilometrosRepository> _kilometrosRepository;
        private readonly Lazy<IServidorRepository> _servidorRepository;
        private readonly Lazy<ITurnosRepository> _turnosRepository;
        private readonly Lazy<IPasajerosRepository> _pasajerosRepository;
        private readonly Lazy<IPreplanRepository> _preplanRepository;
        private readonly Lazy<IAlertaRepository> _alertaRepository;
        private readonly Lazy<IRecorridoRepository> _recorridoRepository;
        private readonly Lazy<IKmServicioRepository> _kmServicioRepository;
        private readonly Lazy<IGacelaRepository> _gacelaRepository;
        private readonly Lazy<ITalmaRepository> _talmaRepository;
        private readonly Lazy<IAdminRepository> _adminRepository;

        private bool _disposed = false;
        private bool _committed = false;
        private readonly object _lockObject = new object();

        public UnitOfWork(MySqlConfiguration configuration)
        {
            _defaultConnectionString = configuration.DefaultConnection
                ?? throw new ArgumentNullException(nameof(configuration.DefaultConnection));
            _secondConnectionString = configuration.SecondConnection
                ?? throw new ArgumentNullException(nameof(configuration.SecondConnection));
            _doConnectionString = configuration.DOConnection
            ?? throw new ArgumentNullException(nameof(configuration.DOConnection));


            // ✅ Inicializar Lazy para cada repositorio
            _userRepository = new Lazy<IUserRepository>(() =>
                new UserRepository(DefaultConnection, _defaultTransaction));

            _datosCargaInicialService = new Lazy<IDatosCargainicialService>(() =>
                new DatosCargainicialService(DefaultConnection, _defaultTransaction));

            _servidorRepository = new Lazy<IServidorRepository>(() =>
                new ServidorRepository(DefaultConnection, _defaultTransaction));

            _turnosRepository = new Lazy<ITurnosRepository>(() =>
                new TurnosRepository(DOConnection, _doTransaction));

            _pasajerosRepository = new Lazy<IPasajerosRepository>(() =>
                new PasajerosRepository(DefaultConnection, _defaultTransaction, DOConnection, _doTransaction));

            _preplanRepository = new Lazy<IPreplanRepository>(() =>
                new PreplanRepository(DefaultConnection, _defaultTransaction, DOConnection, _doTransaction));

            _alertaRepository = new Lazy<IAlertaRepository>(() =>
                new AlertaRepository(DefaultConnection, _defaultTransaction));

            _recorridoRepository = new Lazy<IRecorridoRepository>(() => new RecorridoRepository(DOConnection, _doTransaction));

            _gacelaRepository = new Lazy<IGacelaRepository>(() =>
                new GacelaRepository(DefaultConnection, _defaultTransaction, DOConnection, _doTransaction));

            _talmaRepository = new Lazy<ITalmaRepository>(() => new TalmaRepository(DOConnection, _doTransaction));

            // Repositorios con ambas conexiones
            _historicosRepository = new Lazy<IHistoricosRepository>(() =>
                new HistoricosRepository(DefaultConnection, SecondConnection, _defaultTransaction, _secondTransaction));

            _kilometrosRepository = new Lazy<IKilometrosRepository>(() =>
                new KilometrosRepository(DefaultConnection, SecondConnection, _defaultTransaction, _secondTransaction));

            _kmServicioRepository = new Lazy<IKmServicioRepository>(() =>
                new KmServicioRepository(DefaultConnection, SecondConnection, _defaultTransaction, _secondTransaction, DOConnection, _doTransaction));

            //ADMIN
            _adminRepository = new Lazy<IAdminRepository>(() => new AdminRepository(DefaultConnection, _defaultTransaction));
        }

        // ✅ Conexión principal con inicialización thread-safe y retry logic
        private MySqlConnection DefaultConnection
        {
            get
            {
                ValidateNotDisposedOrCommitted();

                if (_defaultConnection == null)
                {
                    lock (_lockObject)
                    {
                        if (_defaultConnection == null)
                        {
                            // ✅ CAMBIO: Usar método con retry
                            _defaultConnection = OpenConnectionWithRetry(
                                _defaultConnectionString,
                                "DEFAULT (con transacción)");

                            // Iniciar transacción DESPUÉS de abrir la conexión exitosamente
                            _defaultTransaction = _defaultConnection.BeginTransaction();

                            System.Diagnostics.Debug.WriteLine(
                                $"[UnitOfWork] Transacción DEFAULT iniciada");
                        }
                    }
                }
                return _defaultConnection;
            }
        }

        // ✅ Conexión secundaria con retry logic
        private MySqlConnection SecondConnection
        {
            get
            {
                ValidateNotDisposedOrCommitted();

                if (_secondConnection == null)
                {
                    lock (_lockObject)
                    {
                        if (_secondConnection == null)
                        {
                            // ✅ CAMBIO: Usar método con retry
                            _secondConnection = OpenConnectionWithRetry(
                                _secondConnectionString,
                                "SECOND (con transacción)");

                            // Iniciar transacción DESPUÉS de abrir la conexión exitosamente
                            _secondTransaction = _secondConnection.BeginTransaction();

                            System.Diagnostics.Debug.WriteLine(
                                $"[UnitOfWork] Transacción SECOND iniciada");
                        }
                    }
                }
                return _secondConnection;
            }
        }

        // ✅ NUEVA: Propiedad para conexión DO
        private MySqlConnection DOConnection
        {
            get
            {
                ValidateNotDisposedOrCommitted();

                if (_doConnection == null)
                {
                    lock (_lockObject)
                    {
                        if (_doConnection == null)
                        {
                            _doConnection = OpenConnectionWithRetry(
                                _doConnectionString,
                                "DO (con transacción)");

                            // ✅ Iniciar transacción en DO
                            _doTransaction = _doConnection.BeginTransaction();

                            System.Diagnostics.Debug.WriteLine(
                                $"[UnitOfWork] Transacción DO iniciada");
                        }
                    }
                }
                return _doConnection;
            }
        }

        // ✅ Validación mejorada
        private void ValidateNotDisposedOrCommitted()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork),
                    "No se puede usar un UnitOfWork que ya ha sido liberado. Crea una nueva instancia.");
            }

            if (_committed)
            {
                throw new InvalidOperationException(
                    "Este UnitOfWork ya fue confirmado con SaveChanges(). Crea una nueva instancia para realizar más operaciones.");
            }
        }

        /// <summary>
        /// Abre una conexión MySQL con reintentos automáticos en caso de colisión de pool.
        /// </summary>
        private MySqlConnection OpenConnectionWithRetry(
            string connectionString,
            string connectionName,
            int maxRetries = 5)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var connection = new MySqlConnection(connectionString);
                    connection.Open();

                    // ✅ CRÍTICO: Configurar charset UTF-8 inmediatamente después de abrir
                    using (var cmd = new MySqlCommand("SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"[UnitOfWork] ✅ Conexión {connectionName} " +
                        $"{connection.ServerThread} abierta con transacción" +
                        (attempt > 0 ? $" (intento {attempt + 1})" : ""));

                    return connection;
                }
                catch (ArgumentException ex) when (
                    ex.Message.Contains("An item with the same key has already been added"))
                {
                    lastException = ex;

                    System.Diagnostics.Debug.WriteLine(
                        $"[UnitOfWork] ⚠️ Pool collision detectada en {connectionName} " +
                        $"(intento {attempt + 1}/{maxRetries})");

                    if (attempt < maxRetries - 1)
                    {
                        // Backoff exponencial: 10ms, 20ms, 40ms, 80ms, 160ms
                        int delayMs = 10 * (int)Math.Pow(2, attempt);
                        System.Threading.Thread.Sleep(delayMs);

                        // ✅ CRÍTICO: Intentar limpiar el pool antes de reintentar
                        try
                        {
                            MySqlConnection.ClearPool(new MySqlConnection(connectionString));
                            System.Diagnostics.Debug.WriteLine(
                                $"[UnitOfWork] Pool {connectionName} limpiado");
                        }
                        catch
                        {
                            // Ignorar errores al limpiar
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Otros errores no relacionados con el pool - fallar inmediatamente
                    System.Diagnostics.Debug.WriteLine(
                        $"[UnitOfWork] ❌ Error abriendo {connectionName}: {ex.Message}");
                    throw;
                }
            }

            // Si llegamos aquí, fallaron todos los intentos
            throw new InvalidOperationException(
                $"No se pudo abrir la conexión {connectionName} después de {maxRetries} intentos. " +
                $"Pool de conexiones MySQL posiblemente corrupto.",
                lastException);
        }


        // ✅ Propiedades usando Lazy
        public IUserRepository UserRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _userRepository.Value;
            }
        }

        public IDatosCargainicialService DatosCargainicialService
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _datosCargaInicialService.Value;
            }
        }

        public IServidorRepository ServidorRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _servidorRepository.Value;
            }
        }

        public ITurnosRepository TurnosRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _turnosRepository.Value;
            }
        }

        public IPasajerosRepository PasajerosRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _pasajerosRepository.Value;
            }
        }

        public IPreplanRepository PreplanRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _preplanRepository.Value;
            }
        }

        public IAlertaRepository AlertaRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _alertaRepository.Value;
            }
        }

        public IRecorridoRepository RecorridoRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _recorridoRepository.Value;
            }
        }

        public ITalmaRepository TalmaRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _talmaRepository.Value;
            }
        }

        public IGacelaRepository GacelaRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _gacelaRepository.Value;
            }
        }

        public IHistoricosRepository HistoricosRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _historicosRepository.Value;
            }
        }

        public IKilometrosRepository KilometrosRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _kilometrosRepository.Value;
            }
        }

        public IKmServicioRepository KmServicioRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _kmServicioRepository.Value;
            }
        }

        public IAdminRepository AdminRepository
        {
            get
            {
                ValidateNotDisposedOrCommitted();
                return _adminRepository.Value;
            }
        }

        // ✅ SaveChanges optimizado
        public void SaveChanges()
        {
            ValidateNotDisposedOrCommitted();

            lock (_lockObject)
            {
                try
                {
                    // ✅ Commit de las 3 transacciones
                    _defaultTransaction?.Commit();
                    _secondTransaction?.Commit();
                    _doTransaction?.Commit(); // ✅ NUEVA

                    _committed = true;

                    System.Diagnostics.Debug.WriteLine(
                        "[UnitOfWork] ✅ Todas las transacciones confirmadas");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[UnitOfWork] ❌ Error en SaveChanges: {ex.Message}");

                    // ✅ Rollback de las 3 transacciones en caso de error
                    try { _defaultTransaction?.Rollback(); } catch { }
                    try { _secondTransaction?.Rollback(); } catch { }
                    try { _doTransaction?.Rollback(); } catch { } // ✅ NUEVA

                    throw;
                }
                finally
                {
                    DisposeTransactionsAndConnections();
                }
            }
        }

        // ✅ NUEVO: Método que libera transacciones Y conexiones inmediatamente
        private void DisposeTransactionsAndConnections()
        {
            // Liberar transacciones
            if (_defaultTransaction != null)
            {
                _defaultTransaction.Dispose();
                _defaultTransaction = null;
            }

            if (_secondTransaction != null)
            {
                _secondTransaction.Dispose();
                _secondTransaction = null;
            }

            if (_doTransaction != null) // ✅ NUEVA
            {
                _doTransaction.Dispose();
                _doTransaction = null;
            }

            // Cerrar y disponer conexiones
            if (_defaultConnection != null)
            {
                try
                {
                    if (_defaultConnection.State == ConnectionState.Open)
                        _defaultConnection.Close();
                    _defaultConnection.Dispose();
                }
                catch { }
                finally { _defaultConnection = null; }
            }

            if (_secondConnection != null)
            {
                try
                {
                    if (_secondConnection.State == ConnectionState.Open)
                        _secondConnection.Close();
                    _secondConnection.Dispose();
                }
                catch { }
                finally { _secondConnection = null; }
            }

            if (_doConnection != null) // ✅ NUEVA
            {
                try
                {
                    if (_doConnection.State == ConnectionState.Open)
                        _doConnection.Close();
                    _doConnection.Dispose();
                }
                catch { }
                finally { _doConnection = null; }
            }
        }

        // ✅ Dispose optimizado
        public void Dispose()
        {
            Dispose(true);
            // ✅ REMOVIDO el finalizer, así que esto ya no es necesario
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_committed)
                        {
                            try
                            {
                                if (_defaultTransaction != null && _defaultTransaction.Connection != null)
                                    _defaultTransaction.Rollback();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[UnitOfWork] Rollback default error: {ex.Message}");
                            }

                            try
                            {
                                if (_secondTransaction != null && _secondTransaction.Connection != null)
                                    _secondTransaction.Rollback();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[UnitOfWork] Rollback second error: {ex.Message}");
                            }

                            try // ✅ NUEVA
                            {
                                if (_doTransaction != null && _doTransaction.Connection != null)
                                    _doTransaction.Rollback();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[UnitOfWork] Rollback DO error: {ex.Message}");
                            }
                        }

                        DisposeTransactionsAndConnections();
                        DisposeRepositories();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UnitOfWork] Error disposing: {ex.Message}");
                    }
                    finally
                    {
                        _disposed = true;
                    }
                }
            }
        }

        // ✅ NUEVO: Liberar repositorios si implementan IDisposable
        private void DisposeRepositories()
        {
            // Solo disponer si fueron inicializados
            TryDisposeRepository(_userRepository);
            TryDisposeRepository(_datosCargaInicialService);
            TryDisposeRepository(_historicosRepository);
            TryDisposeRepository(_kilometrosRepository);
            TryDisposeRepository(_servidorRepository);
            TryDisposeRepository(_turnosRepository);
            TryDisposeRepository(_pasajerosRepository);
            TryDisposeRepository(_preplanRepository);
            TryDisposeRepository(_alertaRepository);
            TryDisposeRepository(_recorridoRepository);
            TryDisposeRepository(_kmServicioRepository);
            TryDisposeRepository(_gacelaRepository);
            TryDisposeRepository(_adminRepository);
        }

        private void TryDisposeRepository<T>(Lazy<T> lazyRepo)
        {
            if (lazyRepo != null && lazyRepo.IsValueCreated && lazyRepo.Value is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // Ignorar errores al disponer repositorios
                }
            }
        }

    }
}