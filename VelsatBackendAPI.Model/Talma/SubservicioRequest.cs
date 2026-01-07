using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Talma
{
    public class SubservicioRequest
    {
        public string Codubicli {  get; set; }
        public string Fecha { get; set; }
        public string? Estado { get; set; }
        public string Codcliente { get; set; }
        public string? Numero { get; set; }
        public string? Codservicio { get; set; }
        public string Orden {  get; set; }

        // Nuevo campo para actualizar preplan_talma
        public string Codigo { get; set; }
    }
}
