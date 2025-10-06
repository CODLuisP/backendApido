using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class GUnidad
    {
        public int? Id { get; set; }
        public string? Codunidad { get; set; }
        public Ggps? Gps { get; set; }
        public List<GDespacho>? Listadespachos { get; set; }
        public List<Ggps>? Historico { get; set; }
        public GUsuario? Conductor { get; set; }
        public GUsuario? Cobrador { get; set; }
    }
}
