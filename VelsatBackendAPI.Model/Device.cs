using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Model
{
    public class Device
    {
        public string DeviceId { get; set; }
        public double LastValidLatitude { get; set; }
        public double LastValidLongitude { get; set; }
        public double LastValidHeading { get; set; }
        public double LastGPSTimestamp { get; set; }
        public double LastValidSpeed { get; set; }
        public double LastOdometerKM { get; set; }
        public string Descripcion { get; set; }
        public string Direccion { get; set; }
        public string? Codgeoact { get; set; }
        public string? Rutaact { get; set; }
        public string? Servicio { get; set; }
        public bool Telemetria { get; set; }

        // null = el equipo aún no ha reportado ignition.
        public bool? Ignition { get; set; }

        // Texto crudo de la columna JSON "attributes"; no se envía al cliente.
        [JsonIgnore]
        public string? AttributesJson { get; set; }

        // Se serializa como objeto JSON y no como texto escapado.
        public JsonElement? Attributes => string.IsNullOrEmpty(AttributesJson)
            ? null
            : JsonSerializer.Deserialize<JsonElement>(AttributesJson);

        public Servicio? UltimoServicio { get; set; }
        public Geocercausu? DatosGeocercausu { get; set; }

    }
}
