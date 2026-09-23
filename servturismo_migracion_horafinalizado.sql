-- Agrega la columna que guarda la fecha/hora en que el conductor finalizó el servicio
-- (la pone el backend al recibir PATCH api/ServTurismo/{idservicio}/finalizar, no la app móvil).
ALTER TABLE servturismo
  ADD COLUMN horafinalizado DATETIME NULL AFTER finalizado;
