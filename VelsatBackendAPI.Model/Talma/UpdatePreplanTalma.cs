using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Talma
{
    public class UpdatePreplanTalma
    {
        public int Codigo { get; set; }
        public string Horaprog { get; set; }
        public string Orden { get; set; }
        public string Grupo { get; set; }
        public string? Codconductor { get; set; }
        public string? Codunidad { get; set; }
        public string? Destinocodigo { get; set; }
        public string Destinocodlugar { get; set; }
    }
}
