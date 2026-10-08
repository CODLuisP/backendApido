namespace VelsatBackendAPI.Model.Configuracion
{
    public class ActualizarPerfilRequest
    {
        public string? DisplayName { get; set; }
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
