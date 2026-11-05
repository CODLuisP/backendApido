using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    public class ServicioRuta
    {
        public long Id { get; set; }
        public string AccountID { get; set; } = string.Empty;
        public string DeviceID { get; set; } = string.Empty;
        public long RutaId { get; set; }

        // Solo lectura: se completan con un JOIN al listar / consultar.
        public string? RutaCodigo { get; set; }
        public string? RutaNombre { get; set; }

        // Misma convención que Turismo: fecha "dd/MM/yyyy" y hora "HH:mm" en el JSON.
        [JsonConverter(typeof(FechaDdMmYyyyJsonConverter))]
        public DateTime? FechaSalida { get; set; }

        [JsonConverter(typeof(HoraHmJsonConverter))]
        public TimeSpan? HoraSalida { get; set; }

        public string ConductorNombre { get; set; } = string.Empty;
        public string ConductorBrevete { get; set; } = string.Empty;
        public string ConductorCelular { get; set; } = string.Empty;

        public string Estado { get; set; } = "PROGRAMADO";   // PROGRAMADO | EN_CURSO | FINALIZADO | CANCELADO
        public DateTime? InicioReal { get; set; }
        public DateTime? FinReal { get; set; }
        public decimal? DistanciaRealKm { get; set; }
        public string? Observaciones { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Solo en el listado: paraderos con llegada registrada / total de paraderos del servicio.
        public int? ParaderosCompletados { get; set; }
        public int? TotalParaderos { get; set; }

        // Solo en el detalle (GET por id)
        public List<ServicioRutaParadero>? Paraderos { get; set; }
    }

    public class ServicioRutaParadero
    {
        public long Id { get; set; }
        public long ServicioId { get; set; }
        public long RutaParaderoId { get; set; }
        public int Orden { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime HoraEsperada { get; set; }
        public DateTime? HoraLlegadaReal { get; set; }
        public DateTime? HoraSalidaReal { get; set; }

        // Positivo = retraso, negativo = adelanto (columna calculada en la base).
        public int? DiferenciaMin { get; set; }

        public string Estado { get; set; } = "PENDIENTE";   // PENDIENTE | A_TIEMPO | ADELANTADO | RETRASADO | OMITIDO
    }

    public class CambiarEstadoServicioRequest
    {
        public string? Estado { get; set; }

        // Opcionales; útiles al FINALIZAR.
        public decimal? DistanciaRealKm { get; set; }
        public string? Observaciones { get; set; }
    }

    // Cuerpo opcional al registrar llegada / salida de un paradero.
    // Hora en formato "dd/MM/yyyy HH:mm" (o "dd/MM/yyyy HH:mm:ss"), en hora local de Perú.
    // Si no se envía, el servidor usa la hora actual de Perú.
    public class RegistrarMarcaRequest
    {
        public string? Hora { get; set; }
    }

    public class ConfigServiciosRutas
    {
        public string AccountID { get; set; } = string.Empty;
        public int ToleranciaMin { get; set; }
    }

    public class ToleranciaRequest
    {
        public int ToleranciaMin { get; set; }
    }
}
