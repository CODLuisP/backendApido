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
        public string? Descripcion { get; set; }
        public decimal? DistanciaKm { get; set; }
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
