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

        // ---------- Configuración ----------
        Task<int> GetTolerancia(string accountID);
        Task SetTolerancia(string accountID, int toleranciaMin);
    }
}
