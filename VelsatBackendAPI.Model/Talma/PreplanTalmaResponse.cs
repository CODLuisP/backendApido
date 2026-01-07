using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Talma
{
    public class PreplanTalmaResponse
    {
        // Campos que vienen directamente del SELECT
        public string Codigo { get; set; }
        public string Codcliente { get; set; }
        public string Codlan { get; set; }
        public string Nombre { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Tipo { get; set; }
        public string Horaprog { get; set; }
        public string Orden { get; set; }
        public string Grupo { get; set; }
        public string Cerrado { get; set; }
        public string Eliminado { get; set; }
        public Conductor? Conductor { get; set; }
        public string Codunidad { get; set; }
        public string Empresa { get; set; }

        // Campos que se llenarán con los métodos privados (ahora son objetos)
        public LugarInfo Destino { get; set; }  // Obtenido con destinocodigo
        public LugarInfo DireccionPasajero { get; set; }  // Obtenido con destinocodlugar
    }
}
