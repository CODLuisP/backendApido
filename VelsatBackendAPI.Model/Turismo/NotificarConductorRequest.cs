namespace VelsatBackendAPI.Model.Turismo
{
    // Body de PATCH api/servturismo/{idservicio}/notificar. Solo viaja el tipo de plantilla:
    // el mensaje final lo arma el backend con los datos reales del servicio, para que el
    // cliente no pueda mandar texto arbitrario en la notificación.
    public class NotificarConductorRequest
    {
        public string Tipo { get; set; } = string.Empty;
    }
}
