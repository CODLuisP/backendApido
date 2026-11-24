-- Tokens de cambio de contraseña por enlace (módulo Configuración > Seguridad).
-- Ejecutar en la base gts del servidor principal (la misma donde está `usuarios`).
-- Solo se guarda el HASH (SHA-256) del token; el token en claro viaja únicamente en el correo.
-- Tiempos en segundos Unix UTC.

CREATE TABLE IF NOT EXISTS password_reset_tokens (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    accountID   VARCHAR(32)  NOT NULL,
    token_hash  CHAR(64)     NOT NULL,
    created_at  BIGINT       NOT NULL,
    expires_at  BIGINT       NOT NULL,
    used_at     BIGINT       NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_prt_token_hash (token_hash),
    KEY ix_prt_account (accountID, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
