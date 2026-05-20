using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class ConductorHorarioCalendario
    {
        public int Id { get; set; }
        public string IdConductor { get; set; }
        public DateTime Fecha { get; set; }
        public string HoraInicio { get; set; }
        public string? Turno { get; set; }
        public char Tipo { get; set; } // 'N', 'T', 'P'
        public DateTime FechaRegistro { get; set; }
    }
}
