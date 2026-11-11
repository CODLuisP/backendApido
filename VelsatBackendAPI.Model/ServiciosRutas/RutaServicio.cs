using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    public class RutaServicio
    {
        public long Id { get; set; }
        public string AccountID { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Grupo { get; set; }

        // "IDA" o "REGRESO". Cada sentido es una ruta propia.
        public string Sentido { get; set; } = "IDA";

        // Ruta del sentido contrario (null si no tiene). Se guarda en las dos rutas.

        public string? Descripcion { get; set; }
        public decimal? DistanciaKm { get; set; }
        public bool Activo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Solo en el listado
        public int? CantidadParaderos { get; set; }

        // Solo en el listado: minutos totales de la ruta (suma de los tramos de sus paraderos activos).
        public int? DuracionMin { get; set; }

        // Texto JSON tal como está guardado en la columna `trazado`. No sale en la respuesta:
        // el detalle lo entrega ya convertido en la lista `Trazado`.
        [JsonIgnore]
        public string? TrazadoJson { get; set; }

        // Solo en el detalle (GET por id): línea del recorrido como pares [latitud, longitud]
        // en el orden en que se recorre. Null si la ruta no tiene trazado cargado.
        public List<double[]>? Trazado { get; set; }

        // Solo en el detalle (GET por id)
        public List<RutaParadero>? Paraderos { get; set; }
    }
}
