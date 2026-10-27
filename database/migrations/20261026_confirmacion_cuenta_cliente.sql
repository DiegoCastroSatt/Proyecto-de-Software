ALTER TABLE cuentas_cliente
    MODIFY COLUMN estado VARCHAR(24) NOT NULL DEFAULT 'Activa',
    ADD COLUMN token_confirmacion_hash CHAR(64) NULL,
    ADD COLUMN token_confirmacion_expira_utc DATETIME NULL,
    ADD COLUMN ultimo_correo_confirmacion_utc DATETIME NULL;
