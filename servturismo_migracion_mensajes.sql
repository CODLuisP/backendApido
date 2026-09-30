-- Mensajes/solicitudes que el conductor manda desde la app (botón "Mensaje / Solicitud" en el
-- detalle del servicio). "atendido" queda en 0 hasta que el operador cierra la alerta a mano en
-- el front (PATCH /ServTurismo/mensajes/{idmensaje}/atendido); así el front puede recuperar los
-- mensajes que llegaron mientras estaba desconectado (GET /ServTurismo/mensajes/pendientes).
CREATE TABLE servturismo_mensaje (
  idmensaje INT AUTO_INCREMENT PRIMARY KEY,
  idservicio INT NOT NULL,
  tipo VARCHAR(20) NOT NULL,        -- 'observacion' | 'ampliacion'
  texto VARCHAR(500) NULL,          -- observación libre, o detalle adicional de la ampliación
  dias INT NULL,                    -- solo 'ampliacion': cantidad de días elegida en el select
  brevete VARCHAR(20) NULL,         -- conductor que lo envió (resuelto en servidor, no lo manda la app)
  fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  atendido TINYINT NOT NULL DEFAULT 0,
  fecha_atendido DATETIME NULL,
  CONSTRAINT fk_servturismo_mensaje_servicio FOREIGN KEY (idservicio)
    REFERENCES servturismo (idservicio)
);

CREATE INDEX idx_servturismo_mensaje_atendido ON servturismo_mensaje (atendido);
