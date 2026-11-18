-- Migración (la tabla ya existe): quita la precisión del GPS y hace obligatorio el paradero
-- en todas las notificaciones, excepto "Sugerir nuevo paradero". Ejecutar UNA vez.
ALTER TABLE servicio_notificacion DROP COLUMN precision_m;

UPDATE servicio_notificacion_tipo SET requiere_paradero = 1 WHERE codigo <> 'NUEVO_PARADERO';
UPDATE servicio_notificacion_tipo SET requiere_paradero = 0 WHERE codigo = 'NUEVO_PARADERO';

SELECT codigo, requiere_paradero, requiere_comentario FROM servicio_notificacion_tipo ORDER BY orden;
