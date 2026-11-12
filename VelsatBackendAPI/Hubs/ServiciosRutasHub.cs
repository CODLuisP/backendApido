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
        private const string ClaveCuenta = "cuenta";
        private readonly PosicionesEnVivoStore _posiciones;

        public ServiciosRutasHub(PosicionesEnVivoStore posiciones)
        {
            _posiciones = posiciones;
        }

        // Grupo de una ruta: lo usa el controlador para enviar solo a quienes miran esa ruta.
        public static string GrupoRuta(string cuenta, long rutaId) =>
            $"{cuenta.ToLowerInvariant()}|ruta|{rutaId}";

        // Web de despacho: /serviciosRutasHub/{cuenta} -> entra al grupo de la cuenta y recibe TODAS las unidades.
        // App del pasajero: /serviciosRutasHub/{cuenta}?pasajero=1 -> NO entra al grupo de la cuenta; solo
        // recibe las unidades de las rutas a las que se une con UnirseARuta.
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null
                && httpContext.Request.RouteValues.TryGetValue("usuario", out var valor)
                && !string.IsNullOrWhiteSpace(valor?.ToString()))
            {
                var cuenta = valor!.ToString()!;
                Context.Items[ClaveCuenta] = cuenta;

                var esPasajero = httpContext.Request.Query["pasajero"] == "1";
                if (!esPasajero)
                    await Groups.AddToGroupAsync(Context.ConnectionId, cuenta);
            }

            await base.OnConnectedAsync();
        }

        // El pasajero elige una ruta: empieza a recibir "PosicionActualizada" solo de esa ruta y recibe de
        // inmediato las unidades que ya están en marcha en ella.
        public async Task<IReadOnlyList<PosicionEnVivo>> UnirseARuta(long rutaId)
        {
            var cuenta = CuentaActual();
            if (cuenta == null || rutaId <= 0) return Array.Empty<PosicionEnVivo>();

            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoRuta(cuenta, rutaId));
            return _posiciones.Vigentes(cuenta).Where(p => p.RutaId == rutaId).ToList();
        }

        // El pasajero sale de la ruta (o elige otra).
        public async Task SalirDeRuta(long rutaId)
        {
            var cuenta = CuentaActual();
            if (cuenta == null) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoRuta(cuenta, rutaId));
        }

        private string? CuentaActual() =>
            Context.Items.TryGetValue(ClaveCuenta, out var c) ? c as string : null;
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
