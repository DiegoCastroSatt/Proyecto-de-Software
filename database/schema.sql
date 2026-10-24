
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

CREATE TABLE cuentas_cliente (
    id_cuenta INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL UNIQUE,
    correo VARCHAR(100) NOT NULL,
    correo_normalizado VARCHAR(100) NOT NULL UNIQUE,
    contrasena_hash VARCHAR(500) NOT NULL,
    estado VARCHAR(12) NOT NULL DEFAULT 'Activa',
    fecha_creacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_cuentas_cliente_cliente FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente) ON DELETE RESTRICT
);

CREATE TABLE recetas (
id_receta INT AUTO_INCREMENT PRIMARY KEY,
id_cliente INT NOT NULL,
fecha DATE NOT NULL,
observaciones TEXT,
imagen_path VARCHAR(255) NULL,
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
estado ENUM('Disponible', 'Agotado') NOT NULL DEFAULT 'Disponible',
ruta_imagen VARCHAR(255)
);

CREATE TABLE pedidos (
id_pedido INT AUTO_INCREMENT PRIMARY KEY,
rut VARCHAR(12) NOT NULL,
id_receta INT,
fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
estado ENUM('Pendiente', 'En proceso', 'Listo', 'Entregado', 'Cancelado')
NOT NULL DEFAULT 'Pendiente',
total DECIMAL(10,2) NOT NULL DEFAULT 0,
anotaciones TEXT,
CONSTRAINT fk_pedidos_cliente
FOREIGN KEY (rut) REFERENCES clientes(rut)
ON DELETE CASCADE,
CONSTRAINT fk_pedidos_receta
FOREIGN KEY (id_receta) REFERENCES recetas(id_receta)
ON DELETE SET NULL
);

CREATE TABLE ventas (
id_venta INT AUTO_INCREMENT PRIMARY KEY,
fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
total DECIMAL(10,2) NOT NULL DEFAULT 0
);

CREATE TABLE detalle_venta (
id_detalle INT AUTO_INCREMENT PRIMARY KEY,
id_venta INT NOT NULL,
id_producto INT,
nombre_producto VARCHAR(100) NOT NULL,
cantidad INT NOT NULL DEFAULT 1,
precio_unitario DECIMAL(10,2) NOT NULL,
subtotal DECIMAL(10,2) NOT NULL,
CONSTRAINT fk_detalle_venta
FOREIGN KEY (id_venta) REFERENCES ventas(id_venta)
ON DELETE CASCADE,
CONSTRAINT fk_detalle_producto
FOREIGN KEY (id_producto) REFERENCES productos(id_producto)
ON DELETE SET NULL
);
CREATE TABLE administradores (
    id_administrador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(60) NOT NULL,
    correo VARCHAR(100) NOT NULL UNIQUE,
    contrasena VARCHAR(255) NOT NULL,
    estado ENUM('Activo', 'Inactivo') NOT NULL DEFAULT 'Activo',
    CONSTRAINT uq_administrador_nombre UNIQUE (nombre)
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

    token_confirmacion_hash CHAR(64) NULL UNIQUE,
    token_cancelacion_hash CHAR(64) NULL UNIQUE,
    token_accion_expira_utc DATETIME NULL,
    recordatorio_enviado_utc DATETIME NULL,

    estado ENUM('Pendiente', 'Confirmada', 'Cancelada', 'Realizada')
        NOT NULL DEFAULT 'Pendiente',

    CONSTRAINT fk_reservas_cliente
        FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente)
        ON DELETE RESTRICT,

    CONSTRAINT fk_reservas_horario
        FOREIGN KEY (id_horario)
        REFERENCES horarios_atencion(id_horario)
        ON DELETE RESTRICT

    -- Se permiten varias filas históricas por horario cuando una reserva se cancela.
    -- La API controla que solo exista una reserva activa para cada horario.
);

CREATE INDEX idx_recetas_cliente ON recetas(id_cliente);
CREATE INDEX idx_pedidos_cliente ON pedidos(rut);
CREATE INDEX idx_pedidos_estado ON pedidos(estado);
CREATE INDEX idx_reservas_horario ON reservas(id_horario);
CREATE INDEX idx_reservas_recordatorio ON reservas(estado, recordatorio_enviado_utc);

CREATE TABLE correos_reservas (
    id_correo BIGINT AUTO_INCREMENT PRIMARY KEY,
    id_reserva INT NOT NULL,
    id_horario INT NOT NULL,
    tipo VARCHAR(30) NOT NULL,
    destinatario VARCHAR(254) NOT NULL,
    asunto VARCHAR(255) NOT NULL,
    cuerpo_html_protegido LONGTEXT NOT NULL,
    adjunto LONGBLOB NULL,
    nombre_adjunto VARCHAR(255) NULL,
    tipo_adjunto VARCHAR(100) NULL,
    enviado BOOLEAN NOT NULL DEFAULT FALSE,
    intentos INT NOT NULL DEFAULT 0,
    creado_utc DATETIME NOT NULL,
    proximo_intento_utc DATETIME NOT NULL,
    reclamado_hasta_utc DATETIME NULL,
    INDEX idx_correos_reservas_pendientes (enviado, proximo_intento_utc)
);

CREATE TABLE intentos_correo_reservas (
    id_intento BIGINT AUTO_INCREMENT PRIMARY KEY,
    id_correo BIGINT NOT NULL,
    fecha_utc DATETIME NOT NULL,
    exitoso BOOLEAN NOT NULL,
    error TEXT NULL,
    CONSTRAINT fk_intentos_correo_reservas FOREIGN KEY (id_correo)
        REFERENCES correos_reservas(id_correo) ON DELETE CASCADE,
    INDEX idx_intentos_correo_reservas_fecha (id_correo, fecha_utc)
);

CREATE TABLE correos_pedidos (
    id_correo BIGINT AUTO_INCREMENT PRIMARY KEY,
    id_pedido INT NOT NULL,
    destinatario VARCHAR(254) NOT NULL,
    asunto VARCHAR(255) NOT NULL,
    cuerpo_html LONGTEXT NOT NULL,
    importante BOOLEAN NOT NULL DEFAULT FALSE,
    enviado BOOLEAN NOT NULL DEFAULT FALSE,
    intentos INT NOT NULL DEFAULT 0,
    creado_utc DATETIME NOT NULL,
    proximo_intento_utc DATETIME NOT NULL,
    reclamado_hasta_utc DATETIME NULL,
    INDEX idx_correos_pedidos_pendientes (enviado, proximo_intento_utc)
);

CREATE TABLE intentos_correo_pedidos (
    id_intento BIGINT AUTO_INCREMENT PRIMARY KEY,
    id_correo BIGINT NOT NULL,
    fecha_utc DATETIME NOT NULL,
    exitoso BOOLEAN NOT NULL,
    error TEXT NULL,
    CONSTRAINT fk_intentos_correo_pedidos FOREIGN KEY (id_correo)
        REFERENCES correos_pedidos(id_correo) ON DELETE CASCADE,
    INDEX idx_intentos_correo_pedidos_fecha (id_correo, fecha_utc)
);
CREATE INDEX idx_productos_categoria ON productos(categoria);
CREATE INDEX idx_productos_stock ON productos(stock);

-- Migración aditiva para bases existentes. Ejecutar una vez antes de iniciar la API actualizada.
USE optica_db;
CREATE TABLE IF NOT EXISTS roles_trabajadores (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(60) NOT NULL UNIQUE,
    permisos_json LONGTEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS trabajadores (
    id_trabajador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(100) NOT NULL UNIQUE,
    contrasena VARCHAR(255) NOT NULL,
    estado VARCHAR(8) NOT NULL DEFAULT 'Activo',
    id_rol INT NOT NULL,
    id_administrador_creador INT NOT NULL,
    CONSTRAINT fk_trabajador_rol FOREIGN KEY (id_rol) REFERENCES roles_trabajadores(id_rol),
    CONSTRAINT fk_trabajador_administrador FOREIGN KEY (id_administrador_creador) REFERENCES administradores(id_administrador)
);

-- Tipos de marco administrables para recomendador y simulador.
CREATE TABLE IF NOT EXISTS tipos_marco (
    id_tipo_marco INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(80) NOT NULL UNIQUE,
    nombre VARCHAR(80) NOT NULL UNIQUE,
    categoria VARCHAR(50) NOT NULL,
    svg MEDIUMTEXT NOT NULL,
    rostros VARCHAR(100) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);
-- Rostros administrables del recomendador.
CREATE TABLE IF NOT EXISTS tipos_rostro (
    id_tipo_rostro INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(80) NOT NULL UNIQUE,
    nombre VARCHAR(80) NOT NULL UNIQUE,
    rasgos VARCHAR(500) NOT NULL,
    consejo VARCHAR(500) NOT NULL,
    ruta_imagen VARCHAR(255) NULL,
    imagen MEDIUMBLOB NULL,
    tipo_imagen VARCHAR(30) NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

-- Un armazón del inventario utiliza el diseño de un tipo de marco.
ALTER TABLE productos ADD COLUMN id_tipo_marco INT NULL;
ALTER TABLE productos ADD CONSTRAINT fk_producto_tipo_marco FOREIGN KEY (id_tipo_marco) REFERENCES tipos_marco(id_tipo_marco) ON DELETE SET NULL;

-- Contornos personalizados reutilizables en el editor de marcos.
CREATE TABLE IF NOT EXISTS formas_lente (
    id_forma_lente INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    nombre VARCHAR(80) NOT NULL,
    nombre_normalizado VARCHAR(80) NOT NULL UNIQUE,
    geometria VARCHAR(20) NOT NULL DEFAULT 'contorno',
    mantener_proporcion BOOLEAN NOT NULL DEFAULT FALSE,
    contorno TEXT NOT NULL
);
