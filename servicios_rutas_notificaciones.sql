-- =========================================================================
-- Notificaciones del conductor (alertas, sugerencias, incidencias) hacia la web.
-- Todo se estandariza en "notificación": cambia solo el TIPO (catálogo administrable desde la web).
-- Ejecutar UNA vez, en la base DO.
-- =========================================================================

-- 1) Catálogo de tipos: son los mensajes predefinidos que el conductor ve en la app.
--    El cliente los crea, edita, ordena y activa/desactiva desde la web.
CREATE TABLE IF NOT EXISTS servicio_notificacion_tipo (
    id                   BIGINT       NOT NULL AUTO_INCREMENT,
    account_id           VARCHAR(50)  NOT NULL,
    codigo               VARCHAR(40)  NOT NULL,   -- estable, para filtros y reportes (ej. DESVIO_TRAFICO)
    categoria            VARCHAR(20)  NOT NULL,   -- SUGERENCIA | ALERTA | INCIDENCIA | INFO
    titulo               VARCHAR(120) NOT NULL,   -- lo que lee el conductor en el botón/lista
    descripcion          VARCHAR(255) NULL,       -- ayuda opcional bajo el título
    requiere_paradero    TINYINT(1)   NOT NULL DEFAULT 0,  -- la app obliga a elegir el paradero afectado
    requiere_comentario  TINYINT(1)   NOT NULL DEFAULT 0,  -- la app obliga a escribir un comentario
    activo               TINYINT(1)   NOT NULL DEFAULT 1,
    orden                INT          NOT NULL DEFAULT 0,
    created_at           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_notif_tipo_cuenta_codigo (account_id, codigo),
    KEY ix_notif_tipo_activo (account_id, activo, orden)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2) Notificaciones enviadas por los conductores (historial consultable).
--    Se guarda una copia del texto y de los datos del conductor/unidad/paradero (snapshot): si luego
--    editan el catálogo o cambian al conductor, el historial no se altera.
CREATE TABLE IF NOT EXISTS servicio_notificacion (
    id                 BIGINT        NOT NULL AUTO_INCREMENT,
    account_id         VARCHAR(50)   NOT NULL,

    -- Qué se envió
    tipo_id            BIGINT        NULL,
    tipo_codigo        VARCHAR(40)   NOT NULL,
    categoria          VARCHAR(20)   NOT NULL,
    titulo             VARCHAR(120)  NOT NULL,   -- texto del tipo al momento de enviar
    comentario         VARCHAR(500)  NULL,       -- texto libre del conductor (opcional)

    -- Contexto: de quién, en qué servicio y sobre qué paradero
    servicio_id        BIGINT        NULL,
    ruta_id            BIGINT        NULL,
    ruta_nombre        VARCHAR(150)  NULL,
    ruta_paradero_id   BIGINT        NULL,       -- paradero señalado (ej. "corrección de paradero")
    paradero_nombre    VARCHAR(200)  NULL,
    device_id          VARCHAR(50)   NULL,
    conductor_brevete  VARCHAR(30)   NULL,
    conductor_nombre   VARCHAR(150)  NULL,

    -- Dónde estaba el conductor al enviarla (obligatorio)
    latitud            DECIMAL(10,7) NOT NULL,
    longitud           DECIMAL(10,7) NOT NULL,
    precision_m        DECIMAL(7,1)  NULL,       -- precisión del GPS del celular

    -- Cuándo (hora de Perú): fecha_evento = reloj del celular; created_at = cuando llegó al servidor
    fecha_evento       DATETIME      NULL,
    created_at         DATETIME      NOT NULL,

    -- Gestión desde la web (solo informativa: no cambia ninguna ruta ni paradero)
    leida              TINYINT(1)    NOT NULL DEFAULT 0,
    leida_at           DATETIME      NULL,
    estado             VARCHAR(12)   NOT NULL DEFAULT 'PENDIENTE',   -- PENDIENTE | ACEPTADA | RECHAZADA
    respuesta          VARCHAR(500)  NULL,       -- nota del operador al aceptar o rechazar
    gestionado_por     VARCHAR(50)   NULL,
    gestionado_at      DATETIME      NULL,

    PRIMARY KEY (id),
    KEY ix_notif_cuenta_fecha (account_id, created_at),
    KEY ix_notif_cuenta_estado (account_id, estado, created_at),
    KEY ix_notif_cuenta_leida (account_id, leida),
    KEY ix_notif_servicio (servicio_id),
    KEY ix_notif_ruta (account_id, ruta_id, created_at),
    KEY ix_notif_tipo (account_id, tipo_codigo, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- 3) Mensajes predefinidos iniciales (cuenta movilbus). El cliente los puede cambiar desde la web.
--    (El COLLATE evita el error 1267: la variable usa otro collation que las tablas.)
-- =========================================================================
SET @cuenta := 'movilbus';

INSERT INTO servicio_notificacion_tipo
    (account_id, codigo, categoria, titulo, descripcion, requiere_paradero, requiere_comentario, orden)
SELECT (@cuenta COLLATE utf8mb4_unicode_ci), t.codigo, t.categoria, t.titulo, t.descripcion,
       t.req_paradero, t.req_comentario, t.orden
FROM (
    -- Sugerencias para la web (el operador decide si las acepta)
    SELECT 'CORRECCION_PARADERO' AS codigo, 'SUGERENCIA' AS categoria,
           'Corrección de paradero' AS titulo,
           'El paradero está unos metros desviado en el mapa; el conductor indica la ubicación correcta.' AS descripcion,
           1 AS req_paradero, 0 AS req_comentario, 10 AS orden
    UNION ALL SELECT 'PARADERO_NO_UTILIZABLE', 'SUGERENCIA',
           'Paradero ya no se puede usar',
           'Ya no se puede estacionar en ese punto o el paradero cambió de lugar.',
           1, 1, 20
    UNION ALL SELECT 'NUEVO_PARADERO', 'SUGERENCIA',
           'Sugerir nuevo paradero',
           'Propone un punto de parada que no existe en la ruta.',
           0, 1, 30

    -- Incidencias en el recorrido
    UNION ALL SELECT 'DESVIO_TRAFICO', 'INCIDENCIA',
           'Desvío por tráfico',
           'Tuve que tomar otra vía por congestión vehicular.',
           0, 0, 40
    UNION ALL SELECT 'DESVIO_CALLE_CERRADA', 'INCIDENCIA',
           'Desvío por calle cerrada',
           'La vía está cerrada o bloqueada y no se puede pasar.',
           0, 0, 50
    UNION ALL SELECT 'IMPOSIBLE_LLEGAR_PARADERO', 'INCIDENCIA',
           'No puedo llegar al paradero',
           'No se puede llegar o detenerse en el paradero indicado.',
           1, 0, 60
    UNION ALL SELECT 'OBRAS_VIA', 'INCIDENCIA',
           'Obras en la vía',
           'Trabajos en la pista que afectan el recorrido.',
           0, 0, 70
    UNION ALL SELECT 'MANIFESTACION_BLOQUEO', 'INCIDENCIA',
           'Manifestación o bloqueo de vía',
           'Protesta u otro bloqueo que impide el paso.',
           0, 0, 80

    -- Alertas
    UNION ALL SELECT 'RETRASO_TRAFICO', 'ALERTA',
           'Retraso por tráfico intenso',
           'Voy a llegar tarde por el tráfico.',
           0, 0, 90
    UNION ALL SELECT 'ACCIDENTE_VIA', 'ALERTA',
           'Accidente en la vía',
           'Hay un accidente en el recorrido.',
           0, 0, 100
    UNION ALL SELECT 'FALLA_MECANICA', 'ALERTA',
           'Falla mecánica de la unidad',
           'La unidad tiene un problema y puede no completar el servicio.',
           0, 1, 110
    UNION ALL SELECT 'EMERGENCIA', 'ALERTA',
           'Emergencia',
           'Situación de riesgo o emergencia a bordo.',
           0, 0, 120

    -- Otros
    UNION ALL SELECT 'OTRO', 'INFO',
           'Otro (escribir comentario)',
           'Cualquier otra situación que quieras informar.',
           0, 1, 999
) t
WHERE NOT EXISTS (
    SELECT 1 FROM servicio_notificacion_tipo x
    WHERE x.account_id = (@cuenta COLLATE utf8mb4_unicode_ci) AND x.codigo = t.codigo
);

-- Verificación: deben salir 12 tipos
SELECT id, codigo, categoria, titulo, requiere_paradero, requiere_comentario, orden
FROM servicio_notificacion_tipo
WHERE account_id = (@cuenta COLLATE utf8mb4_unicode_ci)
ORDER BY orden;
