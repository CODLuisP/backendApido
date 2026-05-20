using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class HorarioCalendarioRequest
    {
        public int IdConductor { get; set; }
        public string Fecha { get; set; }       // "dd/MM/yyyy"
        public string HoraInicio { get; set; }  // "HH:mm"
        public string? Turno { get; set; }
        public string Tipo { get; set; }          // 'N', 'T', 'P'
        public bool AplicarDesdeAqui { get; set; } // true = cambia de esta fecha en adelante
    }
}
