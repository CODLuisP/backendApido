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

        Task<IEnumerable<PedidoTalma>> GetPreplanTalma(string tipo, string fecha, string hora);

    }
}
