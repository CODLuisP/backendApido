namespace VelsatBackendAPI.Model.ServiciosRutas
{
    public class RutaParadero
    {
        public long Id { get; set; }
        public long RutaId { get; set; }
        public int Orden { get; set; }
        public string Tipo { get; set; } = "INTERMEDIO";   // ORIGEN | INTERMEDIO | DESTINO
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public int RadioMetros { get; set; }
        public int MinutosDesdeAnterior { get; set; }

        // No es una columna: se calcula al leer la ruta (suma de minutosDesdeAnterior hasta este paradero).
        public int MinutosAcumulados { get; set; }

        public bool Activo { get; set; }
    }
}
