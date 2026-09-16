using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IGeocercasRepository
    {
        Task<IEnumerable<Geocercas>> GetByAccount(string accountID);
        Task<Geocercas> GetById(int id);
        Task<int> Insert(Geocercas geocerca);
        Task<string> Update(Geocercas geocerca);
        Task<string> Delete(int id);
    }
}
