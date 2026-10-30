using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model
{
    public class SpeedReporting
    {
        public int Item { get; set; }
        public string DeviceId { get; set; }
        public double SpeedKPH { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
    }

    public class SpeedExcelRequest
    {
        public string FechaIni { get; set; }
        public string FechaFin { get; set; }
        public string DeviceId { get; set; }
        public string AccountID { get; set; }
        public bool AllUnits { get; set; }
        public List<SpeedReporting> Rows { get; set; }
    }
}
