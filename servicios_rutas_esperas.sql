-- =========================================================================
-- Avisos de espera: el pasajero avisa al conductor que está esperando en un paradero.
-- Solo llega a la app del conductor (nada a la web). Ejecutar UNA vez, en la base DO.
-- =========================================================================
CREATE TABLE IF NOT EXISTS servicio_espera (
    id                BIGINT       NOT NULL AUTO_INCREMENT,
    account_id        VARCHAR(50)  NOT NULL,
    ruta_id           BIGINT       NOT NULL,
    ruta_paradero_id  BIGINT       NOT NULL,
    pasajero_id       VARCHAR(64)  NOT NULL,                -- id anónimo del celular del pasajero
    estado            VARCHAR(10)  NOT NULL DEFAULT 'ACTIVA',   -- ACTIVA | ATENDIDA | CANCELADA
    created_at        DATETIME     NOT NULL,                -- hora de Perú
    updated_at        DATETIME     NOT NULL,                -- último aviso (vigencia 30 min)
    atendida_at       DATETIME     NULL,
    -- 1 mientras esté ACTIVA, NULL si no: así un pasajero tiene UN solo aviso activo por ruta
    -- (si presiona varias veces o cambia de paradero, se actualiza el mismo) y el historial se conserva.
    activa            TINYINT      GENERATED ALWAYS AS (IF(estado = 'ACTIVA', 1, NULL)) STORED,
    PRIMARY KEY (id),
    UNIQUE KEY uq_espera_activa (account_id, ruta_id, pasajero_id, activa),
    KEY ix_espera_ruta_estado (account_id, ruta_id, estado, updated_at),
    KEY ix_espera_paradero (ruta_paradero_id, estado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
