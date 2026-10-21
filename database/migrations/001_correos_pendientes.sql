-- Ejecutar una sola vez en bases existentes; el schema.sql contiene estas mismas tablas para instalaciones nuevas.
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
