using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Services;

namespace VelsatBackendAPI.Data.Repositories
{
    /// <summary>
    /// Interfaz para operaciones de solo lectura (sin transacciones)
    /// </summary>
    public interface IReadOnlyUnitOfWork : IDisposable
    {
        IDatosCargainicialService DatosCargainicialService { get; }

        IServidorRepository ServidorRepository { get; }

        IHistoricosRepository HistoricosRepository { get; }

        IKilometrosRepository KilometrosRepository { get; }

        IKmServicioRepository KmServicioRepository { get; }

        IRecorridoRepository RecorridoRepository { get; } 

        IUserRepository UserRepository { get; }

        IGacelaRepository GacelaRepository { get; }

        IPasajerosRepository PasajerosRepository { get; }

        IPreplanRepository PreplanRepository { get; }

        ITurnosRepository TurnosRepository { get; }

        ITalmaRepository TalmaRepository { get; }
    }
}