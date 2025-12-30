using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Model.Talma
{
    public class PedidoTalma
    {
        public string Codcliente { get; set; }
        public string Codlan { get; set; }
        public string Nombre { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Tipo { get; set; }
        public string Fecreg { get; set; }
        public double? Distancia { get; set; }
        public string? Horaprog { get; set; }
        public string? Orden { get; set; }
        public string? Grupo { get; set; }
        public string? Codconductor { get; set; }
        public string? Codunidad { get; set; }
        public string Usuario { get; set; }
        public string Empresa { get; set; }
        public string Eliminado { get; set; }
        public string Cerrado { get; set; }
        public string? Destinocodigo { get; set; } //Por defecto es el aeropuerto
        public string? Destinocodlugar { get; set; } //Dirección del pasajero
        public string? Direccionalterna { get; set; }
        public string? Codservicio { get; set; }
    }
}
