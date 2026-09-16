using System;

namespace VelsatBackendAPI.Model
{
    public class Geocercas
    {
        public int Id { get; set; }
        public string AccountID { get; set; }
        public int GeofenceID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Tipo { get; set; }
        public string AreaWkt { get; set; }
        public string CoordenadasJson { get; set; }
        public string Color { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
