using System;
using System.Collections.Generic;

namespace VelsatBackendAPI.Model.Turismo
{
    // Un registro por campo modificado en un PATCH de edición del panel de servicios de turismo.
    // No audita visto/confirmado/finalizado/standby/cancelado/reprogramado (son acuses del conductor
    // o acciones con su propio endpoint, no ediciones manuales del formulario).
    public class ServTurismoAuditoria
    {
        public int Idauditoria { get; set; }
        public int Idservicio { get; set; }
        public string Campo { get; set; } = string.Empty;
        public string? ValorAnterior { get; set; }
        public string? ValorNuevo { get; set; }
        public string? Usuario { get; set; }
        public string? Motivo { get; set; }
        public DateTime Fecha { get; set; }
    }

    // Resumen de la última edición de un servicio, para resaltar en la tabla (negrita) qué
    // campos cambiaron sin tener que pedir el historial completo de cada fila.
    public class UltimaModificacionInfo
    {
        public List<string> Campos { get; set; } = new();
        public DateTime Fecha { get; set; }
    }
}
