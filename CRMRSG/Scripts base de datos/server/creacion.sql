```sql id="r6xg5o"
-- =====================================================
-- BASE DE DATOS CRM-RSG
-- Compatible con:
-- SQL Server + SSMS + .NET Framework
-- =====================================================

-- =========================================
-- CREACIÓN DE BASE DE DATOS
-- =========================================

CREATE DATABASE CRM_RSG;
GO

USE CRM_RSG;
GO

-- =========================================
-- TABLA ROLES
-- =========================================

CREATE TABLE roles (
    id_rol INT PRIMARY KEY IDENTITY(1,1),

    nombre VARCHAR(50) NOT NULL UNIQUE,

    descripcion VARCHAR(255)
);
GO

-- =========================================
-- TABLA USUARIOS
-- =========================================

CREATE TABLE usuarios (
    id_usuario INT PRIMARY KEY IDENTITY(1,1),

    nombre VARCHAR(100) NOT NULL,

    apellido VARCHAR(100) NOT NULL,

    correo VARCHAR(150) NOT NULL UNIQUE,

    -- CONTRASEÑA HASHEADA CON BCRYPT
    password_hash VARCHAR(255) NOT NULL,

    telefono VARCHAR(20),

    estado BIT DEFAULT 1,

    correo_verificado BIT DEFAULT 0,

    -- TOKEN VERIFICACIÓN CORREO
    token_verificacion VARCHAR(255),

    fecha_expiracion_token DATETIME,

    -- TOKEN RECUPERACIÓN CONTRASEÑA
    token_recuperacion VARCHAR(255),

    fecha_expiracion_recuperacion DATETIME,

    fecha_creacion DATETIME DEFAULT GETDATE(),

    ultimo_login DATETIME,

    id_rol INT NOT NULL,

    CONSTRAINT FK_usuarios_roles
        FOREIGN KEY (id_rol)
        REFERENCES roles(id_rol)
);
GO

-- =========================================
-- TABLA CLIENTES
-- =========================================

CREATE TABLE clientes (
    id_cliente INT PRIMARY KEY IDENTITY(1,1),

    nombre VARCHAR(150) NOT NULL,

    empresa VARCHAR(150),

    telefono VARCHAR(20),

    correo VARCHAR(150),

    direccion VARCHAR(255),

    estado VARCHAR(50),

    fecha_registro DATETIME DEFAULT GETDATE(),

    id_usuario INT,

    CONSTRAINT FK_clientes_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =========================================
-- TABLA CITAS
-- =========================================

CREATE TABLE citas (
    id_cita INT PRIMARY KEY IDENTITY(1,1),

    fecha DATE NOT NULL,

    hora TIME NOT NULL,

    descripcion VARCHAR(255),

    lugar VARCHAR(150),

    estado VARCHAR(50),

    id_cliente INT,

    id_usuario INT,

    CONSTRAINT FK_citas_clientes
        FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente),

    CONSTRAINT FK_citas_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =========================================
-- TABLA TAREAS
-- =========================================

CREATE TABLE tareas (
    id_tarea INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150) NOT NULL,

    descripcion VARCHAR(255),

    prioridad VARCHAR(50),

    estado VARCHAR(50),

    fecha_limite DATE,

    id_cliente INT,

    id_usuario INT,

    CONSTRAINT FK_tareas_clientes
        FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente),

    CONSTRAINT FK_tareas_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =========================================
-- TABLA OPORTUNIDADES
-- =========================================

CREATE TABLE oportunidades (
    id_oportunidad INT PRIMARY KEY IDENTITY(1,1),

    nombre VARCHAR(150),

    descripcion VARCHAR(255),

    etapa VARCHAR(100),

    probabilidad DECIMAL(5,2),

    valor_estimado DECIMAL(18,2),

    fecha_creacion DATETIME DEFAULT GETDATE(),

    estado VARCHAR(50),

    id_cliente INT,

    id_usuario INT,

    CONSTRAINT FK_oportunidades_clientes
        FOREIGN KEY (id_cliente)
        REFERENCES clientes(id_cliente),

    CONSTRAINT FK_oportunidades_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =========================================
-- TABLA NOTIFICACIONES
-- =========================================

CREATE TABLE notificaciones (
    id_notificacion INT PRIMARY KEY IDENTITY(1,1),

    mensaje VARCHAR(255),

    fecha DATETIME DEFAULT GETDATE(),

    leida BIT DEFAULT 0,

    id_usuario INT,

    CONSTRAINT FK_notificaciones_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =========================================
-- TABLA REPORTES
-- =========================================

CREATE TABLE reportes (
    id_reporte INT PRIMARY KEY IDENTITY(1,1),

    tipo_reporte VARCHAR(100),

    fecha_generacion DATETIME DEFAULT GETDATE(),

    descripcion VARCHAR(255),

    id_usuario INT,

    CONSTRAINT FK_reportes_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =========================================
-- TABLA BITÁCORA / AUDITORÍA
-- =========================================

CREATE TABLE bitacora (
    id_registro INT PRIMARY KEY IDENTITY(1,1),

    accion VARCHAR(50),

    tabla_afectada VARCHAR(100),

    id_registro_afectado INT,

    valor_anterior TEXT,

    valor_nuevo TEXT,

    fecha_hora DATETIME DEFAULT GETDATE(),

    direccion_ip VARCHAR(100),

    id_usuario INT,

    CONSTRAINT FK_bitacora_usuarios
        FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario)
);
GO

-- =====================================================
-- FIN DEL SCRIPT
-- =====================================================
