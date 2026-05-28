-- =====================================================
-- BASE DE DATOS CRM-RSG
-- Compatible con:
-- MySQL + MySQL Workbench + .NET Framework
-- =====================================================

-- =========================================
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

    -- CONTRASEÑA HASHEADA CON BCRYPT
    password_hash VARCHAR(255) NOT NULL,

    telefono VARCHAR(20),

    estado BOOLEAN DEFAULT TRUE,

    correo_verificado BOOLEAN DEFAULT FALSE,

    -- TOKEN PARA VERIFICACIÓN DE CORREO
    token_verificacion VARCHAR(255),

    fecha_expiracion_token DATETIME,

    -- TOKEN PARA RECUPERACIÓN DE CONTRASEÑA
    token_recuperacion VARCHAR(255),

    fecha_expiracion_recuperacion DATETIME,

    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,

    ultimo_login DATETIME,

    id_rol INT NOT NULL,

    FOREIGN KEY (id_rol)
        REFERENCES roles(id_rol)
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

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
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

    FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente),

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
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

    FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente),

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
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

    FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente),

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
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

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
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

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
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

    FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- =====================================================
-- FIN DEL SCRIPT
-- =====================================================

