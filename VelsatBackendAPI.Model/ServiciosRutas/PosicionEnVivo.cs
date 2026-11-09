using System;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    // Cuerpo que manda la app del conductor cada pocos segundos. NO se guarda en la base de datos:
    // solo se reenvía en vivo a la web (SignalR) y se conserva la última posición de cada unidad en memoria.
    public class EnviarPosicionRequest
    {
        // Unidad (placa / deviceID). Obligatorio.
        public string? DeviceID { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }

        // Grados desde el norte, sentido horario (0-360). Opcional.
        public double? Rumbo { get; set; }

        // km/h. Opcional.
        public double? Velocidad { get; set; }

        // Opcionales: la app ya los conoce y así el backend no consulta la base en cada envío.
        public string? Conductor { get; set; }
        public long? RutaId { get; set; }
        public long? ServicioId { get; set; }
    }

    // Evento "ServicioActualizado" que recibe la web cuando cambia un servicio.
    // Tipo: CREADO | EDITADO | REPROGRAMADO | ESTADO | LLEGADA | SALIDA.
    public class AvisoServicio
    {
        public long ServicioId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? DeviceID { get; set; }

        // Solo LLEGADA / SALIDA
        public int? Orden { get; set; }
        public string? Paradero { get; set; }
        public string? Hora { get; set; }          // HH:mm:ss, hora de Perú

        // Solo LLEGADA: A_TIEMPO | ADELANTADO | RETRASADO y minutos de diferencia (+ = retraso)
        public string? EstadoParadero { get; set; }
        public int? DiferenciaMin { get; set; }

        // Solo SALIDA: minutos entre la llegada y la salida (permanencia dentro de la geocerca)
        public int? PermanenciaMin { get; set; }

        // Solo ESTADO: nuevo estado del servicio
        public string? EstadoServicio { get; set; }
    }

    // Lo que recibe la web (evento "PosicionActualizada" y GET posiciones).
    public class PosicionEnVivo
    {
        // Identificador estable del marcador en el mapa (= deviceID).
        public string Clave { get; set; } = string.Empty;
        public string DeviceID { get; set; } = string.Empty;
        public string? Conductor { get; set; }
        public long? RutaId { get; set; }
        public long? ServicioId { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public double Rumbo { get; set; }
        public double? Velocidad { get; set; }

        // Momento en que el servidor recibió la posición (UTC, ISO 8601).
        public DateTime Fecha { get; set; }
    }
}
