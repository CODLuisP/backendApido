using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class Ggeocerca
    {
        public string? Codgeocerca { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Observacion { get; set; }
        //public List<Detallegeocerca>? Detalle { get; set; }
        public string? Alarma { get; set; }
        public string? Radio { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public GUnidad? Unidad { get; set; }
        public string? Tiempogeo { get; set; }
        public string? Fechageo { get; set; }
        public string? Forfecha { get; set; }
        public string? Forhora { get; set; }
        public double? Recorrido { get; set; }
        public double? Recoracumulado { get; set; }
        public double? Distgeo { get; set; }
        public string? Timeentregeo { get; set; }
    }
}
