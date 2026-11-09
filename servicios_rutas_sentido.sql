-- Sentido de la ruta (IDA / REGRESO) y su ruta "pareja" (la del sentido contrario).
-- Cada sentido es una ruta propia (con su trazado y sus paraderos), porque el regreso puede usar
-- otras calles. La pareja se guarda en las dos rutas: A.ruta_pareja_id = B y B.ruta_pareja_id = A.
-- Las rutas ya existentes quedan como 'IDA' y sin pareja.

ALTER TABLE ruta
    ADD COLUMN sentido ENUM('IDA','REGRESO') NOT NULL DEFAULT 'IDA' AFTER grupo,
    ADD COLUMN ruta_pareja_id BIGINT NULL AFTER sentido;

-- Aparte, para que las columnas queden aunque esto falle. Si da error de tipos incompatibles,
-- revisa el tipo de ruta.id con SHOW CREATE TABLE ruta (ej. BIGINT UNSIGNED) y usa el mismo en ruta_pareja_id.
ALTER TABLE ruta
    ADD CONSTRAINT fk_ruta_pareja FOREIGN KEY (ruta_pareja_id) REFERENCES ruta (id) ON DELETE SET NULL;

-- Verificación
SELECT id, codigo, nombre, grupo, sentido, ruta_pareja_id FROM ruta WHERE account_id = 'movilbus' ORDER BY codigo;
