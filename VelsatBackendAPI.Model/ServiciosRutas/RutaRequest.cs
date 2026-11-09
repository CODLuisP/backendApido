using System.Collections.Generic;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    // Cuerpo para crear o editar una ruta junto con sus paraderos.
    // El orden de la lista define el orden de los paraderos: el primero es el ORIGEN,
    // el último el DESTINO y los del medio INTERMEDIO (el backend asigna orden y tipo).
    public class RutaRequest
    {
        public string? AccountID { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }

        // Grupo al que pertenece la ruta (ej. "Grupo A"). Opcional, máx. 50 caracteres.
        public string? Grupo { get; set; }

        // "IDA" o "REGRESO". Null: al crear = IDA, al editar = conserva el actual.
        public string? Sentido { get; set; }

        // Ruta del sentido contrario. Debe ser de la misma cuenta y de sentido distinto. Se enlaza en
        // las dos rutas. Al EDITAR, null = sin pareja (si tenía una, se desenlaza).
        public long? RutaParejaId { get; set; }
        public string? Descripcion { get; set; }
        public decimal? DistanciaKm { get; set; }

        // Línea del recorrido como pares [latitud, longitud]. Al EDITAR: null = conservar el trazado
        // actual (así el administrador de rutas no lo borra), lista vacía = borrarlo.
        public List<double[]>? Trazado { get; set; }
        public List<ParaderoRequest> Paraderos { get; set; } = new();
    }

    public class ParaderoRequest
    {
        // Null o 0 = paradero nuevo. Con valor = paradero existente de la ruta (al editar).
        public long? Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public int RadioMetros { get; set; } = 100;

        // Minutos para llegar a este paradero desde el anterior. Se ignora en el origen (siempre 0).
        public int MinutosDesdeAnterior { get; set; }
    }

    public class ActivoRequest
    {
        public bool Activo { get; set; }
    }
}
