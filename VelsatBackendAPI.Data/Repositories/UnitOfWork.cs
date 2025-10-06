using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Services;

namespace VelsatBackendAPI.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly IDbConnection _defaultConnection;
        private IDbTransaction _defaultTransaction;

        private readonly IDbConnection _secondConnection;
        private IDbTransaction _secondTransaction;

        private readonly IUserRepository _userRepository;
        private readonly IDatosCargainicialService _datosCargaInicialService;
        private readonly IHistoricosRepository _historicosRepository;
        private readonly IKilometrosRepository _kilometrosRepository;
        private readonly IServidorRepository _servidorRepository;
        private readonly ITurnosRepository _turnosRepository;
        private readonly IPasajerosRepository _pasajerosRepository;
        private readonly IPreplanRepository _preplanRepository;
        private readonly IAlertaRepository _alertaRepository;
        private readonly IRecorridoRepository _recorridoRepository;
        private readonly IKmServicioRepository _kmServicioRepository;
        private readonly IGacelaRepository _gacelaRepository;

        private bool _disposed = false;

        public UnitOfWork(MySqlConfiguration configuration, IConfiguration config)
        {
            _defaultConnection = new MySqlConnection(configuration.DefaultConnection);
            _defaultConnection.Open();
            _defaultTransaction = _defaultConnection.BeginTransaction();

            _secondConnection = new MySqlConnection(configuration.SecondConnection);
            _secondConnection.Open();
            _secondTransaction = _secondConnection.BeginTransaction();

            _userRepository = new UserRepository(_defaultConnection, _defaultTransaction);
            _datosCargaInicialService = new DatosCargainicialService(_defaultConnection, _defaultTransaction);
            _historicosRepository = new HistoricosRepository(_defaultConnection, _secondConnection, _secondTransaction);
            _kilometrosRepository = new KilometrosRepository(_defaultConnection, _secondConnection, _defaultTransaction, _secondTransaction);

            _servidorRepository = new ServidorRepository(_defaultConnection);
            _turnosRepository = new TurnosRepository(_defaultConnection, _defaultTransaction);
            _pasajerosRepository = new PasajerosRepository(_defaultConnection, _defaultTransaction);
            _preplanRepository = new PreplanRepository(_defaultConnection, _secondConnection, _defaultTransaction);
            _alertaRepository = new AlertaRepository(_defaultConnection, _defaultTransaction);
            _recorridoRepository = new RecorridoRepository(_defaultConnection, _defaultTransaction);
            _kmServicioRepository = new KmServicioRepository(_defaultConnection, _secondConnection, _defaultTransaction, _secondTransaction);
            _gacelaRepository = new GacelaRepository(_defaultConnection, _defaultTransaction);
        }

        public IUserRepository UserRepository => _userRepository;
        public IDatosCargainicialService DatosCargainicialService => _datosCargaInicialService;
        public IHistoricosRepository HistoricosRepository => _historicosRepository;
        public IKilometrosRepository KilometrosRepository => _kilometrosRepository;
        public IServidorRepository ServidorRepository => _servidorRepository;
        public ITurnosRepository TurnosRepository => _turnosRepository;
        public IPasajerosRepository PasajerosRepository => _pasajerosRepository;
        public IPreplanRepository PreplanRepository => _preplanRepository;
        public IAlertaRepository AlertaRepository => _alertaRepository;
        public IRecorridoRepository RecorridoRepository => _recorridoRepository;
        public IKmServicioRepository KmServicioRepository => _kmServicioRepository;
        public IGacelaRepository GacelaRepository => _gacelaRepository;

        public void SaveChanges()
        {
            try
            {
                _defaultTransaction?.Commit();
                _secondTransaction?.Commit();
            }
            catch
            {
                // Si hay error, hacer rollback
                _defaultTransaction?.Rollback();
                _secondTransaction?.Rollback();
                throw;
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
                try
                {
                    // Primero hacer rollback si las transacciones siguen activas
                    if (_defaultTransaction != null)
                    {
                        _defaultTransaction.Rollback();
                        _defaultTransaction.Dispose();
                        _defaultTransaction = null;
                    }

                    if (_secondTransaction != null)
                    {
                        _secondTransaction.Rollback();
                        _secondTransaction.Dispose();
                        _secondTransaction = null;
                    }

                    // Luego cerrar y liberar las conexiones
                    if (_defaultConnection != null)
                    {
                        if (_defaultConnection.State == ConnectionState.Open)
                        {
                            _defaultConnection.Close();
                        }
                        _defaultConnection.Dispose();
                    }

                    if (_secondConnection != null)
                    {
                        if (_secondConnection.State == ConnectionState.Open)
                        {
                            _secondConnection.Close();
                        }
                        _secondConnection.Dispose();
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

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}