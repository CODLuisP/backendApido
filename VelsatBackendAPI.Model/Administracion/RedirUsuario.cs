namespace VelsatBackendAPI.Model.Administracion
{
    // Mapea las tablas serverprueba (web) y servermobile (mobile), ambas con las mismas columnas.
    public class RedirUsuario
    {
        public int Id { get; set; }
        public string? Loginusu { get; set; }
        public string? Servidor { get; set; }
        public string? Tipo { get; set; }
    }
}
