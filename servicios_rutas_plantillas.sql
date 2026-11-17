-- =========================================================================
-- Servicios automáticos de Rutas: plantillas.
-- Una plantilla = un servicio que se repite (ruta + hora de salida + unidad + conductor + días).
-- El backend (ServiciosRutasAutomaticosService) crea cada día los servicios de las plantillas
-- COMPLETAS (hora, unidad y conductor con nombre, brevete y celular). Las incompletas se ignoran.
-- Ejecutar UNA vez, en la base DO.
-- =========================================================================

CREATE TABLE IF NOT EXISTS servicio_plantilla (
    id                 BIGINT       NOT NULL AUTO_INCREMENT,
    account_id         VARCHAR(50)  NOT NULL,
    ruta_id            BIGINT       NOT NULL,
    hora_salida        TIME         NULL,
    device_id          VARCHAR(50)  NULL,
    conductor_nombre   VARCHAR(150) NULL,
    conductor_brevete  VARCHAR(30)  NULL,
    conductor_celular  VARCHAR(30)  NULL,
    -- Días de la semana en que se genera: 1=lunes ... 7=domingo (lista separada por comas).
    dias               VARCHAR(20)  NOT NULL DEFAULT '1,2,3,4,5,6,7',
    activo             TINYINT(1)   NOT NULL DEFAULT 1,
    created_at         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_plantilla_cuenta (account_id, activo),
    KEY ix_plantilla_ruta (ruta_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Registro de lo ya generado: garantiza que no se duplique y que, si un operador elimina o cancela
-- un servicio generado, el sistema no lo vuelva a crear.
CREATE TABLE IF NOT EXISTS servicio_plantilla_generado (
    plantilla_id  BIGINT   NOT NULL,
    fecha         DATE     NOT NULL,
    servicio_id   BIGINT   NULL,
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (plantilla_id, fecha)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- Carga inicial: 9 plantillas (una por ruta, sentido IDA, lunes a domingo), con la hora del
-- primer paradero de cada Excel. Unidad y conductor quedan vacíos: se completan desde la web.
--   San Isidro: Ruta 1 06:30 | Ruta 2 06:00 | Ruta 3 05:58 | Ruta 4 06:00 | Ruta 5 06:35
--   San Juan de Miraflores: Ruta 6 05:50 | Ruta 7 06:15 | Ruta 8 05:45 | Ruta 9 06:55
-- (El COLLATE evita el error 1267: la variable usa otro collation que las tablas.)
-- La ruta se busca por el número del código (RT-001 -> 1, RT-006 -> 6...) en sentido IDA.
-- Revisa los SELECT del final: deben salir 9 filas y 0 rutas sin encontrar.
-- =========================================================================
SET @cuenta := 'movilbus';

INSERT INTO servicio_plantilla (account_id, ruta_id, hora_salida, dias)
SELECT (@cuenta COLLATE utf8mb4_unicode_ci), r.id, h.hora, '1,2,3,4,5,6,7'
FROM (
    SELECT 1 AS num, '06:30:00' AS hora UNION ALL
    SELECT 2, '06:00:00' UNION ALL
    SELECT 3, '05:58:00' UNION ALL
    SELECT 4, '06:00:00' UNION ALL
    SELECT 5, '06:35:00' UNION ALL
    SELECT 6, '05:50:00' UNION ALL
    SELECT 7, '06:15:00' UNION ALL
    SELECT 8, '05:45:00' UNION ALL
    SELECT 9, '06:55:00'
) h
JOIN ruta r
  ON r.account_id = (@cuenta COLLATE utf8mb4_unicode_ci)
 AND r.activo = 1
 AND r.sentido = 'IDA'
 AND CAST(REGEXP_SUBSTR(r.codigo, '[0-9]+') AS UNSIGNED) = h.num
WHERE NOT EXISTS (
    SELECT 1 FROM servicio_plantilla p WHERE p.account_id = (@cuenta COLLATE utf8mb4_unicode_ci) AND p.ruta_id = r.id
);

-- Verificación 1: plantillas creadas
SELECT p.id, r.codigo, r.nombre, r.grupo, p.hora_salida, p.dias, p.device_id, p.conductor_nombre
FROM servicio_plantilla p JOIN ruta r ON r.id = p.ruta_id
WHERE p.account_id = (@cuenta COLLATE utf8mb4_unicode_ci)
ORDER BY CAST(REGEXP_SUBSTR(r.codigo, '[0-9]+') AS UNSIGNED);

-- Verificación 2: números de ruta (1..9) que NO encontraron una ruta IDA activa
SELECT n.num AS ruta_sin_encontrar
FROM (SELECT 1 num UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5
      UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9) n
WHERE NOT EXISTS (
    SELECT 1 FROM ruta r
    WHERE r.account_id = (@cuenta COLLATE utf8mb4_unicode_ci) AND r.activo = 1 AND r.sentido = 'IDA'
      AND CAST(REGEXP_SUBSTR(r.codigo, '[0-9]+') AS UNSIGNED) = n.num
);

-- Nota: el intervalo de revisión (30 minutos) es interno del backend y no se guarda en la base.
-- Si ya habías ejecutado ADD COLUMN intervalo_generacion_min, esa columna ya no se usa y puedes dejarla
-- o quitarla con: ALTER TABLE servicio_ruta_config DROP COLUMN intervalo_generacion_min;
