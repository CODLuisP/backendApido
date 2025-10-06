using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.MovilProgramacion
{
    public class Unidad
    {
        public int? Id { get; set; }
        public string? Codunidad { get; set; }
        public Gps? Gps { get; set; }
        public List<Despacho>? Listadespachos { get; set; }
        public List<Gps>? Historico { get; set; }
        public Usuario? Conductor { get; set; }
        public Usuario? Cobrador { get; set; }

    }
}
