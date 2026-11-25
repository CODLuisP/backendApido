namespace VelsatBackendAPI.Model
{
    /// <summary>
    /// Servicio vigente de una unidad (placa), leído de la base DO (donde viven los servicios).
    /// Status: "1" = programado, "2" = en curso. Los finalizados ("3") no se devuelven.
    /// </summary>
    public class ServicioActual
    {
        public string DeviceId { get; set; } = string.Empty;
        public string? Codservicio { get; set; }
        public string? Status { get; set; }
        public string? Numero { get; set; }
        public string? Tipo { get; set; }
        public string? Empresa { get; set; }
        public string? Conductor { get; set; }
        // Texto tal como está en la base: dd/MM/yyyy HH:mm
        public string? Fecha { get; set; }
    }
}
