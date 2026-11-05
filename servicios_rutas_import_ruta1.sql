-- Importación de la ruta "Ruta 1" (Ruta REP San Isidro) desde el KML de Google My Maps.
-- Requiere haber ejecutado antes: ALTER TABLE ruta ADD COLUMN trazado LONGTEXT NULL (servicios_rutas_trazado.sql).
-- ANTES de ejecutar: confirma que @cuenta es el accountID de tu usuario (el mismo del token JWT).
--
-- Datos que NO vienen en el KML y se estimaron (corrígelos luego en "Administrar rutas"):
--   * radio de cada geocerca: 100 m
--   * minutos desde el paradero anterior: estimados a 20 km/h sobre la línea del trazado
--   * distancia_km: largo de la línea (14.41 km)

SET @cuenta := 'movilbus';

START TRANSACTION;

INSERT INTO ruta (account_id, codigo, nombre, grupo, descripcion, distancia_km, trazado)
VALUES (@cuenta, 'RT-001', 'Ruta 1', 'Ruta REP San Isidro', NULL, 14.41,
        '[[-12.1779406,-76.9960183],[-12.1729486,-76.9918984],[-12.1697813,-76.9921559],[-12.1666979,-76.9903964],[-12.16309,-76.9907612],[-12.159608,-76.9887227],[-12.1584963,-76.9886583],[-12.1576572,-76.9891089],[-12.1557483,-76.9904608],[-12.1550981,-76.9906753],[-12.1543639,-76.990611],[-12.1529165,-76.9896454],[-12.1505251,-76.9878858],[-12.1495812,-76.9869202],[-12.1477142,-76.9858688],[-12.1470849,-76.9870275],[-12.1428684,-76.9904822],[-12.1408545,-76.9922203],[-12.1391344,-76.9936365],[-12.1422531,-76.9958105],[-12.1397266,-76.9995329],[-12.1395391,-76.9994101],[-12.1394761,-76.9995039],[-12.1397541,-76.9996836],[-12.1410566,-77.0013022],[-12.1416964,-77.0017528],[-12.1419796,-77.0021497],[-12.1418642,-77.0026969],[-12.1405741,-77.0045852],[-12.1401441,-77.0050787],[-12.1390637,-77.0039629],[-12.1374904,-77.0030295],[-12.1361373,-77.0057332],[-12.135424,-77.0070314],[-12.1351093,-77.0075893],[-12.1345774,-77.0097525],[-12.1326774,-77.0092824],[-12.1276095,-77.0095419],[-12.1244558,-77.0089516],[-12.1239942,-77.0090374],[-12.1241935,-77.0130071],[-12.1203543,-77.0130071],[-12.1204762,-77.0182384],[-12.1185018,-77.0182471],[-12.1056557,-77.0186879],[-12.1084903,-77.0264718],[-12.1091406,-77.0284459],[-12.1091616,-77.0295188],[-12.1033081,-77.0305058],[-12.1037831,-77.032066]]');

SET @ruta_id := LAST_INSERT_ID();

INSERT INTO ruta_paradero
    (ruta_id, orden, tipo, nombre, descripcion, latitud, longitud, radio_metros, minutos_desde_anterior, activo)
VALUES
    (@ruta_id, 1, 'ORIGEN', 'Avenida El Sol & Avenida Guardia Civil Sur', NULL, -12.1781451, -76.9963693, 100, 0, 1),
    (@ruta_id, 2, 'INTERMEDIO', 'Tottus', NULL, -12.1589379, -76.9890423, 100, 7, 1),
    (@ruta_id, 3, 'INTERMEDIO', 'Av Tomás Marsan & Av. Andrés Tinoco (Wong)', NULL, -12.142702, -76.990616, 100, 7, 1),
    (@ruta_id, 4, 'INTERMEDIO', 'Avenida Ayacucho & Doña Rosa', NULL, -12.1397266, -76.9995329, 100, 4, 1),
    (@ruta_id, 5, 'INTERMEDIO', 'Avenida Ayacucho & Doña Ana', NULL, -12.1409214, -77.0012115, 100, 1, 1),
    (@ruta_id, 6, 'INTERMEDIO', 'Avenida La Merced & Avenida Mariscal Ramón Castilla', NULL, -12.1345774, -77.0097525, 100, 5, 1),
    (@ruta_id, 7, 'INTERMEDIO', 'Avenida Roca y Boloña & Avenida República de Panamá', NULL, -12.1204762, -77.0182384, 100, 8, 1),
    (@ruta_id, 8, 'INTERMEDIO', 'Av. República de Panamá & Tienda Mass', NULL, -12.1185018, -77.0182471, 100, 1, 1),
    (@ruta_id, 9, 'INTERMEDIO', 'Avenida Domingo Orué & Avenida República de Panamá', NULL, -12.1056557, -77.0186879, 100, 4, 1),
    (@ruta_id, 10, 'INTERMEDIO', 'Av. Domingo Orué & C. Los Halcones (caseta de serenazgo)', NULL, -12.1071284, -77.0226962, 100, 1, 1),
    (@ruta_id, 11, 'DESTINO', 'Av. Sta. Cruz 120', NULL, -12.1037831, -77.032066, 100, 5, 1);

COMMIT;

-- Verificación
SELECT id, codigo, nombre, grupo, distancia_km, CHAR_LENGTH(trazado) AS largo_trazado
FROM ruta WHERE account_id = 'movilbus' AND codigo = 'RT-001';

SELECT orden, tipo, nombre, latitud, longitud, radio_metros, minutos_desde_anterior
FROM ruta_paradero WHERE ruta_id = @ruta_id ORDER BY orden;
