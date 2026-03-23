using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class CompletarServiciosLatamRequest
    {
        public List<RegistroLatam> Registros { get; set; }
        public string Fecha { get; set; }
        public string CodUsuario { get; set; }
    }
}
