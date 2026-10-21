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
