using System;

namespace VelsatBackendAPI.Model.Notificaciones
{
    public class UserFCMToken
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string FCMToken { get; set; }
        public string Platform { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
