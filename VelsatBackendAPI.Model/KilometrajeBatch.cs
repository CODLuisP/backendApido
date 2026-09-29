using System.Collections.Generic;

namespace VelsatBackendAPI.Model
{
    public class RangoKilometrajeRequest
    {
        public string RangoId { get; set; }
        public string DeviceID { get; set; }
        public string FechaIni { get; set; }
        public string FechaFin { get; set; }
    }

    public class KilometrajeBatchRequest
    {
        public string AccountID { get; set; }
        public List<RangoKilometrajeRequest> Rangos { get; set; }
    }

    public class KilometrosRecorridosServicio
    {
        public string RangoId { get; set; }
        public string DeviceId { get; set; }
        public double Maximo { get; set; }
        public double Minimo { get; set; }
        public double Kilometros { get; set; }
    }

    public class KilometrajeBatchReporting
    {
        public List<KilometrosRecorridosServicio> Resultados { get; set; } = new List<KilometrosRecorridosServicio>();
        public string Mensaje { get; set; }
    }
}
