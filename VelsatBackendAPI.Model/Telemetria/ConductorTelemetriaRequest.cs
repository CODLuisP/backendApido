namespace VelsatBackendAPI.Model.Telemetria
{
    // Cuerpo del POST y del PUT. No incluye traccar_driver_id ni traccar_sync a propósito:
    // el cliente no puede fijarlos.
    public class ConductorTelemetriaRequest
    {
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Dni { get; set; }
        public string? Telefono { get; set; }
        public string? Brevete { get; set; }
        public string? Codusuario { get; set; }
        public string? CodigoIbutton { get; set; }
    }
}
