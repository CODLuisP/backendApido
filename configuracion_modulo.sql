-- ============================================================
-- Módulo de configuración (básico) - servidor 66.240.210.125, esquema gts (DefaultConnection)
-- MySQL 5.6.38.
-- Revisar y ejecutar por bloques. No se ejecuta desde la aplicación.
-- ============================================================

USE gts;

-- 0) Respaldo de dispositivo_icono antes de tocar índices
CREATE TABLE dispositivo_icono_bak_20261008 AS SELECT * FROM dispositivo_icono;

-- 1) dispositivo_icono: una sola fila por (accountID, deviceID)
--    Se conserva la fila más reciente (mayor id) y se borran las repetidas.
DELETE di FROM dispositivo_icono di
INNER JOIN dispositivo_icono nuevo
        ON nuevo.accountID = di.accountID
       AND nuevo.deviceID  = di.deviceID
       AND nuevo.id        > di.id;

ALTER TABLE dispositivo_icono
  ADD UNIQUE KEY uq_cuenta_dispositivo (accountID, deviceID);

-- 2) Catálogo de íconos predefinidos. El usuario solo puede elegir uno de esta tabla;
--    la API copia icono_url al guardar, nunca acepta una URL libre.
CREATE TABLE IF NOT EXISTS icono_catalogo (
  id          INT(11)      NOT NULL AUTO_INCREMENT,
  nombre      VARCHAR(60)  NOT NULL,
  icono_url   VARCHAR(500) NOT NULL,
  orden       INT(11)      NOT NULL DEFAULT 0,
  activo      TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  KEY idx_activo_orden (activo, orden)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3) Cargar el catálogo (ajustar nombres y URLs reales antes de ejecutar):
-- INSERT INTO icono_catalogo (nombre, icono_url, orden) VALUES
--   ('Auto',   'https://.../iconos/auto.png',   1),
--   ('Camión', 'https://.../iconos/camion.png', 2),
--   ('Bus',    'https://.../iconos/bus.png',    3);

-- Reversa:
--   ALTER TABLE dispositivo_icono DROP INDEX uq_cuenta_dispositivo;
--   DROP TABLE icono_catalogo;
--   (las filas borradas en el paso 1 están en dispositivo_icono_bak_20261008)
