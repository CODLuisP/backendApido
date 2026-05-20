using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class GenerarMesRequest
    {
        public int IdConductor { get; set; }
        public string HoraInicio { get; set; }
        public string? Turno { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
    }
}
