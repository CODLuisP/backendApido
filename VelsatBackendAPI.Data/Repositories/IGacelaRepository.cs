using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Cgcela;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IGacelaRepository
    {
        Task<IEnumerable<GPedido>> GetDetalleServicios(string usuario, string fechaini, string fechafin);

        Task<IEnumerable<GServicio>> GetDuracionServicios(string usuario, string fechaini, string fechafin);

        Task<IEnumerable<GCarro>> GetUnidadesCercanas(double km, GCarro carroBase, string usuario);

        Task<List<GServicio>> GetServicios(string fechaini, string fechafin, string usuario);

        Task<List<GPedido>> ListaPasajeroServicio(string codservicio);

        Task<int> UpdateEstadoServicio(GPedido pedido);

        Task<int> NuevoSubServicioPasajero(GPedido pedido);

        Task<int> ReiniciarServicio(int codservicio);

        Task<int> GuardarServicio(GPedido pedido);

        Task<List<GServicio>> ProcessExcel(string filePath, string tipoGrupo, string usuario);

        Task<List<GServicio>> RegistrarServicioExterno(List<GServicio> listaServicios, string usuario);

        Task<List<GServicio>> CancelarServicioExterno(List<GServicio> listaServicios, string usuario);

        Task<string> CancelarPasajeroExterno(GPedido pedido, string usuario);

        Task<string> AgregarPasajeroExterno(GPedido pedido, string usuario);


    }
}
