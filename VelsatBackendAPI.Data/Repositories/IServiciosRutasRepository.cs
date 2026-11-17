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
        Task<int> GetIntervaloGeneracion(string accountID);
        Task<int> GetIntervaloGeneracionMinimo();
        Task SetIntervaloGeneracion(string accountID, int minutos);
        Task<bool> ExisteServicioDeRuta(string accountID, long rutaId, DateTime fecha, TimeSpan hora);
        Task<HashSet<(long PlantillaId, DateTime Fecha)>> GetPlantillasGeneradas(DateTime desde, DateTime hasta);
        Task<bool> ReservarGeneracion(long plantillaId, DateTime fecha);
        Task<int> VincularGeneracion(long plantillaId, DateTime fecha, long servicioId);

        // ---------- Configuración ----------
        Task<int> GetTolerancia(string accountID);
        Task SetTolerancia(string accountID, int toleranciaMin);
    }
}
