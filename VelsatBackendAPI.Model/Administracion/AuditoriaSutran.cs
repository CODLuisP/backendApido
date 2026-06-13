using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Administracion
{
    public class AuditoriaSutran
    {
        public int Id { get; set; }
        public string AccountID { get; set; }
        public string DeviceID { get; set; }
        public DateTime? Fecharegistro { get; set; }
        public string Lastenvio { get; set; }
        public string Lastrespuesta { get; set; }
    }
}
