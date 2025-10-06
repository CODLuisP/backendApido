using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.AlarmasCorreo
{
    public class RegistroAlarmas
    {
        public int Codigo { get; set; }
        public string AccountID { get; set; }
        public string DeviceID { get; set; }
        public int Timestamp { get; set; }
        public int StatusCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public bool IsEnviado { get; set; }

    }
}
