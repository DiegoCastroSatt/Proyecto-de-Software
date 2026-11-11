SET NAMES utf8mb4;

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
    estado VARCHAR(24) NOT NULL DEFAULT 'Activa',
    fecha_creacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    token_confirmacion_hash CHAR(64) NULL,
    token_confirmacion_expira_utc DATETIME NULL,
    ultimo_correo_confirmacion_utc DATETIME NULL,
    correo_pendiente VARCHAR(100) NULL,
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

-- Tipos de marco administrables para recomendador y simulador.
CREATE TABLE tipos_marco (
    id_tipo_marco INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(80) NOT NULL UNIQUE,
    nombre VARCHAR(80) NOT NULL UNIQUE,
    categoria VARCHAR(50) NOT NULL,
    svg MEDIUMTEXT NOT NULL,
    rostros TEXT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE
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
ruta_imagen VARCHAR(255),
id_tipo_marco INT NULL,
CONSTRAINT fk_producto_tipo_marco FOREIGN KEY (id_tipo_marco) REFERENCES tipos_marco(id_tipo_marco) ON DELETE SET NULL
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
-- RESTRICT evita borrar pedidos al eliminar un cliente; CASCADE en UPDATE permite
-- anonimizar el RUT del cliente (ANON-{id}) conservando sus pedidos.
CONSTRAINT fk_pedidos_cliente
FOREIGN KEY (rut) REFERENCES clientes(rut)
ON DELETE RESTRICT
ON UPDATE CASCADE,
CONSTRAINT fk_pedidos_receta
FOREIGN KEY (id_receta) REFERENCES recetas(id_receta)
ON DELETE SET NULL
);

CREATE TABLE ventas (
id_venta INT AUTO_INCREMENT PRIMARY KEY,
id_cliente INT NULL,
CONSTRAINT fk_venta_cliente FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE SET NULL,
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
CREATE TABLE roles_trabajadores (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(60) NOT NULL UNIQUE,
    permisos_json LONGTEXT NOT NULL
);

CREATE TABLE trabajador_optica (
    id_trabajador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    nombre_usuario VARCHAR(60) NOT NULL UNIQUE,
    correo VARCHAR(100) NOT NULL,
    contrasena VARCHAR(255) NOT NULL,
    estado VARCHAR(8) NOT NULL DEFAULT 'Activo',
    tipo_usuario VARCHAR(20) NOT NULL DEFAULT 'Trabajador',
    id_rol INT NULL,
    id_administrador_creador INT NULL,
    CONSTRAINT fk_personal_rol FOREIGN KEY (id_rol) REFERENCES roles_trabajadores(id_rol),
    CONSTRAINT fk_personal_creador FOREIGN KEY (id_administrador_creador) REFERENCES trabajador_optica(id_trabajador),
    CONSTRAINT ck_personal_tipo CHECK (tipo_usuario IN ('Administrador', 'Trabajador')),
    CONSTRAINT ck_personal_rol CHECK (tipo_usuario = 'Administrador' OR id_rol IS NOT NULL)
);

CREATE TABLE historial_stock (
    id_cambio INT AUTO_INCREMENT PRIMARY KEY,
    id_producto INT NOT NULL,
    motivo_movimiento VARCHAR(255) NOT NULL,
    id_responsable INT NOT NULL,
    tipo_stock ENUM('Normal', 'Minimo') NOT NULL,
    valor_anterior INT NOT NULL,
    valor_actual INT NOT NULL,
    fecha_cambio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_historial_stock_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto) ON DELETE RESTRICT,
    CONSTRAINT fk_historial_stock_responsable
        FOREIGN KEY (id_responsable) REFERENCES trabajador_optica(id_trabajador) ON DELETE RESTRICT,
    INDEX idx_historial_stock_producto_fecha (id_producto, fecha_cambio)
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
        REFERENCES trabajador_optica(id_trabajador)
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

-- Encuestas de satisfacción por atención: control de envío y de respuesta única.
-- No guarda el contenido de la respuesta para mantenerla anónima.
CREATE TABLE IF NOT EXISTS encuestas_atencion (
    id_encuesta INT AUTO_INCREMENT PRIMARY KEY,
    id_reserva INT NOT NULL,
    token_hash CHAR(64) NULL,
    token_expira_utc DATETIME NULL,
    fecha_envio_utc DATETIME NOT NULL,
    respondida BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_encuesta_reserva UNIQUE (id_reserva),
    CONSTRAINT uq_encuesta_token UNIQUE (token_hash),
    CONSTRAINT fk_encuesta_reserva FOREIGN KEY (id_reserva)
        REFERENCES reservas(id_reserva) ON DELETE RESTRICT,
    INDEX idx_encuestas_fecha_envio (fecha_envio_utc)
);

-- Respuestas anónimas: sin referencia a la reserva, al cliente ni a la encuesta.
-- La fecha se guarda sin hora para dificultar el cruce con encuestas_atencion.
CREATE TABLE IF NOT EXISTS respuestas_encuesta_atencion (
    id_respuesta INT AUTO_INCREMENT PRIMARY KEY,
    calificacion_general TINYINT NOT NULL,
    calificacion_trato TINYINT NOT NULL,
    calificacion_puntualidad TINYINT NOT NULL,
    comentario VARCHAR(1000) NULL,
    -- El personal puede ocultar un comentario inapropiado sin borrar la respuesta.
    comentario_oculto BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_respuesta DATE NOT NULL,
    CONSTRAINT ck_respuesta_general CHECK (calificacion_general BETWEEN 1 AND 5),
    CONSTRAINT ck_respuesta_trato CHECK (calificacion_trato BETWEEN 1 AND 5),
    CONSTRAINT ck_respuesta_puntualidad CHECK (calificacion_puntualidad BETWEEN 1 AND 5),
    INDEX idx_respuestas_encuesta_fecha (fecha_respuesta)
);
