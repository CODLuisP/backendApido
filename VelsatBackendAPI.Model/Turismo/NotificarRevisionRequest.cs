using System.Collections.Generic;

namespace VelsatBackendAPI.Model.Turismo
{
    // Body de POST api/servturismo/notificar-revision. Brevetes elegidos en el modal del front; el
    // backend solo notifica a los que de verdad tienen servicios activos en la fecha, así que no
    // sirve para mandar el mensaje a un conductor cualquiera.
    public class NotificarRevisionRequest
    {
        public List<string> Brevetes { get; set; } = new();
    }
}
