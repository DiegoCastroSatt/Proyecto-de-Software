-- Fixtures exclusivamente sintéticos para el Compose E2E efímero.
-- Este usuario administra horarios; no es una cuenta válida para iniciar sesión.
INSERT INTO trabajador_optica
    (nombre, nombre_usuario, correo, contrasena, estado, tipo_usuario)
VALUES
    ('Usuario Sintético E2E', 'e2e-fixture-admin', 'admin-e2e@optica.invalid', '', 'Activo', 'Administrador');

INSERT INTO clientes (rut, nombre, apellido, telefono, correo, estado)
VALUES ('E2E000001', 'Cliente', 'E2E', '900000000', 'cliente-e2e@optica.invalid', 'Activo');

SET @cliente_e2e_id = LAST_INSERT_ID();

INSERT INTO cuentas_cliente
    (id_cliente, correo, correo_normalizado, contrasena_hash, estado)
VALUES
    (@cliente_e2e_id, 'cliente-e2e@optica.invalid', 'CLIENTE-E2E@OPTICA.INVALID',
     'AQAAAAIAAYagAAAAEGKKaqoSvGW3pL5Bx4GBG1Rtvzs8fkipQepFOO9D7iE9NuKyjv9wBGzvuqXMkzTFRw==', 'Activa');

INSERT INTO horarios_atencion (id_administrador, fecha, hora_inicio, hora_fin, estado)
SELECT id_trabajador, DATE_ADD(CURDATE(), INTERVAL 14 DAY), '10:00:00', '10:20:00', 'Habilitada'
FROM trabajador_optica
WHERE nombre_usuario = 'e2e-fixture-admin'
  AND NOT EXISTS (
      SELECT 1
      FROM horarios_atencion
      WHERE fecha = DATE_ADD(CURDATE(), INTERVAL 14 DAY)
        AND hora_inicio = '10:00:00'
  );
