using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Administracion
{
    public class Deviceuser
    {
        public string Id { get; set; }
        public string? UserId { get; set; }
        public string? DeviceName { get; set; }
        public string? Status { get; set; }
        public string? DeviceID { get; set; }
    }
}
