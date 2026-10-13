using System;

namespace VelsatBackendAPI.Model.Turismo
{
    // Datos mínimos para armar y enviar la notificación manual de un servicio (ver
    // ServTurismoRepository.GetDatosNotificacion): fecha/hora del servicio y los brevetes del piloto
    // y del copiloto. El teléfono y el token FCM de cada uno se resuelven después por brevete en BD
    // (nunca desde un valor que mande el cliente).
    public class DatosNotificacionConductor
    {
        public DateTime? Fechainicio { get; set; }
        public TimeSpan? Horainicio { get; set; }
        public string? Brevete { get; set; }
        public string? Cobrevete { get; set; }
    }
}
