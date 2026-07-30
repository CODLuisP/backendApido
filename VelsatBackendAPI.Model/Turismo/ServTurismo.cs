using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Turismo
{
    public class ServTurismo
    {
        public int Idservicio { get; set; }

        [JsonConverter(typeof(FechaDdMmYyyyJsonConverter))]
        public DateTime? Fechainicio { get; set; }

        public string? Instrucciones { get; set; }

        [JsonConverter(typeof(HoraHmJsonConverter))]
        public TimeSpan? Horainicio { get; set; }

        public string? Indicaciones { get; set; }
        public string? Horaretorno { get; set; }
        public string? Bus { get; set; }
        public string? Placa { get; set; }
        public string? Brevete { get; set; }
        public string? Piloto { get; set; }
        public string? Celular { get; set; }
        public string? Cobrevete { get; set; }
        public string? Copiloto { get; set; }
        public string? Cocelular { get; set; }
        public string? Tipounidad { get; set; }
        public string? Cliente { get; set; }
        public string? Grupo { get; set; }
        public string? Numpax { get; set; }
        public string? Origen { get; set; }
        public string? Destino { get; set; }
        public string? Guiaturista { get; set; }
        public string? Vuelocliente { get; set; }
        public string? Observaciones { get; set; }
        public string? Ejecutivo { get; set; }
        public string? Cotizacion { get; set; }
    }
}
