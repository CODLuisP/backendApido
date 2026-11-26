namespace VelsatBackendAPI.Model.Telemetria
{
    // Conductor de telemetría (tabla "taxi" con telemetria = '1').
    public class ConductorTelemetria
    {
        public int Codtaxi { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Dni { get; set; }
        public string? Telefono { get; set; }
        public string? Brevete { get; set; }
        public string? Codusuario { get; set; }
        public string? CodigoIbutton { get; set; }

        // Los dos siguientes los administra el backend según la respuesta de Traccar.
        public int? TraccarDriverId { get; set; }
        public bool TraccarSync { get; set; }
    }
}
