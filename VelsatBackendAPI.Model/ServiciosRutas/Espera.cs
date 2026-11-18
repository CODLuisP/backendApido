using System.Collections.Generic;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    // Lo que envía la app del pasajero: "estoy esperando en este paradero de esta ruta".
    // PasajeroId es un id anónimo del celular: varias pulsaciones del mismo celular cuentan como UNA persona.
    public class EsperaRequest
    {
        public long RutaId { get; set; }
        public long RutaParaderoId { get; set; }
        public string PasajeroId { get; set; } = string.Empty;
    }

    // Personas esperando en un paradero (lo que ve el conductor).
    public class EsperaParadero
    {
        public long RutaParaderoId { get; set; }
        public int Orden { get; set; }
        public string Paradero { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    // Evento "EsperasActualizadas" que recibe la app del conductor por SignalR: lista completa vigente de la ruta.
    public class EsperasActualizadas
    {
        public long RutaId { get; set; }
        public List<EsperaParadero> Esperas { get; set; } = new();
    }
}
