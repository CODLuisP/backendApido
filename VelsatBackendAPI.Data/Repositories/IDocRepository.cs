using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Documentacion;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IDocRepository
    {
        //----------------------------------UNIDAD--------------------------------------------------//
        Task<List<Docunidad>> GetByDeviceID(string deviceID);
        Task<int> Create(Docunidad docunidad);
        Task<bool> DeleteUnidad(int id);
        Task<List<Docunidad>> GetDocumentosUnidadProximosVencer(string usuario);


        //----------------------------------CONDUCTOR--------------------------------------------------//
        Task<List<Docconductor>> GetByCodtaxi(int codtaxi);
        Task<int> Create(Docconductor docconductor);
        Task<bool> DeleteConductor(int id);
        Task<List<Docconductor>> GetDocumentosConductorProximosVencer(string usuario);
        Task<Usuario> GetDetalleConductor(string codtaxi);

    }
}
