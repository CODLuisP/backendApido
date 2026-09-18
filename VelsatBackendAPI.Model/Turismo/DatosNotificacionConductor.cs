using System;

namespace VelsatBackendAPI.Model.Turismo
{
    // Datos mínimos para armar y enviar la notificación manual por WhatsApp de un servicio
    // (ver ServTurismoRepository.GetDatosNotificacion): fecha/hora del servicio y el teléfono
    // del conductor asignado, resuelto por brevete desde la tabla taxi (nunca desde un valor
    // que mande el cliente).
    public class DatosNotificacionConductor
    {
        public DateTime? Fechainicio { get; set; }
        public TimeSpan? Horainicio { get; set; }
        public string? Brevete { get; set; }
        public string? Telefono { get; set; }
    }
}
