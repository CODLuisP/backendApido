using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IServTurismoRepository
    {
        Task<List<ServTurismo>> GetByFechas(DateTime fechaInicio, DateTime fechaFin, string? brevete = null);

        Task<int> Insert(ServTurismo servicio);

        Task<int> InsertBatch(IEnumerable<ServTurismo> servicios);

        // BreveteAnterior: valor de "brevete" antes del patch, para que el caller pueda detectar
        // un cambio de conductor y notificarlo (ver ServTurismoController.Patch).
        // CamposModificados: columnas auditables que realmente cambiaron de valor en este patch,
        // para que el caller decida si notifica "cambios en el servicio" (ver ServTurismoController.Patch).
        // usuario/motivo: quién hizo la edición y por qué (motivo es opcional); se usan para dejar
        // un registro en servturismo_auditoria por cada campo que realmente cambió de valor.
        Task<(int Filas, string? BreveteAnterior, List<string> CamposModificados)> Patch(int idservicio, ServTurismo campos, bool limpiarNulos = false, string? usuario = null, string? motivo = null);

        Task<int> Delete(int idservicio);

        // Elimina físicamente todos los servicios cuya fechainicio sea la indicada (borrado masivo
        // "Eliminar carga", pensado para deshacer una carga de Excel completa de un día).
        Task<int> DeleteByFecha(DateTime fecha);

        // Historial de auditoría de un servicio, más reciente primero.
        Task<List<ServTurismoAuditoria>> GetAuditoria(int idservicio);

        // Fecha/hora del servicio y teléfono del conductor asignado (join a taxi por brevete),
        // para armar y enviar la notificación manual por WhatsApp del botón en la tabla web.
        // Null si el servicio no existe.
        Task<DatosNotificacionConductor?> GetDatosNotificacion(int idservicio);

        // Teléfono de cada brevete indicado según la ficha vigente en la tabla taxi (brevete -> telefono,
        // sin normalizar). Pensado para notificar por WhatsApp una carga en lote (ver
        // ServTurismoController.InsertLote): el destino siempre se resuelve acá, nunca desde el cliente.
        // Los brevetes sin ficha o sin teléfono no aparecen en el resultado.
        Task<Dictionary<string, string>> GetTelefonosPorBrevete(IEnumerable<string> brevetes);

        // Acuse de recibo del conductor. Devuelven false solo si el idservicio no existe;
        // volver a marcar un servicio ya marcado es una operación válida (no cambia nada).
        Task<bool> MarcarVisto(int idservicio);

        Task<bool> MarcarConfirmado(int idservicio);

        Task<bool> MarcarFinalizado(int idservicio);

        Task<bool> MarcarCancelado(int idservicio);

        Task<bool> MarcarStandby(int idservicio);

        Task<bool> MarcarReanudar(int idservicio);

        //CRUD tabla taxi (conductores)
        Task<List<ConductorTurismo>> GetTaxis(string codusuario);

        Task<ConductorTurismo?> GetTaxiById(int codtaxi);

        Task<int> InsertTaxi(ConductorTurismo taxi);

        Task<int> PatchTaxi(int codtaxi, ConductorTurismo campos);

        Task<int> DeleteTaxi(int codtaxi);
    }
}
