-- Migración incremental: ejecutar una vez en la base optica_db existente.
-- No elimina ni modifica registros de otras tablas.
USE optica_db;

CREATE TABLE IF NOT EXISTS resenas_productos (
    id_resena INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    id_producto INT NOT NULL,
    calificacion TINYINT NOT NULL,
    comentario VARCHAR(1000) NOT NULL,
    fecha_creacion_utc DATETIME NOT NULL,
    fecha_actualizacion_utc DATETIME NULL,
    CONSTRAINT uq_resena_cliente_producto UNIQUE (id_cliente, id_producto),
    CONSTRAINT ck_resena_calificacion CHECK (calificacion BETWEEN 1 AND 5),
    CONSTRAINT fk_resena_cliente FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE RESTRICT,
    CONSTRAINT fk_resena_producto FOREIGN KEY (id_producto) REFERENCES productos(id_producto) ON DELETE RESTRICT
);
