using System;

namespace VelsatBackendAPI.Model.Turismo
{
    // Mensaje/solicitud que el conductor manda desde la app móvil sobre un servicio puntual
    // (botón "Mensaje / Solicitud"). Se notifica en vivo por SignalR al front de despacho y
    // también queda guardado, para poder recuperarlo si el front estaba desconectado.
    public class MensajeTurismo
    {
        public int Idmensaje { get; set; }
        public int Idservicio { get; set; }

        // "observacion": texto libre. "ampliacion": solicitud predefinida de ampliación de
        // servicio, con Horas (del select) y Texto opcional (detalle adicional escrito a mano).
        public string Tipo { get; set; } = string.Empty;

        public string? Texto { get; set; }
        public int? Horas { get; set; }
        public string? Brevete { get; set; }
        public DateTime Fecha { get; set; }
        public byte Atendido { get; set; }

        // Datos del servicio y del conductor (join a servturismo/taxi), para que el front pueda
        // mostrar la alerta completa sin hacer una segunda consulta.
        public string? Fechainicio { get; set; }
        public string? Horainicio { get; set; }
        public string? Cliente { get; set; }
        public string? Origen { get; set; }
        public string? Destino { get; set; }

        // Igual que en GetByFechas: bus/placa se devuelven por separado y el front los combina
        // (mismo criterio que usa para el resto de la tabla de servicios).
        public string? Bus { get; set; }
        public string? Placa { get; set; }
        public string? Piloto { get; set; }
        public string? Celular { get; set; }
    }
}
