using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IServicioActualRepository
    {
        /// <summary>
        /// Servicio vigente (programado o en curso) por cada unidad del usuario.
        /// Las unidades sin servicio vigente no aparecen en el resultado.
        /// </summary>
        Task<List<ServicioActual>> ObtenerServiciosActuales(string login);
    }
}
