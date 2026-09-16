using System;

namespace VelsatBackendAPI.Model
{
    public class GeocercasVehiculos
    {
        public int Id { get; set; }
        public int IdGeocerca { get; set; }
        public string DeviceID { get; set; }
        public DateTime FechaVinculacion { get; set; }
        public bool Activo { get; set; }
    }
}
