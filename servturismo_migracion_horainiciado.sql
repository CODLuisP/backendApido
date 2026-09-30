-- Agrega la columna que guarda la fecha/hora en que el conductor inició el servicio
-- (la pone el backend al recibir PATCH api/servturismo/{idservicio}/iniciar, no la app móvil).
ALTER TABLE servturismo
  ADD COLUMN horainiciado DATETIME NULL AFTER confirmado;
