using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class ListaErrores
    {
        public int Item {  get; set; }
        public string? CodigoOracle {  get; set; }
        public string? Nombre { get; set; }
        public string? Subarea { get; set; }
        public string? Rol { get; set; }
        public string? Motivo { get; set; }
        public string? Archivo { get; set; }
    }
}
