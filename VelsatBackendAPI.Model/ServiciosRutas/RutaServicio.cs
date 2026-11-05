using System;
using System.Collections.Generic;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    public class RutaServicio
    {
        public long Id { get; set; }
        public string AccountID { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Grupo { get; set; }
        public string? Descripcion { get; set; }
        public decimal? DistanciaKm { get; set; }
        public bool Activo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Solo en el listado
        public int? CantidadParaderos { get; set; }

        // Solo en el detalle (GET por id)
        public List<RutaParadero>? Paraderos { get; set; }
    }
}
