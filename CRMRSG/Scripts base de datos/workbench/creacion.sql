-- =====================================================
-- BASE DE DATOS CRM-RSG
-- Compatible con:
-- MySQL + MySQL Workbench + .NET Framework
-- =====================================================
CREATE DATABASE IF NOT EXISTS crm_rsg;
USE crm_rsg;

-- =========================================
-- TABLA ROLES
-- =========================================

CREATE TABLE roles (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL UNIQUE,
    descripcion VARCHAR(255)
);

-- =========================================
-- TABLA USUARIOS
-- =========================================

CREATE TABLE usuarios (
    id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    apellido VARCHAR(100) NOT NULL,
    correo VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    telefono VARCHAR(20),
    estado BOOLEAN DEFAULT TRUE,
    correo_verificado BOOLEAN DEFAULT FALSE,
    token_verificacion VARCHAR(255),
    fecha_expiracion_token DATETIME,
    token_recuperacion VARCHAR(255),
    fecha_expiracion_recuperacion DATETIME,
    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    ultimo_login DATETIME,
    id_rol INT NOT NULL,
    FOREIGN KEY (id_rol) REFERENCES roles(id_rol)
);

-- =========================================
-- TABLA CLIENTES
-- =========================================

CREATE TABLE clientes (
    id_cliente INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL,
    empresa VARCHAR(150),
    telefono VARCHAR(20),
    correo VARCHAR(150),
    direccion VARCHAR(255),
    estado VARCHAR(50),
    fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP,
    id_usuario INT,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario)
);

-- =========================================
-- TABLA CONTACTO CLIENTE
-- =========================================

CREATE TABLE contacto_cliente (
    id_contacto INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    apellido VARCHAR(150),
    puesto VARCHAR(150),
    telefono VARCHAR(20),
    correo VARCHAR(150),
    FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE CASCADE
);

-- =========================================
-- TABLA NOTA CLIENTE
-- =========================================

CREATE TABLE nota_cliente (
    id_nota INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    comentario TEXT NOT NULL,
    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    id_usuario INT NOT NULL,
    FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario)
);

-- =========================================
-- TABLA CITAS
-- =========================================

CREATE TABLE citas (
    id_cita INT AUTO_INCREMENT PRIMARY KEY,
    fecha DATE NOT NULL,
    hora TIME NOT NULL,
    descripcion VARCHAR(255),
    lugar VARCHAR(150),
    estado VARCHAR(50),
    id_cliente INT,
    id_usuario INT,
    id_contacto INT,
    FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario),
    FOREIGN KEY (id_contacto) REFERENCES contacto_cliente(id_contacto) ON DELETE SET NULL
);

-- =========================================
-- TABLA TAREAS
-- =========================================

CREATE TABLE tareas (
    id_tarea INT AUTO_INCREMENT PRIMARY KEY,
    titulo VARCHAR(150) NOT NULL,
    descripcion VARCHAR(255),
    prioridad VARCHAR(50),
    estado VARCHAR(50),
    fecha_limite DATE,
    id_cliente INT,
    id_usuario INT,
    alerta_disparada BOOLEAN DEFAULT FALSE,
    id_contacto INT,
    FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario),
    FOREIGN KEY (id_contacto) REFERENCES contacto_cliente(id_contacto) ON DELETE SET NULL
);

-- =========================================
-- TABLA OPORTUNIDADES
-- =========================================

CREATE TABLE oportunidades (
    id_oportunidad INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(150),
    descripcion VARCHAR(255),
    etapa VARCHAR(100),
    probabilidad DECIMAL(5,2),
    valor_estimado DECIMAL(18,2),
    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    estado VARCHAR(50),
    id_cliente INT,
    id_usuario INT,
    FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario)
);

-- =========================================
-- TABLA NOTIFICACIONES
-- =========================================

CREATE TABLE notificaciones (
    id_notificacion INT AUTO_INCREMENT PRIMARY KEY,
    mensaje VARCHAR(255),
    fecha DATETIME DEFAULT CURRENT_TIMESTAMP,
    leida BOOLEAN DEFAULT FALSE,
    id_usuario INT,
    tipo VARCHAR(50),
    id_referencia INT,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario) ON DELETE CASCADE
);

-- =========================================
-- TABLA REPORTES
-- =========================================

CREATE TABLE reportes (
    id_reporte INT AUTO_INCREMENT PRIMARY KEY,
    tipo_reporte VARCHAR(100),
    fecha_generacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    descripcion VARCHAR(255),
    id_usuario INT,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario)
);

-- =========================================
-- TABLA BITÁCORA / AUDITORÍA
-- =========================================

CREATE TABLE bitacora (
    id_registro INT AUTO_INCREMENT PRIMARY KEY,
    accion VARCHAR(50),
    tabla_afectada VARCHAR(100),
    id_registro_afectado INT,
    valor_anterior TEXT,
    valor_nuevo TEXT,
    fecha_hora DATETIME DEFAULT CURRENT_TIMESTAMP,
    direccion_ip VARCHAR(100),
    id_usuario INT,
    FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario) ON DELETE SET NULL
);

-- =========================================
-- TABLA CORREOS PROGRAMADOS
-- =========================================

CREATE TABLE correos_programados (
    id_correo INT AUTO_INCREMENT PRIMARY KEY,
    destinatario VARCHAR(150) NOT NULL,
    asunto VARCHAR(150) NOT NULL,
    cuerpo TEXT NOT NULL,
    fecha_envio DATETIME NOT NULL,
    enviado BOOLEAN DEFAULT FALSE
);

-- =====================================================
-- INSERCIÓN DE DATOS SEMILLA (ROLES Y ADMINISTRADOR)
-- =====================================================

-- Roles del sistema
INSERT INTO roles (id_rol, nombre, descripcion) VALUES 
(1, 'Administrador', 'Control total del sistema, gestión de usuarios, roles e historial de acciones.'),
(2, 'Vendedor', 'Gestión de clientes, oportunidades, tareas, interacciones y eventos comerciales.'),
(3, 'Gerente', 'Visualización de indicadores generales y rendimiento de vendedores.'),
(4, 'Supervisor', 'Visualización de estadísticas de eventos, tareas y clasificación de clientes.')
ON DUPLICATE KEY UPDATE nombre=VALUES(nombre), descripcion=VALUES(descripcion);

-- Usuario Administrador por defecto (Contraseña: Password123!)
-- El hash corresponde a la encriptación SHA-256 utilizada por el sistema
INSERT INTO usuarios (id_usuario, nombre, apellido, correo, password_hash, telefono, estado, correo_verificado, id_rol) VALUES
(1, 'Administrador', 'CRM', 'admin.crm@gmail.com', 'a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea', '88888888', 1, 1, 1)
ON DUPLICATE KEY UPDATE correo=VALUES(correo);

-- =====================================================
-- FIN DEL SCRIPT
-- =====================================================
