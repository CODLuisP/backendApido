using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class ExcelAvianca
    {
        public int Id { get; set; }
        public string CodigoOracle { get; set; }
        public string Nombre { get; set; }
        public string Subarea { get; set; }
        public string Area { get; set; }
        public string Rol { get; set; }
        public string? Empresa { get; set; }

    }
}
