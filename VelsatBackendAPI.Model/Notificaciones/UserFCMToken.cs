using System;

namespace VelsatBackendAPI.Model.Notificaciones
{
    public class UserFCMToken
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string FCMToken { get; set; }
        public string Platform { get; set; }
        // Identifica el dispositivo físico (react-native-device-info getUniqueId en el móvil).
        // Permite que un mismo Codigo tenga un token activo por dispositivo en vez de uno solo.
        public string DeviceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
