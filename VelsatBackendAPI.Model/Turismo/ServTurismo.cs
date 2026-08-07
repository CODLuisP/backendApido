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

        // Acuse de recibo del conductor desde la app móvil.
        // Visto: se marca solo, cuando el servicio se muestra en pantalla.
        // Confirmado: se marca cuando el conductor desliza la tarjeta del servicio.
        public byte? Visto { get; set; }
        public byte? Confirmado { get; set; }

        // Se marca cuando el conductor desliza la tarjeta hacia la derecha (acción irreversible,
        // confirmada con un modal en la app). Es el estado final del ciclo de vida del servicio.
        public byte? Finalizado { get; set; }

        // Se marca en 1 cuando el servicio se reprograma (Patch cambia fechainicio a otro día).
        // Al reprogramar, el repositorio limpia visto/confirmado/finalizado (ver ServTurismoRepository.Patch)
        // para que solo quede visible la etiqueta "Reprogramado" hasta que el conductor vuelva a actuar.
        public byte? Reprogramado { get; set; }

        // Cancelado (1 = cancelado) reemplaza al antiguo campo de texto "estado". Un servicio cancelado
        // ya no admite ediciones (ver ServTurismoRepository.Patch) y no se devuelve en las consultas
        // filtradas por conductor (brevete) para que no aparezca en la app móvil.
        public byte? Cancelado { get; set; }
    }
}
