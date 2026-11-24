namespace VelsatBackendAPI.Model.Configuracion
{
    // Cambio de contraseña con el token recibido por correo (no requiere sesión).
    public class ResetPasswordRequest
    {
        public string? Token { get; set; }
        public string? PasswordNueva { get; set; }
    }
}
