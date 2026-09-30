namespace VelsatBackendAPI.Model.Turismo
{
    // Body del POST api/ServTurismo/{idservicio}/mensaje.
    public class EnviarMensajeTurismoRequest
    {
        // "observacion" | "ampliacion"
        public string Tipo { get; set; } = string.Empty;

        // Requerido si Tipo = "observacion" (texto libre); opcional en "ampliacion" (detalle adicional).
        public string? Texto { get; set; }

        // Requerido si Tipo = "ampliacion": cantidad de días elegida en el select.
        public int? Dias { get; set; }
    }
}
