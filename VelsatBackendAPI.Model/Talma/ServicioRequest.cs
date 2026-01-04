using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Talma
{
    public class ServicioRequest
    {
        // Datos del servicio (para tabla servicio)
        public string? Numero { get; set; }
        public string Tipo { get; set; }
        public string? Unidad { get; set; }
        public string? Codconductor { get; set; }
        public string Codusuario { get; set; }
        public string? Estado { get; set; }
        public string Fecha { get; set; }
        public string? Grupo { get; set; }
        public string? Empresa { get; set; }
        public string? Totalpax { get; set; }
        public string? Numeromovil {  get; set; }

        // Lista de pasajeros/detalles (PedidoTalma)
        public List<SubservicioRequest> Subservicios { get; set; }

        public ServicioRequest()
        {
            Subservicios = new List<SubservicioRequest>();
        }
    }
}
