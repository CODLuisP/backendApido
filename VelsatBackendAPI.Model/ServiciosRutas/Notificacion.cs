using System;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    // Mensaje predefinido que el conductor ve en la app. Lo administra el cliente desde la web.
    public class NotificacionTipo
    {
        public long Id { get; set; }
        public string AccountID { get; set; } = string.Empty;

        // Estable (ej. DESVIO_TRAFICO): sirve para filtros y reportes aunque cambie el título.
        public string Codigo { get; set; } = string.Empty;

        // SUGERENCIA | ALERTA | INCIDENCIA | INFO
        public string Categoria { get; set; } = "INFO";

        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        // La app obliga a elegir el paradero afectado / a escribir un comentario.
        public bool RequiereParadero { get; set; }
        public bool RequiereComentario { get; set; }

        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
    }

    // Notificación enviada por un conductor. Guarda una copia del texto y de los datos de la unidad,
    // el conductor y el paradero: si luego cambia el catálogo, el historial no se altera.
    public class Notificacion
    {
        public long Id { get; set; }
        public string AccountID { get; set; } = string.Empty;

        public long? TipoId { get; set; }
        public string TipoCodigo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? Comentario { get; set; }

        public long? ServicioId { get; set; }
        public long? RutaId { get; set; }
        public string? RutaNombre { get; set; }
        public long? RutaParaderoId { get; set; }
        public string? ParaderoNombre { get; set; }
        public string? DeviceID { get; set; }
        public string? ConductorBrevete { get; set; }
        public string? ConductorNombre { get; set; }

        // Dónde estaba el conductor al enviarla.
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }

        // Hora de Perú. FechaEvento = reloj del celular; CreatedAt = cuando llegó al servidor.
        public DateTime? FechaEvento { get; set; }
        public DateTime CreatedAt { get; set; }

        // Gestión desde la web (solo informativa: no cambia ninguna ruta ni paradero).
        public bool Leida { get; set; }
        public DateTime? LeidaAt { get; set; }
        public string Estado { get; set; } = "PENDIENTE";   // PENDIENTE | ACEPTADA | RECHAZADA
        public string? Respuesta { get; set; }
        public string? GestionadoPor { get; set; }
        public DateTime? GestionadoAt { get; set; }
    }

    // Lo que envía la app del conductor. El backend completa unidad, conductor, ruta y nombre del
    // paradero a partir del servicio. Sin servicio, la app puede mandar la unidad y el conductor.
    public class EnviarNotificacionRequest
    {
        public long TipoId { get; set; }
        public long? ServicioId { get; set; }
        public long? RutaId { get; set; }            // solo si no hay servicio y se señala un paradero
        public long? RutaParaderoId { get; set; }    // paradero señalado (ej. corrección de paradero)
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public string? Comentario { get; set; }

        // "dd/MM/yyyy HH:mm:ss" (hora de Perú del celular). Si falta, queda vacío.
        public string? FechaEvento { get; set; }

        // Solo si no se envía servicio.
        public string? DeviceID { get; set; }
        public string? ConductorBrevete { get; set; }
        public string? ConductorNombre { get; set; }
    }

    public class GestionarNotificacionRequest
    {
        public string? Estado { get; set; }          // ACEPTADA | RECHAZADA | PENDIENTE
        public string? Respuesta { get; set; }
        public string? Usuario { get; set; }         // quién la gestiona (si no, la cuenta)
    }
}
