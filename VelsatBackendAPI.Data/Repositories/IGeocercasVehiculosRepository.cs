using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IGeocercasVehiculosRepository
    {
        Task<IEnumerable<GeocercasVehiculos>> GetByGeocerca(int idGeocerca);
        Task<IEnumerable<Geocercas>> GetGeocercasByDevice(string deviceID);
        Task<string> Vincular(int idGeocerca, IEnumerable<string> deviceIds);
        Task<string> Desvincular(int idGeocerca, string deviceID);
    }
}
