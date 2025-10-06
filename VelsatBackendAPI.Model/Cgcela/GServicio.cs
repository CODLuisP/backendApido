using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class GServicio
    {
        public string? Codservicio { get; set; }
        public string? Codigoexterno { get; set; } 
        public string? Destino { get; set; }
        public string? NomDestino { get; set; }
        public string? Empresa { get; set; }
        public string? Area { get; set; }
        public string? Nomgrupo { get; set; }
        public string? Fecha { get; set; }
        public string? Fecpreplan { get; set; }
        public string? Fecatoavianca { get; set; }
        public string? Fecparqueolatam { get; set; }
        public string? Fecatolatam { get; set; }
        public string? Fecgourmetlatam { get; set; }
        public string? Feclcclatam { get; set; }
        public string? Formathorarec { get; set; }
        public string? Fecplan { get; set; }
        public string? Fecfin { get; set; }
        public string? Formatfecato { get; set; }
        public string? Formathoraato { get; set; }
        public string? Newfechaini { get; set; }
        public string? Newfechafin { get; set; }
        public string? Fecasignacion { get; set; }
        public string? Grupo { get; set; }
        public string? Numguia { get; set; }
        public string? Estado { get; set; }
        public string? Numero { get; set; }
        public string? Numeromovil { get; set; }
        public string? Numpax { get; set; }
        public string? Tipo { get; set; }
        public string? Usuario { get; set; }
        public string? Costototal { get; set; }
        public List<GPedido>? Listapuntos { get; set; }
        public GUsuario? Conductor { get; set; }
        public Ggps? Gps { get; set; }
        public GUnidad? Unidad { get; set; }
        public GUsuario? Owner { get; set; }
        public GZona? Zona { get; set; }
    }
}
