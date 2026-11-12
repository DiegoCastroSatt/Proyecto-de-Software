-- Migración para bases existentes: encuestas de satisfacción por atención.
-- Las bases nuevas ya obtienen estas tablas desde database/schema.sql.

USE optica_db;

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
