using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class InsertPedidoResponse
    {
        public bool Success { get; set; }
        public List<ListaErrores> Errores { get; set; }
    }
}
