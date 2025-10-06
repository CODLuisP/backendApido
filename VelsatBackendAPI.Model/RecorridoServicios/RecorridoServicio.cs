using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.RecorridoServicios
{
    public class RecorridoServicio
    {
        public int Codservicio { get; set; }
        public int Numero { get; set; }
        public char Tipo { get; set; }
        public string Unidad { get; set; }
        public string Empresa {  get; set; }
        public string Fechaini { get; set; }
        public string Fechafin { get; set; }
    }
}
