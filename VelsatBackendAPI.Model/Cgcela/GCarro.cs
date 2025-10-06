using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Model.Cgcela
{
    public class GCarro
    {
        public string? Codunidad { get; set; }
        //public string? Descripcion { get; set; }
        public Ggps? Gps { get; set; }
        //public string? Modelo { get; set; }
        //public string? Marca { get; set; }
        //public string? Color { get; set; }
        //public string? Ano { get; set; }
        //public Mantenimiento? Mantenimiento { get; set; }
        //public Geocerca? Geocerca { get; set; }
        //public Grupo? Grupo { get; set; }
        //public string? Agente { get; set; }
        public double? Distancia { get; set; }
        //public Ubicacion? Ubicacion { get; set; }
        //public int Numubi { get; set; }
        //public List<Gps>? Historico { get; set; }
        //public Parada? Parada { get; set; }
        //public List<Gps>? Alarmas { get; set; }
        public string? Tipo { get; set; }
        //public Servicio? Servicio { get; set; }
        //public List<object>? ListaServicios { get; set; }
        //public List<GDespacho>? ListaDespachos { get; set; }
        //public GUsuario? Conductor { get; set; }
        public string? Habilitado { get; set; }
        public string? RutaDefault { get; set; }
        public bool EsUnidadBase { get; set; } = false;

    }
}
