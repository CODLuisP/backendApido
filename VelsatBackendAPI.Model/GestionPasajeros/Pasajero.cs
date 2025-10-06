using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.GestionPasajeros
{
    public class Pasajero
    {
        public string Codlan { get; set; }
        public string Apellidos { get; set; }
        public string? Telefono { get; set; }
        public char? Sexo { get; set; }
        public string? Empresa { get; set; }
        public string? Codusuario { get; set; }
        public string? Zona { get; set; }
        public string Direccion { get; set; }
        public string Distrito { get; set; }
        public string Wy { get; set; }
        public string Wx { get; set; }
    }
}
