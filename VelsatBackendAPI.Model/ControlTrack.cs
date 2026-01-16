using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model
{
    public class ControlTrack
    {
        public string Token { get; set; }
        public string DeviceId { get; set; }
        public string Username { get; set; }
        public int DuracionMinutos { get; set; }
        public DateTime? Creationdate { get; set; }
        public DateTime? Expirationdate { get; set; }

    }
}
