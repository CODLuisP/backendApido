namespace VelsatBackendAPI.Model.Configuracion
{
    public class PerfilCuenta
    {
        public string AccountID { get; set; }
        public string? DisplayName { get; set; }
        // Empresa / descripción de la cuenta (solo lectura: PUT perfil no la modifica).
        public string? Description { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressState { get; set; }
        public string? AddressPostalCode { get; set; }
        public string? AddressCountry { get; set; }
    }
}
