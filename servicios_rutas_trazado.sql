-- Trazado (línea del recorrido) de cada ruta, para dibujarla en el mapa.
-- Se guarda como JSON en texto: [[lat,lng],[lat,lng],...]
-- LONGTEXT porque una ruta larga puede tener miles de puntos.
ALTER TABLE ruta
    ADD COLUMN trazado LONGTEXT NULL AFTER distancia_km;
