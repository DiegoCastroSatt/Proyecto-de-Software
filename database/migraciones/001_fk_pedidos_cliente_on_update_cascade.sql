-- Migración para bases existentes: eliminación de cuenta de cliente.
-- Redefine la FK pedidos.rut -> clientes.rut para:
--   * impedir que borrar un cliente borre sus pedidos (ON DELETE RESTRICT);
--   * permitir anonimizar el RUT del cliente propagándolo a pedidos (ON UPDATE CASCADE).
-- Las bases nuevas ya obtienen esta definición desde database/schema.sql.

USE optica_db;

ALTER TABLE pedidos DROP FOREIGN KEY fk_pedidos_cliente;

ALTER TABLE pedidos ADD CONSTRAINT fk_pedidos_cliente
    FOREIGN KEY (rut) REFERENCES clientes(rut)
    ON DELETE RESTRICT
    ON UPDATE CASCADE;
