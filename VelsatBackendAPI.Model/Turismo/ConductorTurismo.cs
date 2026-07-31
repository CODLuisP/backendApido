using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Turismo
{
    // Representa los datos de conductor relevantes para turismo (tabla "taxi").
    public class ConductorTurismo
    {
        public int Codtaxi { get; set; }
        public string? Apellidos { get; set; }
        public string? Login { get; set; }
        public string? Clave { get; set; }
        public string? Sexo { get; set; }
        public string? Codusuario { get; set; }
        public string? Telefono { get; set; }
        public string? Brevete { get; set; }
        public string? Turismo { get; set; }
    }
}
