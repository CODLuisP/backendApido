using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class CopiarMesTodosRequest
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string Codusuario { get; set; }
    }
}
