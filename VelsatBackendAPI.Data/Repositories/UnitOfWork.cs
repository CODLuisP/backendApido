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

        private MySqlConnection _defaultConnection;
        private MySqlTransaction _defaultTransaction;

        private MySqlConnection _secondConnection;
        private MySqlTransaction _secondTransaction;

        // Repositorios lazy
        private IUserRepository _userRepository;
        private IDatosCargainicialService _datosCargaInicialService;
        private IHistoricosRepository _historicosRepository;
        private IKilometrosRepository _kilometrosRepository;
        private IServidorRepository _servidorRepository;
        private ITurnosRepository _turnosRepository;
        private IPasajerosRepository _pasajerosRepository;
        private IPreplanRepository _preplanRepository;
        private IAlertaRepository _alertaRepository;
        private IRecorridoRepository _recorridoRepository;
        private IKmServicioRepository _kmServicioRepository;
        private IGacelaRepository _gacelaRepository;

        private bool _disposed = false;
        private bool _committed = false; // ⭐ NUEVO
        private readonly object _lockObject = new object();

        public UnitOfWork(MySqlConfiguration configuration)
        {
            _defaultConnectionString = configuration.DefaultConnection
                ?? throw new ArgumentNullException(nameof(configuration.DefaultConnection));
            _secondConnectionString = configuration.SecondConnection
                ?? throw new ArgumentNullException(nameof(configuration.SecondConnection));
        }

        // Conexión principal - se abre la primera vez que se usa
        private MySqlConnection DefaultConnection
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_defaultConnection == null)
                {
                    lock (_lockObject)
                    {
                        if (_defaultConnection == null)
                        {
                            _defaultConnection = new MySqlConnection(_defaultConnectionString);
                            _defaultConnection.Open();
                            _defaultTransaction = _defaultConnection.BeginTransaction();
                        }
                    }
                }
                return _defaultConnection;
            }
        }

        // Conexión secundaria - SOLO se abre cuando se accede a repositorios históricos
        private MySqlConnection SecondConnection
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_secondConnection == null)
                {
                    lock (_lockObject)
                    {
                        if (_secondConnection == null)
                        {
                            _secondConnection = new MySqlConnection(_secondConnectionString);
                            _secondConnection.Open();
                            _secondTransaction = _secondConnection.BeginTransaction();
                        }
                    }
                }
                return _secondConnection;
            }
        }

        // ⭐ NUEVO: Validación para prevenir uso después de commit
        private void ValidateNotCommitted()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork),
                    "No se puede usar un UnitOfWork que ya ha sido liberado.");
            }

            if (_committed)
            {
                throw new InvalidOperationException(
                    "Este UnitOfWork ya fue confirmado. Crea una nueva instancia para realizar más operaciones.");
            }
        }

        // Repositorios que SOLO usan la conexión principal
        public IUserRepository UserRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_userRepository == null)
                {
                    _userRepository = new UserRepository(DefaultConnection, _defaultTransaction);
                }
                return _userRepository;
            }
        }

        public IDatosCargainicialService DatosCargainicialService
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_datosCargaInicialService == null)
                {
                    _datosCargaInicialService = new DatosCargainicialService(DefaultConnection, _defaultTransaction);
                }
                return _datosCargaInicialService;
            }
        }

        public IServidorRepository ServidorRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_servidorRepository == null)
                {
                    _servidorRepository = new ServidorRepository(DefaultConnection, _defaultTransaction);
                }
                return _servidorRepository;
            }
        }

        public ITurnosRepository TurnosRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_turnosRepository == null)
                {
                    _turnosRepository = new TurnosRepository(DefaultConnection, _defaultTransaction);
                }
                return _turnosRepository;
            }
        }

        public IPasajerosRepository PasajerosRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_pasajerosRepository == null)
                {
                    _pasajerosRepository = new PasajerosRepository(DefaultConnection, _defaultTransaction);
                }
                return _pasajerosRepository;
            }
        }

        public IPreplanRepository PreplanRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_preplanRepository == null)
                {
                    _preplanRepository = new PreplanRepository(DefaultConnection, _defaultTransaction);
                }
                return _preplanRepository;
            }
        }

        public IAlertaRepository AlertaRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_alertaRepository == null)
                {
                    _alertaRepository = new AlertaRepository(DefaultConnection, _defaultTransaction);
                }
                return _alertaRepository;
            }
        }

        public IRecorridoRepository RecorridoRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_recorridoRepository == null)
                {
                    _recorridoRepository = new RecorridoRepository(DefaultConnection, _defaultTransaction);
                }
                return _recorridoRepository;
            }
        }

        public IGacelaRepository GacelaRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_gacelaRepository == null)
                {
                    _gacelaRepository = new GacelaRepository(DefaultConnection, _defaultTransaction);
                }
                return _gacelaRepository;
            }
        }

        // Repositorios que usan AMBAS conexiones (históricos)
        public IHistoricosRepository HistoricosRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_historicosRepository == null)
                {
                    _historicosRepository = new HistoricosRepository(
                        DefaultConnection,
                        SecondConnection,
                        _defaultTransaction,
                        _secondTransaction);
                }
                return _historicosRepository;
            }
        }

        public IKilometrosRepository KilometrosRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_kilometrosRepository == null)
                {
                    _kilometrosRepository = new KilometrosRepository(
                        DefaultConnection,
                        SecondConnection,
                        _defaultTransaction,
                        _secondTransaction);
                }
                return _kilometrosRepository;
            }
        }

        public IKmServicioRepository KmServicioRepository
        {
            get
            {
                ValidateNotCommitted(); // ⭐ NUEVO

                if (_kmServicioRepository == null)
                {
                    _kmServicioRepository = new KmServicioRepository(
                        DefaultConnection,
                        SecondConnection,
                        _defaultTransaction,
                        _secondTransaction);
                }
                return _kmServicioRepository;
            }
        }

        public void SaveChanges()
        {
            ValidateNotCommitted(); // ⭐ NUEVO

            lock (_lockObject)
            {
                try
                {
                    _defaultTransaction?.Commit();
                    _secondTransaction?.Commit();

                    _committed = true; // ⭐ NUEVO: Marca como confirmado
                }
                catch
                {
                    _defaultTransaction?.Rollback();
                    _secondTransaction?.Rollback();
                    throw;
                }
                finally
                {
                    // Limpiar transacciones después de commit/rollback
                    DisposeTransactions(); // ⭐ NUEVO: Método extraído

                    // ⭐ NUEVO: Cerrar conexiones inmediatamente para liberar recursos
                    CloseConnections();
                }
            }
        }

        // ⭐ NUEVO: Método para liberar transacciones
        private void DisposeTransactions()
        {
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
        }

        // ⭐ NUEVO: Método para cerrar conexiones inmediatamente
        private void CloseConnections()
        {
            if (_defaultConnection != null && _defaultConnection.State == ConnectionState.Open)
            {
                _defaultConnection.Close();
            }

            if (_secondConnection != null && _secondConnection.State == ConnectionState.Open)
            {
                _secondConnection.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lockObject)
                {
                    try
                    {
                        // ⭐ MODIFICADO: Solo hacer rollback si NO se hizo commit
                        if (!_committed)
                        {
                            if (_defaultTransaction != null)
                            {
                                try { _defaultTransaction.Rollback(); } catch { }
                            }

                            if (_secondTransaction != null)
                            {
                                try { _secondTransaction.Rollback(); } catch { }
                            }
                        }

                        // Liberar transacciones
                        DisposeTransactions();

                        // Cerrar y liberar conexiones
                        if (_defaultConnection != null)
                        {
                            if (_defaultConnection.State == ConnectionState.Open)
                            {
                                _defaultConnection.Close();
                            }
                            _defaultConnection.Dispose();
                            _defaultConnection = null;
                        }

                        if (_secondConnection != null)
                        {
                            if (_secondConnection.State == ConnectionState.Open)
                            {
                                _secondConnection.Close();
                            }
                            _secondConnection.Dispose();
                            _secondConnection = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing UnitOfWork: {ex.Message}");
                    }
                    finally
                    {
                        _disposed = true;
                    }
                }
            }
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}