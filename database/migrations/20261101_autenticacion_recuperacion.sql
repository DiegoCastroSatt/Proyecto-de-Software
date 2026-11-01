-- Migración aditiva para la persistencia de códigos de recuperación.
-- Aplicar una vez sobre una base creada con la estructura vigente de trabajador_optica.
-- No modifica cuentas, contraseñas, roles ni relaciones existentes.
USE optica_db;

CREATE TABLE desafios_recuperacion (
    id_desafio BIGINT AUTO_INCREMENT PRIMARY KEY,
    tipo_cuenta VARCHAR(20) NOT NULL,
    id_cuenta_cliente INT NULL,
    id_personal INT NULL,
    hash_codigo CHAR(64) NOT NULL,
    creado_utc DATETIME NOT NULL,
    expira_utc DATETIME NOT NULL,
    intentos TINYINT UNSIGNED NOT NULL DEFAULT 0,
    estado VARCHAR(16) NOT NULL DEFAULT 'Pendiente',
    huella_identificador CHAR(64) NOT NULL,
    huella_ip CHAR(64) NOT NULL,
    ultima_solicitud_utc DATETIME NULL,
    id_cuenta_cliente_pendiente INT GENERATED ALWAYS AS
        (CASE WHEN estado = 'Pendiente' THEN id_cuenta_cliente ELSE NULL END) STORED,
    id_personal_pendiente INT GENERATED ALWAYS AS
        (CASE WHEN estado = 'Pendiente' THEN id_personal ELSE NULL END) STORED,
    CONSTRAINT ck_desafio_tipo_cuenta CHECK (
        (tipo_cuenta = 'Cliente' AND id_cuenta_cliente IS NOT NULL AND id_personal IS NULL)
        OR (tipo_cuenta IN ('Administrador', 'Trabajador') AND id_personal IS NOT NULL AND id_cuenta_cliente IS NULL)
    ),
    CONSTRAINT ck_desafio_intentos CHECK (intentos BETWEEN 0 AND 5),
    CONSTRAINT ck_desafio_estado CHECK (estado IN ('Pendiente', 'Usado', 'Invalidado', 'Agotado', 'Expirado')),
    CONSTRAINT fk_desafio_cuenta_cliente FOREIGN KEY (id_cuenta_cliente)
        REFERENCES cuentas_cliente(id_cuenta) ON DELETE RESTRICT,
    CONSTRAINT fk_desafio_personal FOREIGN KEY (id_personal)
        REFERENCES trabajador_optica(id_trabajador) ON DELETE RESTRICT,
    UNIQUE KEY uq_desafio_cliente_pendiente (id_cuenta_cliente_pendiente),
    UNIQUE KEY uq_desafio_personal_pendiente (id_personal_pendiente),
    INDEX idx_desafio_identificador_fecha (huella_identificador, creado_utc),
    INDEX idx_desafio_ip_fecha (huella_ip, creado_utc)
);

CREATE TABLE limites_recuperacion (
    huella CHAR(64) PRIMARY KEY,
    inicio_ventana_utc DATETIME NOT NULL,
    solicitudes INT UNSIGNED NOT NULL,
    ultima_solicitud_utc DATETIME NOT NULL,
    INDEX idx_limite_ventana (inicio_ventana_utc)
);
