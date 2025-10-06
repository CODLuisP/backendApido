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
        Task<List<SelectedServicio>> GetSelectServicio(string fecha);

        Task<RecorridoServicio> GetDatosServicio (string fecha, string numero);

        Task<List<PasajeroServicio>> GetPasajerosServicio (string codservicio);
    }
}
