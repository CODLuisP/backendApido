using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.RecorridoServicios;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IRecorridoRepository
    {
        Task<List<SelectedServicio>> GetSelectServicio(string fecha, string empresa, string usuario);

        Task<RecorridoServicio> GetDatosServicio (string fecha, string numero, string empresa, string usuario);

        Task<List<PasajeroServicio>> GetPasajerosServicio (string codservicio);
    }
}
