using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace VelsatBackendAPI.Hubs
{
    // Hub dedicado a notificar en vivo al front de despacho los mensajes/solicitudes que el
    // conductor manda desde la app (ver ServTurismoController.EnviarMensaje). Cada cuenta de
    // despacho se une a su propio grupo (el "usuario" de la ruta), igual que el resto de hubs
    // de la app; hoy solo existe la cuenta "movilbus".
    public class TurismoMensajesHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var usuario = ObtenerUsuarioDeRuta();

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, usuario);
            }

            await base.OnConnectedAsync();
        }

        private string? ObtenerUsuarioDeRuta()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.RouteValues.TryGetValue("usuario", out var valor))
            {
                return valor?.ToString();
            }

            return null;
        }
    }
}
