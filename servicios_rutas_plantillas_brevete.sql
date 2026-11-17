-- =========================================================================
-- Plantillas: el conductor se identifica SOLO por el brevete.
-- Nombre y celular se leen de la tabla taxi (conductores de rutas: ruta='1', estado='A')
-- cada vez que se genera un servicio, así siempre salen actualizados. Si el brevete ya no
-- corresponde a un conductor, el servicio no se genera.
-- Ejecutar UNA vez (después de servicios_rutas_plantillas.sql).
-- =========================================================================
ALTER TABLE servicio_plantilla
    DROP COLUMN conductor_nombre,
    DROP COLUMN conductor_celular;

-- =========================================================================
-- ÍNDICES para las búsquedas del generador y de la pantalla.
-- Antes de crear cada uno, mira SHOW INDEX FROM <tabla>: si ya existe uno que empiece por las
-- mismas columnas, no hace falta crearlo.
-- =========================================================================

-- 1) Resolver el conductor por brevete (se hace por cada plantilla en cada listado y generación).
CREATE INDEX ix_taxi_usuario_brevete ON taxi (codusuario, brevete);

-- 2) Detectar si ya existe un servicio de esa ruta, fecha y hora (evita duplicar uno creado a mano).
CREATE INDEX ix_servicio_ruta_cuenta_ruta_fecha ON servicio_ruta (account_id, ruta_id, fecha_salida, hora_salida);

-- 3) Consultar lo ya generado por rango de fechas (la clave primaria empieza por plantilla_id).
CREATE INDEX ix_plantilla_generado_fecha ON servicio_plantilla_generado (fecha);
