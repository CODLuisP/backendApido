namespace VelsatBackendAPI.Model.Configuracion
{
    // Datos mínimos para enviar el correo de cambio de contraseña.
    public class ContactoCuenta
    {
        public string AccountID { get; set; }
        public string? DisplayName { get; set; }
        public string? ContactEmail { get; set; }
    }
}
