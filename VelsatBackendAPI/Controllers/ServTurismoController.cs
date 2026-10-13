using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Data.Services;
using VelsatBackendAPI.Hubs;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServTurismoController : Controller
    {
        private readonly IReadOnlyUnitOfWork _readOnlyUow;  // ✅ Para GET
        private readonly IUnitOfWork _uow;
        private readonly IFirebaseService _firebaseService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ServTurismoController> _logger;
        private readonly IHubContext<TurismoMensajesHub> _mensajesHub;

        // Grupo fijo del hub de mensajes: única cuenta de despacho de turismo hoy (igual que
        // USUARIO_UNIDADES en GetTaxis/GetUnidades).
        private const string GrupoDespachoTurismo = "movilbus";

        public ServTurismoController(
            IReadOnlyUnitOfWork readOnlyUow,
            IUnitOfWork uow,
            IFirebaseService firebaseService,
            IWhatsAppService whatsAppService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ServTurismoController> logger,
            IHubContext<TurismoMensajesHub> mensajesHub)
        {
            _readOnlyUow = readOnlyUow;
            _uow = uow;
            _firebaseService = firebaseService;
            _whatsAppService = whatsAppService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _mensajesHub = mensajesHub;
        }

        // GET api/servturismo?fechaInicio=01/07/2026&fechaFin=31/07/2026&brevete=Q12345678
        // "brevete" es opcional: lo usa la app móvil para traer solo los servicios del conductor logueado.
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string fechaInicio, [FromQuery] string fechaFin, [FromQuery] string? brevete = null)
        {
            if (string.IsNullOrEmpty(fechaInicio) || string.IsNullOrEmpty(fechaFin))
            {
                return BadRequest(new { mensaje = "Los parámetros fechaInicio y fechaFin son requeridos" });
            }

            if (!DateTime.TryParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaInicioDt)
                || !DateTime.TryParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaFinDt))
            {
                return BadRequest(new { mensaje = "Formato de fecha incorrecto. Debe ser dd/MM/yyyy" });
            }

            try
            {
                var servicios = await _readOnlyUow.ServTurismoRepository.GetByFechas(fechaInicioDt, fechaFinDt, brevete);

                if (servicios == null || servicios.Count == 0)
                {
                    return NotFound(new { mensaje = "No se encontraron servicios de turismo en el rango de fechas indicado." });
                }

                return Ok(servicios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los servicios de turismo.", error = ex.Message });
            }
        }

        // POST api/servturismo
        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] ServTurismo servicio)
        {
            if (servicio == null)
            {
                return BadRequest(new { mensaje = "El servicio no puede ser nulo." });
            }

            try
            {
                int idservicio = await _uow.ServTurismoRepository.Insert(servicio);
                _uow.SaveChanges();

                // El servicio ya quedó insertado: la notificación push se manda en segundo plano,
                // sin bloquear la respuesta (ver comentario en InsertLote sobre por qué esto es
                // necesario y no solo una optimización).
                DispararEnBackground(async sp =>
                {
                    var readOnlyUow = sp.GetRequiredService<IReadOnlyUnitOfWork>();
                    var firebaseService = sp.GetRequiredService<IFirebaseService>();

                    foreach (var brevete in BrevetesPilotoYCopiloto(new[] { servicio }))
                    {
                        await NotificarConductorAsync(readOnlyUow, firebaseService, brevete);
                    }
                });

                return Ok(new { mensaje = "Servicio de turismo creado correctamente.", idservicio });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear el servicio de turismo.", error = ex.Message });
            }
        }

        // POST api/servturismo/lote
        // Inserción en lote (ej. carga desde Excel en el front). Ejecuta INSERTs multi-VALUES por bloques
        // para minimizar los round-trips a la base de datos y evitar lentitud con listas grandes.
        //
        // IMPORTANTE: responde apenas termina el SaveChanges, antes de notificar. Antes, la respuesta
        // esperaba a que se mandaran TODOS los push y WhatsApp; si el gateway de WhatsApp estaba caído
        // o desvinculado, esas llamadas podían demorar ~100s (timeout default de HttpClient) y la
        // respuesta tardaba tanto que el front (useServiciosTurismo.cargarServiciosExcel) la trataba
        // como fallo de red, guardaba el lote localmente como pendiente y lo reintentaba solo — pese a
        // que el lote ya estaba insertado en BD, duplicando (hasta triplicando) los servicios cargados.
        [HttpPost("lote")]
        public async Task<IActionResult> InsertLote([FromBody] List<ServTurismo> servicios)
        {
            if (servicios == null || servicios.Count == 0)
            {
                return BadRequest(new { mensaje = "La lista de servicios está vacía o es nula." });
            }

            try
            {
                int insertados = await _uow.ServTurismoRepository.InsertBatch(servicios);
                _uow.SaveChanges();

                DispararEnBackground(async sp =>
                {
                    var readOnlyUow = sp.GetRequiredService<IReadOnlyUnitOfWork>();
                    var firebaseService = sp.GetRequiredService<IFirebaseService>();
                    var whatsAppService = sp.GetRequiredService<IWhatsAppService>();

                    foreach (var brevete in BrevetesPilotoYCopiloto(servicios))
                    {
                        await NotificarConductorAsync(readOnlyUow, firebaseService, brevete);
                    }

                    await NotificarLotePorWhatsappAsync(readOnlyUow, whatsAppService, servicios);
                });

                return Ok(new { mensaje = "Servicios de turismo insertados correctamente. Se están notificando a los conductores.", insertados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al insertar los servicios de turismo en lote.", error = ex.Message });
            }
        }

        // Ejecuta "trabajo" en segundo plano, en su propio scope de DI (los servicios scoped como
        // _uow/_readOnlyUow no sobreviven más allá del request; acá se resuelven instancias nuevas
        // que sí siguen vivas después de responder). Los métodos que llama ya atrapan sus propias
        // excepciones, pero se protege igual para nunca dejar una excepción no observada.
        private void DispararEnBackground(Func<IServiceProvider, Task> trabajo)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    await trabajo(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notificando en segundo plano tras carga de servicios de turismo.");
                }
            });
        }

        private const string MensajeWhatsappCargaLote = "Hola, tienes un servicio de turismo asignado. Revisa el detalle en tu app.";

        // Avisa por WhatsApp a los conductores (brevete y cobrevete) de una carga en lote. El teléfono se
        // consulta acá en la tabla taxi al momento de enviar: se ignoran las columnas celular/cocelular del
        // Excel y nada del destino viene del cliente. Un conductor con varios servicios recibe un solo
        // mensaje. Corre en segundo plano (ver DispararEnBackground) y no debe afectar la respuesta.
        private static async Task<(int Enviados, int Fallidos)> NotificarLotePorWhatsappAsync(
            IReadOnlyUnitOfWork readOnlyUow,
            IWhatsAppService whatsAppService,
            IEnumerable<ServTurismo> servicios)
        {
            try
            {
                var brevetes = BrevetesPilotoYCopiloto(servicios);

                if (brevetes.Count == 0)
                {
                    return (0, 0);
                }

                var telefonosPorBrevete = await readOnlyUow.ServTurismoRepository.GetTelefonosPorBrevete(brevetes);

                var telefonos = telefonosPorBrevete.Values
                    .Select(NormalizarCelularPeru)
                    .Where(t => t != null)
                    .Select(t => t!)
                    .Distinct()
                    .ToList();

                var resultados = await Task.WhenAll(
                    telefonos.Select(t => whatsAppService.EnviarMensajeAsync(t, MensajeWhatsappCargaLote)));

                int enviados = resultados.Count(ok => ok);
                return (enviados, telefonos.Count - enviados);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notificando por WhatsApp la carga en lote de turismo: {ex.Message}");
                return (0, 0);
            }
        }

        // Brevetes de piloto y copiloto de los servicios dados, sin vacíos ni repetidos (un conductor
        // con varios servicios, o que figura como piloto y copiloto, se notifica una sola vez).
        private static List<string> BrevetesPilotoYCopiloto(IEnumerable<ServTurismo> servicios)
        {
            return servicios
                .SelectMany(s => new[] { s.Brevete, s.Cobrevete })
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Notifica por push al conductor dueño de "brevete" y devuelve si el push salió. Se usa en
        // segundo plano (ver DispararEnBackground) tras Insert/lote, donde el resultado se ignora, y en
        // la notificación manual, donde sí se informa. Nunca lanza: un conductor sin app instalada
        // (sin token) o un error de Firebase solo devuelven false. titulo/cuerpo son configurables
        // porque se reusa para "nuevo servicio asignado" (Insert/lote) y para las plantillas manuales.
        private static async Task<bool> NotificarConductorAsync(
            IReadOnlyUnitOfWork readOnlyUow,
            IFirebaseService firebaseService,
            string? brevete,
            string titulo = "Nuevo servicio de turismo",
            string cuerpo = "Se te ha asignado un nuevo servicio de turismo. Revisa la app para ver el detalle.")
        {
            if (string.IsNullOrWhiteSpace(brevete))
            {
                return false;
            }

            try
            {
                var token = await readOnlyUow.NotificacionesRepository.GetFCMTokenByBreveteAsync(brevete);

                if (token == null)
                {
                    return false;
                }

                var resultado = await firebaseService.SendPushNotificationAsync(
                    token.FCMToken,
                    titulo,
                    cuerpo);

                if (resultado.TokenInvalido)
                {
                    await readOnlyUow.NotificacionesRepository.DeleteFCMTokenAsync(token.FCMToken);
                }

                return resultado.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notificando servicio de turismo: {ex.Message}");
                return false;
            }
        }

        // PATCH api/servturismo/{idservicio}?limpiarNulos=true&usuario=jperez&motivo=Cambio+de+solicitud+del+cliente
        // limpiarNulos=true: los campos que llegan en null SÍ se aplican (borran el dato en la BD).
        // Pensado para el formulario de edición del front, que siempre envía el objeto completo.
        // usuario/motivo quedan en servturismo_auditoria por cada campo que cambió de valor (motivo es opcional).
        [HttpPatch("{idservicio}")]
        public async Task<IActionResult> Patch(int idservicio, [FromBody] ServTurismo campos, [FromQuery] bool limpiarNulos = false, [FromQuery] string? usuario = null, [FromQuery] string? motivo = null)
        {
            if (campos == null)
            {
                return BadRequest(new { mensaje = "El servicio no puede ser nulo." });
            }

            try
            {
                // La edición manual ya NO notifica automáticamente (ni push ni WhatsApp): queda a
                // criterio del operador, que dispara la notificación a mano desde la tabla web
                // (PATCH {idservicio}/notificar) eligiendo la plantilla que corresponda.
                var (filasAfectadas, _, _) = await _uow.ServTurismoRepository.Patch(idservicio, campos, limpiarNulos, usuario, motivo);

                if (filasAfectadas == -1)
                {
                    return Conflict(new { mensaje = "El servicio está cancelado y ya no se puede editar." });
                }
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Servicio de turismo actualizado correctamente.", filasAfectadas });
                }

                return NotFound(new { mensaje = "No se encontró el servicio o no se realizaron cambios." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el servicio de turismo.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/visto
        // La app móvil lo llama automáticamente cuando el servicio se muestra en pantalla
        // (equivale al doble check azul de WhatsApp). Es idempotente.
        [HttpPatch("{idservicio}/visto")]
        public async Task<IActionResult> MarcarVisto(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarVisto(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio marcado como visto.", idservicio, visto = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al marcar el servicio como visto.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/confirmado
        // Obsoleto: la app móvil nueva usa /iniciar (que además guarda la hora). Se mantiene para
        // las versiones anteriores de la app. Es idempotente.
        [HttpPatch("{idservicio}/confirmado")]
        public async Task<IActionResult> MarcarConfirmado(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarConfirmado(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio confirmado por el conductor.", idservicio, confirmado = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al confirmar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/iniciar
        // La app móvil lo llama cuando el conductor presiona "Iniciar". Marca confirmado = 1 y guarda
        // la hora de inicio (la pone el servidor). Es idempotente.
        [HttpPatch("{idservicio}/iniciar")]
        public async Task<IActionResult> MarcarIniciado(int idservicio)
        {
            try
            {
                DateTime? horaIniciado = await _uow.ServTurismoRepository.MarcarIniciado(idservicio);
                _uow.SaveChanges();

                if (horaIniciado == null)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio iniciado por el conductor.", idservicio, confirmado = 1, horainiciado = horaIniciado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al iniciar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/finalizar
        // La app móvil lo llama cuando el conductor presiona "Finalizar" y confirma
        // el modal de advertencia (acción irreversible). Es idempotente.
        [HttpPatch("{idservicio}/finalizar")]
        public async Task<IActionResult> MarcarFinalizado(int idservicio)
        {
            try
            {
                DateTime? horaFinalizado = await _uow.ServTurismoRepository.MarcarFinalizado(idservicio);
                _uow.SaveChanges();

                if (horaFinalizado == null)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio finalizado por el conductor.", idservicio, finalizado = 1, horafinalizado = horaFinalizado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al finalizar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/cancelar
        // Cancela el servicio (estado = "Cancelado") en vez de eliminarlo físicamente. Es idempotente.
        [HttpPatch("{idservicio}/cancelar")]
        public async Task<IActionResult> MarcarCancelado(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarCancelado(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio cancelado correctamente.", idservicio, cancelado = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cancelar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/standby
        // Pausa el servicio (el conductor deja de verlo) sin cancelarlo definitivamente. Es idempotente.
        [HttpPatch("{idservicio}/standby")]
        public async Task<IActionResult> MarcarStandby(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarStandby(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio puesto en Stand By correctamente.", idservicio, standby = 1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al poner el servicio en Stand By.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/reanudar
        // Reanuda un servicio que estaba en Stand By, volviéndolo a mostrar al conductor. Es idempotente.
        [HttpPatch("{idservicio}/reanudar")]
        public async Task<IActionResult> MarcarReanudar(int idservicio)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarReanudar(idservicio);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                return Ok(new { mensaje = "Servicio reanudado correctamente.", idservicio, standby = 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al reanudar el servicio.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/{idservicio}/notificar
        // Notificación manual disparada desde el botón de la tabla web (una por servicio, con
        // plantilla a elegir: revisión / cambio / cancelación). Se envía al piloto Y al copiloto, por
        // los DOS canales: WhatsApp y push de la app (Firebase) — cada canal y cada conductor se
        // intentan de forma independiente, así que si uno falla (sin celular, o sin la app instalada)
        // lo demás igual se envía. El teléfono y el token FCM SIEMPRE se resuelven acá contra la ficha
        // del conductor en BD (tabla taxi, por brevete/cobrevete del servicio): el cliente solo manda
        // "tipo", nunca el texto ni el destino, así no se puede notificar algo arbitrario.
        [HttpPatch("{idservicio}/notificar")]
        public async Task<IActionResult> NotificarPorWhatsapp(int idservicio, [FromBody] NotificarConductorRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Tipo))
            {
                return BadRequest(new { mensaje = "El tipo de plantilla es requerido." });
            }

            try
            {
                var datos = await _readOnlyUow.ServTurismoRepository.GetDatosNotificacion(idservicio);

                if (datos == null)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                string fecha = datos.Fechainicio?.ToString("dd/MM/yyyy") ?? "-";
                string hora = datos.Horainicio.HasValue
                    ? DateTime.Today.Add(datos.Horainicio.Value).ToString("HH:mm")
                    : "-";

                (string Titulo, string Cuerpo)? plantilla = body.Tipo switch
                {
                    "revision" => ("Revisa tus servicios", $"Revisa tus servicios asignados para el día {fecha}"),
                    "cambio" => ("Servicio de turismo actualizado", $"Tu servicio del día {fecha} de las {hora} tuvo una modificación"),
                    "cancelacion" => ("Servicio de turismo cancelado", $"Tu servicio del día {fecha} de las {hora} fue cancelado"),
                    _ => null,
                };

                if (plantilla == null)
                {
                    return BadRequest(new { mensaje = "Tipo de plantilla no reconocido." });
                }

                var (titulo, mensaje) = plantilla.Value;

                // Piloto y copiloto (si lo hay). Si ambos tienen el mismo brevete se notifica una vez.
                string? brevetePiloto = datos.Brevete?.Trim();
                string? breveteCopiloto = datos.Cobrevete?.Trim();

                var destinatarios = new List<(string Rol, string Brevete)>();
                if (!string.IsNullOrEmpty(brevetePiloto))
                {
                    destinatarios.Add(("Piloto", brevetePiloto));
                }
                if (!string.IsNullOrEmpty(breveteCopiloto)
                    && !string.Equals(breveteCopiloto, brevetePiloto, StringComparison.OrdinalIgnoreCase))
                {
                    destinatarios.Add(("Copiloto", breveteCopiloto));
                }

                // WhatsApp y push se intentan de forma independiente por cada uno: el que no tenga
                // celular en su ficha solo recibe el push, y el que no tenga la app solo el WhatsApp.
                var telefonosPorBrevete = await _readOnlyUow.ServTurismoRepository.GetTelefonosPorBrevete(
                    destinatarios.Select(d => d.Brevete));
                var telefonos = destinatarios
                    .Select(d => telefonosPorBrevete.TryGetValue(d.Brevete, out var t) ? NormalizarCelularPeru(t) : null)
                    .ToList();

                // Si piloto y copiloto comparten número, sale un solo WhatsApp.
                var telefonosDistintos = telefonos.Where(t => t != null).Select(t => t!).Distinct().ToList();
                var resultadosWhatsapp = await Task.WhenAll(
                    telefonosDistintos.Select(t => _whatsAppService.EnviarMensajeAsync(t, mensaje)));
                var whatsappPorTelefono = telefonosDistintos
                    .Zip(resultadosWhatsapp, (telefono, ok) => (telefono, ok))
                    .ToDictionary(x => x.telefono, x => x.ok);

                // Secuencial: las consultas del token comparten la conexión de _readOnlyUow.
                var resultados = new List<(string Rol, bool Whatsapp, bool Push, bool TieneCelular)>();
                for (int i = 0; i < destinatarios.Count; i++)
                {
                    var (rol, brevete) = destinatarios[i];
                    string? telefono = telefonos[i];
                    bool whatsapp = telefono != null && whatsappPorTelefono[telefono];
                    bool push = await NotificarConductorAsync(_readOnlyUow, _firebaseService, brevete, titulo, mensaje);
                    resultados.Add((rol, whatsapp, push, telefono != null));
                }

                bool whatsappEnviado = resultados.Any(r => r.Whatsapp);
                bool pushEnviado = resultados.Any(r => r.Push);
                string detalle = string.Join(". ", resultados.Select(r => DescribirEnvio(r.Rol, r.Whatsapp, r.Push, r.TieneCelular)));
                var porConductor = resultados.Select(r => new { rol = r.Rol, whatsappEnviado = r.Whatsapp, pushEnviado = r.Push });

                // Solo es error si no le llegó nada a nadie; si al menos uno recibió algo, es éxito
                // y el detalle dice qué le faltó a cada uno.
                if (!whatsappEnviado && !pushEnviado)
                {
                    return StatusCode(502, new
                    {
                        mensaje = "No se pudo enviar la notificación (ni por WhatsApp ni en la app)." +
                                  (detalle.Length > 0 ? $" {detalle}." : ""),
                        whatsappEnviado,
                        pushEnviado,
                        conductores = porConductor,
                    });
                }

                return Ok(new { mensaje = $"Notificación enviada. {detalle}.", whatsappEnviado, pushEnviado, conductores = porConductor });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al notificar al conductor.", error = ex.Message });
            }
        }

        // POST api/servturismo/notificar-revision?fecha=11/10/2026   body: { brevetes: [...] }
        // Botón "Notificar revisión a todos" de la tabla web: manda la plantilla "revision" (la única
        // permitida en masa; cambio/cancelación son por servicio) a los conductores elegidos en el modal.
        // Solo se respetan los brevetes que figuran como piloto o copiloto en algún servicio de esa
        // fecha que no esté cancelado ni en stand by (los que el conductor ve en su app). Un conductor
        // con varios servicios recibe un solo mensaje. Igual que NotificarPorWhatsapp, se intenta
        // WhatsApp y push de forma independiente, y el destino siempre se resuelve acá en BD.
        [HttpPost("notificar-revision")]
        public async Task<IActionResult> NotificarRevisionTodos([FromQuery] string fecha, [FromBody] NotificarRevisionRequest body)
        {
            if (!DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaDt))
            {
                return BadRequest(new { mensaje = "Formato de fecha incorrecto. Debe ser dd/MM/yyyy" });
            }

            if (body?.Brevetes == null || body.Brevetes.Count == 0)
            {
                return BadRequest(new { mensaje = "Selecciona al menos un conductor." });
            }

            try
            {
                var servicios = await _readOnlyUow.ServTurismoRepository.GetByFechas(fechaDt, fechaDt);

                var elegidos = new HashSet<string>(
                    body.Brevetes.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                var brevetes = servicios
                    .Where(s => s.Cancelado != 1 && s.Standby != 1)
                    .SelectMany(s => new[] { s.Brevete, s.Cobrevete })
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Select(b => b!.Trim())
                    .Where(elegidos.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (brevetes.Count == 0)
                {
                    return NotFound(new { mensaje = "Ninguno de los conductores elegidos tiene servicios activos en esa fecha." });
                }

                const string titulo = "Revisa tus servicios";
                string mensaje = $"Revisa tus servicios asignados para el día {fechaDt:dd/MM/yyyy}";

                var telefonosPorBrevete = await _readOnlyUow.ServTurismoRepository.GetTelefonosPorBrevete(brevetes);
                var telefonos = brevetes
                    .Select(b => telefonosPorBrevete.TryGetValue(b, out var t) ? NormalizarCelularPeru(t) : null)
                    .ToList();
                int sinTelefono = telefonos.Count(t => t == null);

                // Dos fichas con el mismo número reciben un solo WhatsApp.
                var telefonosDistintos = telefonos.Where(t => t != null).Select(t => t!).Distinct().ToList();
                var resultadosWhatsapp = await Task.WhenAll(
                    telefonosDistintos.Select(t => _whatsAppService.EnviarMensajeAsync(t, mensaje)));
                int whatsappEnviados = resultadosWhatsapp.Count(ok => ok);

                // Secuencial: las consultas comparten la conexión de _readOnlyUow.
                int pushEnviados = 0;
                foreach (var brevete in brevetes)
                {
                    var token = await _readOnlyUow.NotificacionesRepository.GetFCMTokenByBreveteAsync(brevete);
                    if (token == null)
                    {
                        continue;
                    }

                    var resultadoPush = await _firebaseService.SendPushNotificationAsync(token.FCMToken, titulo, mensaje);
                    if (resultadoPush.Success)
                    {
                        pushEnviados++;
                    }

                    if (resultadoPush.TokenInvalido)
                    {
                        await _readOnlyUow.NotificacionesRepository.DeleteFCMTokenAsync(token.FCMToken);
                    }
                }

                int conductores = brevetes.Count;
                int whatsappFallidos = telefonosDistintos.Count - whatsappEnviados;

                if (whatsappEnviados == 0 && pushEnviados == 0)
                {
                    return StatusCode(502, new
                    {
                        mensaje = "No se pudo notificar a ningún conductor (ni por WhatsApp ni en la app).",
                        conductores, whatsappEnviados, whatsappFallidos, sinTelefono, pushEnviados,
                    });
                }

                string mensajeRespuesta =
                    $"Revisión notificada a {conductores} conductor(es): {whatsappEnviados} por WhatsApp, {pushEnviados} en la app." +
                    (sinTelefono > 0 ? $" {sinTelefono} sin celular válido en su ficha." : "") +
                    (whatsappFallidos > 0 ? $" {whatsappFallidos} WhatsApp no se pudieron enviar." : "");

                return Ok(new { mensaje = mensajeRespuesta, conductores, whatsappEnviados, whatsappFallidos, sinTelefono, pushEnviados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al notificar la revisión a los conductores.", error = ex.Message });
            }
        }

        // Texto corto para el toast del front, ej. "Piloto: WhatsApp y app" / "Copiloto: solo app (sin celular)".
        private static string DescribirEnvio(string rol, bool whatsapp, bool push, bool tieneCelular)
        {
            return (whatsapp, push) switch
            {
                (true, true) => $"{rol}: WhatsApp y app",
                (true, false) => $"{rol}: solo WhatsApp (sin app)",
                (false, true) => tieneCelular ? $"{rol}: solo app (falló WhatsApp)" : $"{rol}: solo app (sin celular)",
                _ => tieneCelular ? $"{rol}: no se pudo notificar" : $"{rol}: no se pudo notificar (sin celular ni app)",
            };
        }

        // Acepta 9 dígitos (celular peruano sin código de país) o 11 (ya con el prefijo 51).
        private static string? NormalizarCelularPeru(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            string digitos = new string(valor.Where(char.IsDigit).ToArray());
            if (digitos.Length == 9) return $"51{digitos}";
            if (digitos.Length == 11 && digitos.StartsWith("51")) return digitos;
            return null;
        }

        // GET api/servturismo/{idservicio}/auditoria
        // Historial de ediciones manuales del servicio (campo, valor anterior/nuevo, usuario, motivo, fecha),
        // más reciente primero. Pensado para el panel expandible de "Historial de cambios" en el front.
        [HttpGet("{idservicio}/auditoria")]
        public async Task<IActionResult> GetAuditoria(int idservicio)
        {
            try
            {
                var auditoria = await _readOnlyUow.ServTurismoRepository.GetAuditoria(idservicio);
                return Ok(auditoria);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el historial del servicio.", error = ex.Message });
            }
        }

        // DELETE api/servturismo/{idservicio}
        [HttpDelete("{idservicio}")]
        public async Task<IActionResult> Delete(int idservicio)
        {
            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.Delete(idservicio);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Servicio de turismo eliminado correctamente." });
                }

                return NotFound(new { mensaje = "No se encontró el servicio a eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar el servicio de turismo.", error = ex.Message });
            }
        }

        // DELETE api/servturismo/fecha/01/07/2026
        // Elimina físicamente TODOS los servicios de la fecha indicada (borrado masivo, "Eliminar carga").
        // Pensado para deshacer una carga de Excel completa cargada por error. Acción irreversible.
        [HttpDelete("fecha/{*fecha}")]
        public async Task<IActionResult> DeleteByFecha(string fecha)
        {
            if (!DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaDt))
            {
                return BadRequest(new { mensaje = "Formato de fecha incorrecto. Debe ser dd/MM/yyyy" });
            }

            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.DeleteByFecha(fechaDt);
                _uow.SaveChanges();

                return Ok(new { mensaje = "Servicios de turismo eliminados correctamente.", filasAfectadas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar los servicios de turismo de la fecha indicada.", error = ex.Message });
            }
        }

        // ===================== MENSAJES / SOLICITUDES DEL CONDUCTOR =====================

        // POST api/servturismo/{idservicio}/mensaje
        // La app móvil lo llama cuando el conductor manda una observación o una solicitud
        // (ampliación de servicio) desde el botón "Mensaje / Solicitud". Guarda el mensaje y lo
        // notifica en vivo al front de despacho por SignalR (grupo GrupoDespachoTurismo); si el
        // front estaba desconectado, lo recupera con GetMensajesPendientes al reconectar.
        [HttpPost("{idservicio}/mensaje")]
        public async Task<IActionResult> EnviarMensaje(int idservicio, [FromBody] EnviarMensajeTurismoRequest body)
        {
            if (body == null || (body.Tipo != "observacion" && body.Tipo != "ampliacion"))
            {
                return BadRequest(new { mensaje = "El tipo de mensaje debe ser 'observacion' o 'ampliacion'." });
            }

            if (body.Tipo == "observacion" && string.IsNullOrWhiteSpace(body.Texto))
            {
                return BadRequest(new { mensaje = "El texto de la observación es requerido." });
            }

            if (body.Tipo == "ampliacion" && !body.Horas.HasValue)
            {
                return BadRequest(new { mensaje = "La cantidad de horas es requerida." });
            }

            try
            {
                var mensajeCreado = await _uow.ServTurismoRepository.InsertMensaje(idservicio, body.Tipo, body.Texto, body.Horas);
                _uow.SaveChanges();

                if (mensajeCreado == null)
                {
                    return NotFound(new { mensaje = "No se encontró el servicio." });
                }

                try
                {
                    await _mensajesHub.Clients.Group(GrupoDespachoTurismo).SendAsync("NuevoMensajeTurismo", mensajeCreado);
                }
                catch (Exception ex)
                {
                    // El mensaje ya quedó guardado; si SignalR falla, el front igual lo recupera
                    // con GetMensajesPendientes, así que no se responde error al conductor.
                    _logger.LogError(ex, "[SignalR] Error notificando mensaje de turismo idmensaje={Idmensaje}", mensajeCreado.Idmensaje);
                }

                return Ok(new { mensaje = "Mensaje enviado correctamente.", idmensaje = mensajeCreado.Idmensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al enviar el mensaje.", error = ex.Message });
            }
        }

        // GET api/servturismo/mensajes/pendientes
        // El front lo consulta al cargar la pantalla para recuperar las alertas que llegaron
        // mientras estaba desconectado (SignalR solo entrega en vivo a quien está conectado).
        [HttpGet("mensajes/pendientes")]
        public async Task<IActionResult> GetMensajesPendientes()
        {
            try
            {
                var mensajes = await _readOnlyUow.ServTurismoRepository.GetMensajesPendientes();
                return Ok(mensajes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los mensajes pendientes.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/mensajes/{idmensaje}/atendido
        // El front lo llama cuando el operador cierra la alerta manualmente. Es idempotente.
        [HttpPatch("mensajes/{idmensaje}/atendido")]
        public async Task<IActionResult> MarcarMensajeAtendido(int idmensaje)
        {
            try
            {
                bool existe = await _uow.ServTurismoRepository.MarcarMensajeAtendido(idmensaje);
                _uow.SaveChanges();

                if (!existe)
                {
                    return NotFound(new { mensaje = "No se encontró el mensaje." });
                }

                return Ok(new { mensaje = "Mensaje marcado como atendido.", idmensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al marcar el mensaje como atendido.", error = ex.Message });
            }
        }

        // ===================== TAXI (CONDUCTORES) CRUD =====================

        // GET api/servturismo/taxi?codusuario=...
        [HttpGet("taxi")]
        public async Task<IActionResult> GetTaxis([FromQuery] string codusuario)
        {
            if (string.IsNullOrEmpty(codusuario))
            {
                return BadRequest(new { mensaje = "El parámetro codusuario es requerido" });
            }

            try
            {
                var taxis = await _readOnlyUow.ServTurismoRepository.GetTaxis(codusuario);

                if (taxis == null || taxis.Count == 0)
                {
                    return NotFound(new { mensaje = "No se encontraron conductores." });
                }

                return Ok(taxis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener los conductores.", error = ex.Message });
            }
        }

        // GET api/servturismo/taxi/{codtaxi}
        [HttpGet("taxi/{codtaxi}")]
        public async Task<IActionResult> GetTaxiById(int codtaxi)
        {
            try
            {
                var taxi = await _readOnlyUow.ServTurismoRepository.GetTaxiById(codtaxi);

                if (taxi == null)
                {
                    return NotFound(new { mensaje = "No se encontró el conductor." });
                }

                return Ok(taxi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el conductor.", error = ex.Message });
            }
        }

        // POST api/servturismo/taxi
        [HttpPost("taxi")]
        public async Task<IActionResult> InsertTaxi([FromBody] ConductorTurismo taxi)
        {
            if (taxi == null)
            {
                return BadRequest(new { mensaje = "El conductor no puede ser nulo." });
            }

            try
            {
                int codtaxi = await _uow.ServTurismoRepository.InsertTaxi(taxi);
                _uow.SaveChanges();

                return Ok(new { mensaje = "Conductor creado correctamente.", codtaxi });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear el conductor.", error = ex.Message });
            }
        }

        // PATCH api/servturismo/taxi/{codtaxi}
        [HttpPatch("taxi/{codtaxi}")]
        public async Task<IActionResult> PatchTaxi(int codtaxi, [FromBody] ConductorTurismo campos)
        {
            if (campos == null)
            {
                return BadRequest(new { mensaje = "El conductor no puede ser nulo." });
            }

            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.PatchTaxi(codtaxi, campos);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Conductor actualizado correctamente.", filasAfectadas });
                }

                return NotFound(new { mensaje = "No se encontró el conductor o no se realizaron cambios." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el conductor.", error = ex.Message });
            }
        }

        // DELETE api/servturismo/taxi/{codtaxi}
        [HttpDelete("taxi/{codtaxi}")]
        public async Task<IActionResult> DeleteTaxi(int codtaxi)
        {
            try
            {
                int filasAfectadas = await _uow.ServTurismoRepository.DeleteTaxi(codtaxi);
                _uow.SaveChanges();

                if (filasAfectadas > 0)
                {
                    return Ok(new { mensaje = "Conductor eliminado correctamente." });
                }

                return NotFound(new { mensaje = "No se encontró el conductor a eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar el conductor.", error = ex.Message });
            }
        }
    }
}
