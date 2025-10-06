using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class GDespacho
    {
        public string? Codigo { get; set; }
        public GUnidad? Carro { get; set; }
        public GRuta? Ruta { get; set; }
        public string? Fecprog { get; set; }
        public string? Fecini { get; set; }
        public string? Fecfin { get; set; }
        public GUsuario? Conductor { get; set; }
        public string? Ultimocontrol { get; set; }
        //public List<Control>? ListaControl { get; set; }
        public string? Motivoelim { get; set; }
        public GUsuario? Cobrador { get; set; }
        public string? Fecelim { get; set; }
        public string? Estado { get; set; }
        //public List<AsignacionBoleto>? ListaBoletos { get; set; }
        public string? Boletos { get; set; }
        public string? Fecreg { get; set; }
    }
}
