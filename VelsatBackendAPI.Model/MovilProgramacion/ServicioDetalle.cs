using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class ServicioDetalle
    {
        public string Codservicio { get; set; }
        public string Fecha { get; set; }
        public string Empresa { get; set; }
        public string Tipo { get; set; }
        public string Numero { get; set; }
        public string HoraTurno { get; set; }
        public string HoraInicio { get; set; }
        public string HoraAto { get; set; }
        public string Apellidos { get; set; } // Cliente
        public string Direccion { get; set; }
        public string Distrito { get; set; }
        public string Unidad { get; set; }
        public string ApellidosConductor { get; set; }
        public string HoraInicioTurno { get; set; }
        public string Turno { get; set; }
        public string Unidadasig { get; set; }

    }
}
