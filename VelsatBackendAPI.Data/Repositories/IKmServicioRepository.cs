using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.KmServicioAremys;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IKmServicioRepository
    {
        Task<List<KilometrajeServicio>> GetKmServicios(string fecha);

    }
}
