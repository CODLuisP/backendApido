using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Data.Services;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository UserRepository { get; }

        IDatosCargainicialService DatosCargainicialService { get; }

        IHistoricosRepository HistoricosRepository { get; }

        IKilometrosRepository KilometrosRepository { get; }

        IServidorRepository ServidorRepository { get; }

        ITurnosRepository TurnosRepository { get; }

        IPasajerosRepository PasajerosRepository { get; }

        IPreplanRepository PreplanRepository { get; }

        IAlertaRepository AlertaRepository { get; }

        IRecorridoRepository RecorridoRepository { get; }

        IKmServicioRepository KmServicioRepository { get; }

        IGacelaRepository GacelaRepository { get; }
    }
}
