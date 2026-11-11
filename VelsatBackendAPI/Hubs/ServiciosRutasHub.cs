using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VelsatBackendAPI.Model.ServiciosRutas;

namespace VelsatBackendAPI.Hubs
{
    // Hub de Servicios Rutas: la web se conecta a /serviciosRutasHub/{usuario} y recibe en vivo
    // el evento "PosicionActualizada" (ver ServiciosRutasController.EnviarPosicion). Cada cuenta se
    // une a su propio grupo (el {usuario} de la URL), igual que TurismoMensajesHub.
    public class ServiciosRutasHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null
                && httpContext.Request.RouteValues.TryGetValue("usuario", out var valor)
                && !string.IsNullOrWhiteSpace(valor?.ToString()))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, valor!.ToString()!);
            }

            await base.OnConnectedAsync();
        }
    }

    // Última posición conocida de cada unidad, SOLO en memoria (no toca la base de datos). Sirve para que
    // la web, al abrirse o reconectarse, pinte de una vez las unidades que ya estaban en marcha sin esperar
    // al siguiente envío. Se pierde al reiniciar o redesplegar el backend (las unidades reaparecen al
    // siguiente envío del celular). Singleton.
    public class PosicionesEnVivoStore
    {
        private readonly ConcurrentDictionary<string, PosicionEnVivo> _ultimas = new();

        // Una posición más vieja que esto se considera que la unidad dejó de reportar.
        public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);

        private static string Clave(string cuenta, string deviceID) =>
            $"{cuenta.ToLowerInvariant()}|{deviceID.ToLowerInvariant()}";

        public void Guardar(string cuenta, PosicionEnVivo posicion)
        {
            _ultimas[Clave(cuenta, posicion.DeviceID)] = posicion;
        }

        public IReadOnlyList<PosicionEnVivo> Vigentes(string cuenta)
        {
            var prefijo = cuenta.ToLowerInvariant() + "|";
            var limite = DateTime.UtcNow - Vigencia;

            // Limpieza de paso: lo que lleva más de una hora sin reportar se descarta.
            var caducadas = DateTime.UtcNow.AddHours(-1);
            foreach (var par in _ultimas.Where(p => p.Value.Fecha < caducadas).ToList())
                _ultimas.TryRemove(par.Key, out _);

            return _ultimas
                .Where(p => p.Key.StartsWith(prefijo, StringComparison.Ordinal) && p.Value.Fecha >= limite)
                .Select(p => p.Value)
                .ToList();
        }
    }
}
