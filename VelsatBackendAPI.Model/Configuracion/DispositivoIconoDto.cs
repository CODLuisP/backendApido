namespace VelsatBackendAPI.Model.Configuracion
{
    public class DispositivoIconoDto
    {
        public string DeviceID { get; set; }
        public string? Descripcion { get; set; }
        // null = la unidad usa el ícono por defecto
        public string? IconoUrl { get; set; }
    }
}
