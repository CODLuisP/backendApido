using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class GUbicacion
    {
        public string? Codigo { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public string? Tipo { get; set; }
        public string? Nomubi { get; set; }
        public string? Nomaltura { get; set; }
        public string? Numcuadra { get; set; }
        public string? Nomurba { get; set; }
        public string? Codubi { get; set; }
        public double? Distancia { get; set; }
        public string? Distrito { get; set; }
        public string? Dircompleta { get; set; }
    }
}
