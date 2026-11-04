-- =========================================================================
-- Módulo Servicios Rutas — ajustes sobre las tablas ya creadas (base DO)
-- Ejecutar UNA vez, en una sola sesión.
-- =========================================================================

-- 1) account_id y device_id son TEXTO en este sistema (accountID / deviceID de gts),
--    no números. Se corrigen los tipos creados originalmente como BIGINT.
--    La FK compuesta se quita y se vuelve a crear porque MySQL no deja
--    cambiar el tipo de una columna que participa en una FK.
ALTER TABLE servicio_ruta DROP FOREIGN KEY fk_servicio_ruta;

ALTER TABLE ruta
    MODIFY account_id VARCHAR(50) NOT NULL;

ALTER TABLE servicio_ruta
    MODIFY account_id VARCHAR(50) NOT NULL,
    MODIFY device_id  VARCHAR(50) NOT NULL;

ALTER TABLE servicio_ruta
    ADD CONSTRAINT fk_servicio_ruta FOREIGN KEY (ruta_id, account_id)
        REFERENCES ruta (id, account_id);

-- 2) Configuración por cuenta (tolerancia de puntualidad en minutos).
--    Si una cuenta no tiene fila, el backend usa 5 minutos por defecto.
CREATE TABLE IF NOT EXISTS servicio_ruta_config (
    account_id      VARCHAR(50) NOT NULL,
    tolerancia_min  INT         NOT NULL DEFAULT 5,
    updated_at      DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (account_id),
    CONSTRAINT ck_config_tolerancia CHECK (tolerancia_min >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
