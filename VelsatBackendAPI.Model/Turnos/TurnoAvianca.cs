using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Turnos
{
    public class TurnoAvianca
    {
        public string? Codigo { get;  set; }
        public string Codrl { get; set; }
        public string Hora { get; set; }
        public string Tipo { get; set; }
        public string Area { get; set; }
        public string Subarea { get; set; }
        public string? Empresa { get; set; }
        public string Programa { get; set; }
        public string? Eliminado { get; set; }
        public string? Usuario { get; set; }
    }
}
