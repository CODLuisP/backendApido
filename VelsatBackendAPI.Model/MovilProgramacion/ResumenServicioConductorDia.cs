using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class ResumenServicioConductorDia
    {
        public string CodConductor { get; set; }
        public string Conductor { get; set; }
        public string Placa { get; set; }
        public string Turno { get; set; }
        public string HoraInicioTurno { get; set; }
        public int Dia { get; set; }
        public int Cantidad { get; set; }
    }
}
