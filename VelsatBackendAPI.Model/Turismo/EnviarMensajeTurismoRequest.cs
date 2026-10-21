namespace VelsatBackendAPI.Model.Turismo
{
    // Body del POST api/ServTurismo/{idservicio}/mensaje.
    public class EnviarMensajeTurismoRequest
    {
        // "observacion" | "ampliacion"
        public string Tipo { get; set; } = string.Empty;

        // Requerido si Tipo = "observacion" (texto libre); opcional en "ampliacion" (detalle adicional).
        public string? Texto { get; set; }

        // Requerido si Tipo = "ampliacion": cantidad de horas elegida en el select.
        public int? Horas { get; set; }

        // Brevete del conductor con la sesión abierta en la app. Opcional: las versiones viejas
        // de la app no lo mandan. Se valida contra piloto/copiloto del servicio; si no viene o no
        // coincide, se asume el piloto.
        public string? Brevete { get; set; }
    }
}
