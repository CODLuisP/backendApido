using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Talma
{
    public class InsertPedidoTalmaResponse
    {
        public bool Success { get; set; }
        public List<ListaErroresTalma> Errores { get; set; }
    }
}
