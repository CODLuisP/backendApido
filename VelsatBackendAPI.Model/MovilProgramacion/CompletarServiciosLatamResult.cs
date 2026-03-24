using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class CompletarServiciosLatamResult
    {
        public int Total { get; set; }
        public List<RegistroLatam> NoEncontrados { get; set; } = new();
    }
}
