using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Talma;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface ITalmaRepository
    {
        Task<InsertPedidoTalmaResponse> InsertPedido(IEnumerable<RegistroExcel> registro);

        Task<IEnumerable<PreplanTalmaResponse>> GetPreplanTalma(string tipo, string fecha, string hora);

        Task<int> SavePreplanTalma(List<UpdatePreplanTalma> pedidos);

        Task<bool> DeletePreplanTalma(int codigo);

        Task<IEnumerable<PreplanTalmaResponse>> GetPreplanTalmaEliminados(string tipo, string fecha, string hora);

        Task<IEnumerable<string>> GetHoras(string fecha);

    }
}
