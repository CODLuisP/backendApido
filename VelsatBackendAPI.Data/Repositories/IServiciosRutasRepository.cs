using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.ServiciosRutas;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IServiciosRutasRepository
    {
        // ---------- Rutas ----------
        Task<IEnumerable<RutaServicio>> GetRutas(string accountID, bool incluirInactivas);
        Task<RutaServicio?> GetRutaById(long id, string accountID);
        Task<bool> ExisteCodigoRuta(string accountID, string codigo, long? excluirId);
        Task<long> InsertRuta(RutaRequest request);
        Task UpdateRuta(long id, RutaRequest request);
        Task SetRutaActivo(long id, string accountID, bool activo);

        // ---------- Servicios ----------
        Task<IEnumerable<ServicioRuta>> GetServicios(string accountID, DateTime? desde, DateTime? hasta, string? estado, string? deviceID,
            string? conductorBrevete = null);
        Task<ServicioRuta?> GetServicioById(long id, string accountID);
        // Reporte: servicios del rango con todos sus paraderos (2 consultas, sin N+1). Sin los conteos: ver ReporteServiciosCalculos.
        Task<List<ReporteServicioItem>> GetReporteServicios(string accountID, DateTime? desde, DateTime? hasta,
            string? estado, string? deviceID, string? conductorBrevete);
        Task<int> ContarParaderosDeRutaActiva(long rutaId, string accountID);
        Task<long> InsertServicio(ServicioRuta servicio);
        Task UpdateServicio(ServicioRuta servicio);
        Task<int> ReprogramarServicio(long id, string accountID, string estadoActual,
            DateTime nuevaSalida, int desplazamientoMin, string? motivo);
        Task<int> CambiarEstadoServicio(long id, string accountID, string estadoActual, string nuevoEstado,
            DateTime ahoraUtc, decimal? distanciaRealKm, string? observaciones);

        // ---------- Seguimiento por paradero ----------
        Task<int> RegistrarLlegada(long servicioId, int orden, DateTime horaLlegada, string estado);
        Task<int> MarcarOmitidosAnteriores(long servicioId, int orden);
        Task<int> RegistrarSalida(long servicioId, int orden, DateTime horaSalida);

        // ---------- Plantillas (servicios automáticos) ----------
        Task<IEnumerable<ServicioPlantilla>> GetPlantillas(string? accountID, bool soloActivas);
        Task<ServicioPlantilla?> GetPlantillaById(long id, string accountID);
        Task<long> InsertPlantilla(ServicioPlantilla plantilla);
        Task<int> UpdatePlantilla(ServicioPlantilla plantilla);
        Task<int> DeletePlantilla(long id, string accountID);
        Task<TimeSpan> GetHoraGeneracion(string accountID);
        Task<Dictionary<string, TimeSpan>> GetHorasGeneracion();
        Task SetHoraGeneracion(string accountID, TimeSpan hora);
        Task<bool> ExisteServicioDeRuta(string accountID, long rutaId, DateTime fecha, TimeSpan hora);
        Task<HashSet<(long PlantillaId, DateTime Fecha)>> GetPlantillasGeneradas(DateTime desde, DateTime hasta);
        Task<bool> ReservarGeneracion(long plantillaId, DateTime fecha);
        Task<int> VincularGeneracion(long plantillaId, DateTime fecha, long servicioId);

        // ---------- Notificaciones del conductor ----------
        Task<IEnumerable<NotificacionTipo>> GetNotificacionTipos(string accountID, bool soloActivos);
        Task<NotificacionTipo?> GetNotificacionTipoById(long id, string accountID);
        Task<bool> ExisteCodigoNotificacionTipo(string accountID, string codigo, long? excluirId);
        Task<long> InsertNotificacionTipo(NotificacionTipo tipo);
        Task<int> UpdateNotificacionTipo(NotificacionTipo tipo);
        Task<int> DeleteNotificacionTipo(long id, string accountID);
        Task<string?> GetNombreParadero(long rutaParaderoId, long rutaId);
        Task<string?> GetNombreRuta(long rutaId, string accountID);
        Task<long> InsertNotificacion(Notificacion notificacion);
        Task<Notificacion?> GetNotificacionById(long id, string accountID);
        Task<IEnumerable<Notificacion>> GetNotificaciones(string accountID, DateTime? desde, DateTime? hasta,
            string? estado, string? tipoCodigo, bool soloNoLeidas, int limite);
        Task<int> ContarNotificacionesNoLeidas(string accountID);
        Task<int> MarcarNotificacionLeida(long id, string accountID, DateTime ahora);
        Task<int> MarcarTodasNotificacionesLeidas(string accountID, DateTime ahora);
        Task<int> GestionarNotificacion(long id, string accountID, string estado, string? respuesta, string usuario, DateTime ahora);

        // ---------- Configuración ----------
        Task<int> GetTolerancia(string accountID);
        Task SetTolerancia(string accountID, int toleranciaMin);
    }
}
