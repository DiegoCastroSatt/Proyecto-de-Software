
-- Script autocontenido: puede ejecutarse directamente en MySQL o mediante Docker.
CREATE DATABASE IF NOT EXISTS optica_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE optica_db;

CREATE TABLE clientes (
id_cliente INT AUTO_INCREMENT PRIMARY KEY,
rut VARCHAR(12) NOT NULL UNIQUE,
nombre VARCHAR(60) NOT NULL,
apellido VARCHAR(60) NOT NULL,
telefono VARCHAR(20),
correo VARCHAR(100) UNIQUE,
estado ENUM('Activo', 'Inactivo') NOT NULL DEFAULT 'Activo',
fecha_registro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE recetas (
id_receta INT AUTO_INCREMENT PRIMARY KEY,
id_cliente INT NOT NULL,
fecha DATE NOT NULL,
observaciones TEXT,
CONSTRAINT fk_recetas_cliente
FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
ON DELETE CASCADE
);

CREATE TABLE graduaciones (
id_graduacion INT AUTO_INCREMENT PRIMARY KEY,
id_receta INT NOT NULL,
ojo ENUM('OD', 'OI') NOT NULL,
esfera DECIMAL(4,2),
cilindro DECIMAL(4,2),
eje SMALLINT,
adicion DECIMAL(4,2),
CONSTRAINT fk_graduaciones_receta
FOREIGN KEY (id_receta) REFERENCES recetas(id_receta)
ON DELETE CASCADE,
CONSTRAINT uq_receta_ojo UNIQUE (id_receta, ojo)
);

CREATE TABLE catalogos (
id_catalogo INT AUTO_INCREMENT PRIMARY KEY,
tipo VARCHAR(20) NOT NULL,
nombre VARCHAR(60) NOT NULL,
CONSTRAINT uq_catalogo_tipo_nombre UNIQUE (tipo, nombre)
);

INSERT IGNORE INTO catalogos (tipo, nombre) VALUES
('Marca', 'Ray-Ban'),
('Marca', 'Oakley'),
('Marca', 'Vogue'),
('Marca', 'Polaroid'),
('Color', 'Negro'),
('Color', 'Café'),
('Color', 'Dorado'),
('Color', 'Plateado'),
('Color', 'Transparente'),
('Categoria', 'Lentes ópticos'),
('Categoria', 'Lentes de sol'),
('Categoria', 'Armazones'),
('Categoria', 'Lentes de contacto'),
('Categoria', 'Accesorios');

CREATE TABLE productos (
id_producto INT AUTO_INCREMENT PRIMARY KEY,
codigo VARCHAR(30) NOT NULL UNIQUE,
nombre VARCHAR(100) NOT NULL,
marca VARCHAR(60),
modelo VARCHAR(60),
color VARCHAR(40),
categoria VARCHAR(50) NOT NULL,
precio DECIMAL(10,2) NOT NULL DEFAULT 0,
stock INT NOT NULL DEFAULT 0,
stock_minimo INT NOT NULL DEFAULT 0,
estado ENUM('Disponible', 'Agotado') NOT NULL DEFAULT 'Disponible'
);

CREATE TABLE pedidos (
id_pedido INT AUTO_INCREMENT PRIMARY KEY,
id_cliente INT NOT NULL,
id_receta INT,
fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
estado ENUM('Pendiente', 'En proceso', 'Listo', 'Entregado', 'Cancelado')
NOT NULL DEFAULT 'Pendiente',
total DECIMAL(10,2) NOT NULL DEFAULT 0,
CONSTRAINT fk_pedidos_cliente
FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
ON DELETE CASCADE,
CONSTRAINT fk_pedidos_receta
FOREIGN KEY (id_receta) REFERENCES recetas(id_receta)
ON DELETE SET NULL
);

CREATE TABLE ventas (
id_venta INT AUTO_INCREMENT PRIMARY KEY,
id_cliente INT NOT NULL,
fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
total DECIMAL(10,2) NOT NULL DEFAULT 0,
CONSTRAINT fk_ventas_cliente
FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
ON DELETE CASCADE
);

CREATE TABLE detalle_venta (
id_detalle INT AUTO_INCREMENT PRIMARY KEY,
id_venta INT NOT NULL,
id_producto INT NOT NULL,
cantidad INT NOT NULL DEFAULT 1,
precio_unitario DECIMAL(10,2) NOT NULL,
subtotal DECIMAL(10,2) NOT NULL,
CONSTRAINT fk_detalle_venta
FOREIGN KEY (id_venta) REFERENCES ventas(id_venta)
ON DELETE CASCADE,
CONSTRAINT fk_detalle_producto
FOREIGN KEY (id_producto) REFERENCES productos(id_producto)
ON DELETE RESTRICT
);
CREATE TABLE administradores (
    id_administrador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(60) NOT NULL,
    apellido VARCHAR(60) NOT NULL,
    correo VARCHAR(100) NOT NULL UNIQUE,
    contrasena VARCHAR(255) NOT NULL,
    estado ENUM('Activo', 'Inactivo') NOT NULL DEFAULT 'Activo'
);
CREATE TABLE horarios_atencion (
    id_horario INT AUTO_INCREMENT PRIMARY KEY,

    id_administrador INT NOT NULL,

    fecha DATE NOT NULL,
    hora_inicio TIME NOT NULL,
    hora_fin TIME NOT NULL,

    estado ENUM('Habilitada', 'Inhabilitada') NOT NULL DEFAULT 'Habilitada',

    CONSTRAINT fk_horarios_administrador
        FOREIGN KEY (id_administrador)
        REFERENCES administradores(id_administrador)
        ON DELETE RESTRICT,

    CONSTRAINT uq_horario_fecha_hora
        UNIQUE (fecha, hora_inicio)
);

CREATE TABLE reservas (
    id_reserva INT AUTO_INCREMENT PRIMARY KEY,

    id_cliente INT NOT NULL,
    id_horario INT NOT NULL,

    motivo VARCHAR(150),

    estado ENUM('Pendiente', 'Confirmada', 'Cancelada', 'Realizada')
        NOT NULL DEFAULT 'Pendiente',

    CONSTRAINT fk_reservas_cliente
        FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente)
        ON DELETE RESTRICT,

    CONSTRAINT fk_reservas_horario
        FOREIGN KEY (id_horario)
        REFERENCES horarios_atencion(id_horario)
        ON DELETE RESTRICT,

    CONSTRAINT uq_reserva_horario
        UNIQUE (id_horario)
);

CREATE INDEX idx_recetas_cliente ON recetas(id_cliente);
CREATE INDEX idx_pedidos_cliente ON pedidos(id_cliente);
CREATE INDEX idx_pedidos_estado ON pedidos(estado);
CREATE INDEX idx_reservas_horario ON reservas(id_horario);
CREATE INDEX idx_ventas_cliente ON ventas(id_cliente);
CREATE INDEX idx_productos_categoria ON productos(categoria);
CREATE INDEX idx_productos_stock ON productos(stock);
