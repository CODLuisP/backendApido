using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Latam
{
    public class ServicioLatam
    {
        public string Idunico { get; set; }
        public string Tipo { get; set; }
        public string? Codusuario { get; set; }
        public string Estado { get; set; }
        public string Fecha { get; set; }
        public string Grupo { get; set; }
        public string Empresa { get; set; }
        public string Pasajeros { get; set; }
        public List<SubservicioLatam> Subservicios { get; set; }
    }
}