using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.GestionPasajeros;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class GPedido
    {
        public int? Id { get; set; }
        public int? Codigo { get; set; }
        public string? Codcliente { get; set; }
        public string? Codlugar { get; set; }
        public string? Nombre { get; set; }
        public string? Rol { get; set; }
        public string? Fecha { get; set; }
        public string? Fecplan { get; set; }
        public string? Area { get; set; }
        public string? Horaprog { get; set; }
        public string? Orden { get; set; }
        public string? Numero { get; set; }
        public string? Usuario { get; set; }
        public string? Codtarifa { get; set; }
        public string? Hora { get; set; }
        public string? Tipo { get; set; }
        public string? Arealan { get; set; }
        public string? Arealatam { get; set; }
        public string? Fecreg { get; set; }
        public double? Distancia { get; set; }
        public string? Fechaini { get; set; }
        public string? Fechafin { get; set; }
        public string? Feccancelpas { get; set; }
        public string? Fecaten { get; set; }
        public string? Formatfecrec { get; set; }
        public string? Formathorarec { get; set; }
        public string? Codconductor { get; set; }
        public string? Codunidad { get; set; }
        public string? Lastorden { get; set; }
        public string? Lastnumero { get; set; }
        public string? Empresa { get; set; }
        public int? Duracion { get; set; }
        public string? Eliminado { get; set; }
        public string? Cerrado { get; set; }
        public string? Borrado { get; set; }
        public string? Destinocodigo { get; set; }
        public string? Nomdestino { get; set; }
        public string? Destinocodlugar { get; set; }
        public string? Direccionalterna { get; set; }
        public string? Codservicio { get; set; }
        public string? Motivocambio { get; set; }
        public string? Replanorden { get; set; }
        public string? Replannumero { get; set; }
        public string? Categorialan { get; set; }
        public string? Vuelo { get; set; }
        public string? Centrocosto { get; set; }
        public string? Tarifa { get; set; }
        public string? Cargo { get; set; }
        public string? Cuenta { get; set; }
        public string? Observacion { get; set; }
        public string? Estado { get; set; }
        public string? Calificacion { get; set; }
        public GUsuario? Pasajero { get; set; }
        public LugarCliente? Lugar { get; set; }
        public GServicio? Servicio { get; set; }
    }
}
