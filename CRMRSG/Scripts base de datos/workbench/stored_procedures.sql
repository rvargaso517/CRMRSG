-- =====================================================
-- PROCEDIMIENTOS ALMACENADOS PARA CRM-RSG
-- =====================================================

DROP PROCEDURE IF EXISTS sp_roles_listar;
DROP PROCEDURE IF EXISTS sp_roles_obtener_por_id;
DROP PROCEDURE IF EXISTS sp_roles_insertar;
DROP PROCEDURE IF EXISTS sp_roles_actualizar;
DROP PROCEDURE IF EXISTS sp_roles_eliminar;

DROP PROCEDURE IF EXISTS sp_usuarios_listar;
DROP PROCEDURE IF EXISTS sp_usuarios_obtener_por_correo;
DROP PROCEDURE IF EXISTS sp_usuarios_obtener_por_id;
DROP PROCEDURE IF EXISTS sp_usuarios_insertar;
DROP PROCEDURE IF EXISTS sp_usuarios_actualizar;
DROP PROCEDURE IF EXISTS sp_usuarios_actualizar_ultimo_login;
DROP PROCEDURE IF EXISTS sp_usuarios_actualizar_token_recuperacion;
DROP PROCEDURE IF EXISTS sp_usuarios_actualizar_token_verificacion;
DROP PROCEDURE IF EXISTS sp_usuarios_actualizar_contrasena;
DROP PROCEDURE IF EXISTS sp_usuarios_verificar_correo;
DROP PROCEDURE IF EXISTS sp_usuarios_obtener_por_token_recuperacion;

DROP PROCEDURE IF EXISTS sp_clientes_listar;
DROP PROCEDURE IF EXISTS sp_clientes_listar_por_usuario;
DROP PROCEDURE IF EXISTS sp_clientes_obtener_por_id;
DROP PROCEDURE IF EXISTS sp_clientes_insertar;
DROP PROCEDURE IF EXISTS sp_clientes_actualizar;
DROP PROCEDURE IF EXISTS sp_clientes_eliminar;

DROP PROCEDURE IF EXISTS sp_citas_listar;
DROP PROCEDURE IF EXISTS sp_citas_listar_con_cliente;
DROP PROCEDURE IF EXISTS sp_citas_listar_con_relaciones;
DROP PROCEDURE IF EXISTS sp_citas_listar_proximas_alertas;
DROP PROCEDURE IF EXISTS sp_citas_obtener_por_id;
DROP PROCEDURE IF EXISTS sp_citas_insertar;
DROP PROCEDURE IF EXISTS sp_citas_actualizar;
DROP PROCEDURE IF EXISTS sp_citas_eliminar;

DROP PROCEDURE IF EXISTS sp_tareas_listar;
DROP PROCEDURE IF EXISTS sp_tareas_listar_con_contacto;
DROP PROCEDURE IF EXISTS sp_tareas_obtener_con_contacto;
DROP PROCEDURE IF EXISTS sp_tareas_obtener_por_id;
DROP PROCEDURE IF EXISTS sp_tareas_insertar;
DROP PROCEDURE IF EXISTS sp_tareas_actualizar;
DROP PROCEDURE IF EXISTS sp_tareas_eliminar;
DROP PROCEDURE IF EXISTS sp_tareas_actualizar_alerta;

DROP PROCEDURE IF EXISTS sp_oportunidades_listar;
DROP PROCEDURE IF EXISTS sp_oportunidades_listar_con_relaciones;
DROP PROCEDURE IF EXISTS sp_oportunidades_obtener_con_relaciones;
DROP PROCEDURE IF EXISTS sp_oportunidades_obtener_por_id;
DROP PROCEDURE IF EXISTS sp_oportunidades_insertar;
DROP PROCEDURE IF EXISTS sp_oportunidades_actualizar;
DROP PROCEDURE IF EXISTS sp_oportunidades_eliminar;

DROP PROCEDURE IF EXISTS sp_contactos_listar_por_cliente;
DROP PROCEDURE IF EXISTS sp_contactos_listar_con_cliente;
DROP PROCEDURE IF EXISTS sp_contactos_insertar;
DROP PROCEDURE IF EXISTS sp_contactos_eliminar;

DROP PROCEDURE IF EXISTS sp_notas_listar_por_cliente;
DROP PROCEDURE IF EXISTS sp_notas_insertar;
DROP PROCEDURE IF EXISTS sp_notas_eliminar;

DROP PROCEDURE IF EXISTS sp_notificaciones_listar_por_usuario;
DROP PROCEDURE IF EXISTS sp_notificaciones_existe_alerta;
DROP PROCEDURE IF EXISTS sp_notificaciones_insertar;
DROP PROCEDURE IF EXISTS sp_notificaciones_marcar_leida;

DROP PROCEDURE IF EXISTS sp_bitacora_insertar;
DROP PROCEDURE IF EXISTS sp_bitacora_listar;
DROP PROCEDURE IF EXISTS sp_bitacora_listar_con_usuario;

DELIMITER //

-- ROLES
CREATE PROCEDURE sp_roles_listar()
BEGIN
    SELECT * FROM roles;
END //

CREATE PROCEDURE sp_roles_obtener_por_id(IN p_id_rol INT)
BEGIN
    SELECT * FROM roles WHERE id_rol = p_id_rol;
END //

CREATE PROCEDURE sp_roles_insertar(
    IN p_nombre VARCHAR(50),
    IN p_descripcion VARCHAR(255)
)
BEGIN
    INSERT INTO roles (nombre, descripcion) VALUES (p_nombre, p_descripcion);
    SELECT LAST_INSERT_ID() AS id_rol;
END //

CREATE PROCEDURE sp_roles_actualizar(
    IN p_id_rol INT,
    IN p_nombre VARCHAR(50),
    IN p_descripcion VARCHAR(255)
)
BEGIN
    UPDATE roles SET nombre = p_nombre, descripcion = p_descripcion WHERE id_rol = p_id_rol;
END //

CREATE PROCEDURE sp_roles_eliminar(IN p_id_rol INT)
BEGIN
    DELETE FROM roles WHERE id_rol = p_id_rol;
END //

-- USUARIOS
CREATE PROCEDURE sp_usuarios_listar()
BEGIN
    SELECT u.*, r.nombre AS RolNombre, r.descripcion AS RolDescripcion
    FROM usuarios u
    LEFT JOIN roles r ON u.id_rol = r.id_rol;
END //

CREATE PROCEDURE sp_usuarios_obtener_por_correo(IN p_correo VARCHAR(150))
BEGIN
    SELECT u.*, r.nombre AS RolNombre, r.descripcion AS RolDescripcion
    FROM usuarios u
    LEFT JOIN roles r ON u.id_rol = r.id_rol
    WHERE u.correo = p_correo;
END //

CREATE PROCEDURE sp_usuarios_obtener_por_id(IN p_id_usuario INT)
BEGIN
    SELECT u.*, r.nombre AS RolNombre, r.descripcion AS RolDescripcion
    FROM usuarios u
    LEFT JOIN roles r ON u.id_rol = r.id_rol
    WHERE u.id_usuario = p_id_usuario;
END //

CREATE PROCEDURE sp_usuarios_insertar(
    IN p_nombre VARCHAR(100),
    IN p_apellido VARCHAR(100),
    IN p_correo VARCHAR(150),
    IN p_password_hash VARCHAR(255),
    IN p_telefono VARCHAR(20),
    IN p_id_rol INT
)
BEGIN
    INSERT INTO usuarios (nombre, apellido, correo, password_hash, telefono, estado, correo_verificado, fecha_creacion, id_rol)
    VALUES (p_nombre, p_apellido, p_correo, p_password_hash, p_telefono, 1, 0, NOW(), p_id_rol);
    SELECT LAST_INSERT_ID() AS id_usuario;
END //

CREATE PROCEDURE sp_usuarios_actualizar(
    IN p_id_usuario INT,
    IN p_nombre VARCHAR(100),
    IN p_apellido VARCHAR(100),
    IN p_correo VARCHAR(150),
    IN p_telefono VARCHAR(20),
    IN p_estado TINYINT(1),
    IN p_id_rol INT
)
BEGIN
    UPDATE usuarios
    SET nombre = p_nombre,
        apellido = p_apellido,
        correo = p_correo,
        telefono = p_telefono,
        estado = p_estado,
        id_rol = p_id_rol
    WHERE id_usuario = p_id_usuario;
END //

CREATE PROCEDURE sp_usuarios_actualizar_ultimo_login(IN p_id_usuario INT)
BEGIN
    UPDATE usuarios SET ultimo_login = NOW() WHERE id_usuario = p_id_usuario;
END //

CREATE PROCEDURE sp_usuarios_actualizar_token_recuperacion(
    IN p_id_usuario INT,
    IN p_token VARCHAR(255),
    IN p_fecha_expiracion DATETIME
)
BEGIN
    UPDATE usuarios
    SET token_recuperacion = p_token,
        fecha_expiracion_recuperacion = p_fecha_expiracion
    WHERE id_usuario = p_id_usuario;
END //

CREATE PROCEDURE sp_usuarios_actualizar_token_verificacion(
    IN p_id_usuario INT,
    IN p_token VARCHAR(255),
    IN p_fecha_expiracion DATETIME
)
BEGIN
    UPDATE usuarios
    SET token_verificacion = p_token,
        fecha_expiracion_token = p_fecha_expiracion
    WHERE id_usuario = p_id_usuario;
END //

CREATE PROCEDURE sp_usuarios_actualizar_contrasena(
    IN p_id_usuario INT,
    IN p_password_hash VARCHAR(255)
)
BEGIN
    UPDATE usuarios
    SET password_hash = p_password_hash,
        token_recuperacion = NULL,
        fecha_expiracion_recuperacion = NULL
    WHERE id_usuario = p_id_usuario;
END //

CREATE PROCEDURE sp_usuarios_verificar_correo(IN p_id_usuario INT)
BEGIN
    UPDATE usuarios
    SET correo_verificado = 1,
        token_verificacion = NULL,
        fecha_expiracion_token = NULL
    WHERE id_usuario = p_id_usuario;
END //

-- CLIENTES (Calculando Lead Score dinámico en base a reglas de negocio)
CREATE PROCEDURE sp_clientes_listar()
BEGIN
    SELECT c.*,
           LEAST(
               LEAST(COALESCE((SELECT COUNT(*) FROM oportunidades o WHERE o.id_cliente = c.id_cliente), 0) * 20, 40) +
               LEAST(FLOOR(COALESCE((SELECT SUM(o.valor_estimado) FROM oportunidades o WHERE o.id_cliente = c.id_cliente), 0) / 10000) * 10, 30) +
               LEAST(COALESCE((SELECT COUNT(*) FROM citas ci WHERE ci.id_cliente = c.id_cliente), 0) * 15, 30) +
               LEAST(COALESCE((SELECT COUNT(*) FROM tareas t WHERE t.id_cliente = c.id_cliente AND t.estado = 'Completada'), 0) * 10, 20) +
               IF(c.estado = 'Activo', 10, 0),
               100
           ) AS LeadScore
    FROM clientes c
    ORDER BY c.id_cliente DESC;
END //

CREATE PROCEDURE sp_clientes_listar_por_usuario(IN p_id_usuario INT)
BEGIN
    SELECT c.*,
           LEAST(
               LEAST(COALESCE((SELECT COUNT(*) FROM oportunidades o WHERE o.id_cliente = c.id_cliente), 0) * 20, 40) +
               LEAST(FLOOR(COALESCE((SELECT SUM(o.valor_estimado) FROM oportunidades o WHERE o.id_cliente = c.id_cliente), 0) / 10000) * 10, 30) +
               LEAST(COALESCE((SELECT COUNT(*) FROM citas ci WHERE ci.id_cliente = c.id_cliente), 0) * 15, 30) +
               LEAST(COALESCE((SELECT COUNT(*) FROM tareas t WHERE t.id_cliente = c.id_cliente AND t.estado = 'Completada'), 0) * 10, 20) +
               IF(c.estado = 'Activo', 10, 0),
               100
           ) AS LeadScore
    FROM clientes c
    WHERE (p_id_usuario IS NULL AND c.id_usuario IS NULL) OR (p_id_usuario IS NOT NULL AND c.id_usuario = p_id_usuario)
    ORDER BY c.id_cliente DESC;
END //

CREATE PROCEDURE sp_clientes_obtener_por_id(IN p_id_cliente INT)
BEGIN
    SELECT c.*,
           LEAST(
               LEAST(COALESCE((SELECT COUNT(*) FROM oportunidades o WHERE o.id_cliente = c.id_cliente), 0) * 20, 40) +
               LEAST(FLOOR(COALESCE((SELECT SUM(o.valor_estimado) FROM oportunidades o WHERE o.id_cliente = c.id_cliente), 0) / 10000) * 10, 30) +
               LEAST(COALESCE((SELECT COUNT(*) FROM citas ci WHERE ci.id_cliente = c.id_cliente), 0) * 15, 30) +
               LEAST(COALESCE((SELECT COUNT(*) FROM tareas t WHERE t.id_cliente = c.id_cliente AND t.estado = 'Completada'), 0) * 10, 20) +
               IF(c.estado = 'Activo', 10, 0),
               100
           ) AS LeadScore
    FROM clientes c
    WHERE c.id_cliente = p_id_cliente;
END //

CREATE PROCEDURE sp_clientes_insertar(
    IN p_nombre VARCHAR(150),
    IN p_empresa VARCHAR(150),
    IN p_telefono VARCHAR(20),
    IN p_correo VARCHAR(150),
    IN p_direccion VARCHAR(255),
    IN p_estado VARCHAR(50),
    IN p_id_usuario INT
)
BEGIN
    INSERT INTO clientes (nombre, empresa, telefono, correo, direccion, estado, fecha_registro, id_usuario)
    VALUES (p_nombre, p_empresa, p_telefono, p_correo, p_direccion, p_estado, NOW(), p_id_usuario);
    SELECT LAST_INSERT_ID() AS id_cliente;
END //

CREATE PROCEDURE sp_clientes_actualizar(
    IN p_id_cliente INT,
    IN p_nombre VARCHAR(150),
    IN p_empresa VARCHAR(150),
    IN p_telefono VARCHAR(20),
    IN p_correo VARCHAR(150),
    IN p_direccion VARCHAR(255),
    IN p_estado VARCHAR(50),
    IN p_id_usuario INT
)
BEGIN
    UPDATE clientes
    SET nombre = p_nombre,
        empresa = p_empresa,
        telefono = p_telefono,
        correo = p_correo,
        direccion = p_direccion,
        estado = p_estado,
        id_usuario = p_id_usuario
    WHERE id_cliente = p_id_cliente;
END //

CREATE PROCEDURE sp_clientes_eliminar(IN p_id_cliente INT)
BEGIN
    DELETE FROM clientes WHERE id_cliente = p_id_cliente;
END //

-- CITAS
CREATE PROCEDURE sp_citas_listar()
BEGIN
    SELECT * FROM citas ORDER BY fecha DESC, hora DESC;
END //

CREATE PROCEDURE sp_citas_listar_con_cliente()
BEGIN
    SELECT c.*, cl.*
    FROM citas c
    LEFT JOIN clientes cl ON c.id_cliente = cl.id_cliente
    ORDER BY c.fecha DESC, c.hora DESC;
END //

CREATE PROCEDURE sp_citas_listar_con_relaciones()
BEGIN
    SELECT c.*, cl.*, u.*, co.nombre AS contacto_nombre
    FROM citas c
    LEFT JOIN clientes cl ON c.id_cliente = cl.id_cliente
    LEFT JOIN usuarios u ON c.id_usuario = u.id_usuario
    LEFT JOIN contacto_cliente co ON c.id_contacto = co.id_contacto
    ORDER BY c.fecha DESC, c.hora DESC;
END //

CREATE PROCEDURE sp_citas_listar_proximas_alertas(IN p_limite DATE)
BEGIN
    SELECT * FROM citas 
    WHERE estado NOT IN ('Completada', 'Realizada', 'Cancelada') AND fecha <= p_limite;
END //

CREATE PROCEDURE sp_citas_obtener_por_id(IN p_id_cita INT)
BEGIN
    SELECT * FROM citas WHERE id_cita = p_id_cita;
END //

CREATE PROCEDURE sp_citas_insertar(
    IN p_fecha DATE,
    IN p_hora TIME,
    IN p_descripcion VARCHAR(255),
    IN p_lugar VARCHAR(150),
    IN p_estado VARCHAR(50),
    IN p_id_cliente INT,
    IN p_id_usuario INT
)
BEGIN
    INSERT INTO citas (fecha, hora, descripcion, lugar, estado, id_cliente, id_usuario)
    VALUES (p_fecha, p_hora, p_descripcion, p_lugar, p_estado, p_id_cliente, p_id_usuario);
    SELECT LAST_INSERT_ID() AS id_cita;
END //

CREATE PROCEDURE sp_citas_actualizar(
    IN p_id_cita INT,
    IN p_fecha DATE,
    IN p_hora TIME,
    IN p_descripcion VARCHAR(255),
    IN p_lugar VARCHAR(150),
    IN p_estado VARCHAR(50),
    IN p_id_cliente INT,
    IN p_id_usuario INT
)
BEGIN
    UPDATE citas
    SET fecha = p_fecha,
        hora = p_hora,
        descripcion = p_descripcion,
        lugar = p_lugar,
        estado = p_estado,
        id_cliente = p_id_cliente,
        id_usuario = p_id_usuario
    WHERE id_cita = p_id_cita;
END //

CREATE PROCEDURE sp_citas_eliminar(IN p_id_cita INT)
BEGIN
    DELETE FROM citas WHERE id_cita = p_id_cita;
END //

-- TAREAS
CREATE PROCEDURE sp_tareas_listar()
BEGIN
    SELECT * FROM tareas ORDER BY id_tarea DESC;
END //

CREATE PROCEDURE sp_tareas_listar_con_contacto()
BEGIN
    SELECT t.*, co.nombre AS contacto_nombre, u.*, c.*
    FROM tareas t
    LEFT JOIN contacto_cliente co ON t.id_contacto = co.id_contacto
    LEFT JOIN usuarios u ON t.id_usuario = u.id_usuario
    LEFT JOIN clientes c ON t.id_cliente = c.id_cliente
    ORDER BY t.id_tarea DESC;
END //

CREATE PROCEDURE sp_tareas_obtener_con_contacto(IN p_id_tarea INT)
BEGIN
    SELECT t.*, co.nombre AS contacto_nombre
    FROM tareas t
    LEFT JOIN contacto_cliente co ON t.id_contacto = co.id_contacto
    WHERE t.id_tarea = p_id_tarea;
END //

CREATE PROCEDURE sp_tareas_obtener_por_id(IN p_id_tarea INT)
BEGIN
    SELECT * FROM tareas WHERE id_tarea = p_id_tarea;
END //

CREATE PROCEDURE sp_tareas_insertar(
    IN p_titulo VARCHAR(150),
    IN p_descripcion VARCHAR(255),
    IN p_prioridad VARCHAR(50),
    IN p_estado VARCHAR(50),
    IN p_fecha_limite DATE,
    IN p_id_cliente INT,
    IN p_id_usuario INT
)
BEGIN
    INSERT INTO tareas (titulo, descripcion, prioridad, estado, fecha_limite, id_cliente, id_usuario, alerta_disparada)
    VALUES (p_titulo, p_descripcion, p_prioridad, p_estado, p_fecha_limite, p_id_cliente, p_id_usuario, 0);
    SELECT LAST_INSERT_ID() AS id_tarea;
END //

CREATE PROCEDURE sp_tareas_actualizar(
    IN p_id_tarea INT,
    IN p_titulo VARCHAR(150),
    IN p_descripcion VARCHAR(255),
    IN p_prioridad VARCHAR(50),
    IN p_estado VARCHAR(50),
    IN p_fecha_limite DATE,
    IN p_id_cliente INT,
    IN p_id_usuario INT
)
BEGIN
    UPDATE tareas
    SET titulo = p_titulo,
        descripcion = p_descripcion,
        prioridad = p_prioridad,
        estado = p_estado,
        fecha_limite = p_fecha_limite,
        id_cliente = p_id_cliente,
        id_usuario = p_id_usuario
    WHERE id_tarea = p_id_tarea;
END //

CREATE PROCEDURE sp_tareas_eliminar(IN p_id_tarea INT)
BEGIN
    DELETE FROM tareas WHERE id_tarea = p_id_tarea;
END //

CREATE PROCEDURE sp_tareas_actualizar_alerta(IN p_id_tarea INT, IN p_alerta TINYINT(1))
BEGIN
    UPDATE tareas SET alerta_disparada = p_alerta WHERE id_tarea = p_id_tarea;
END //

-- OPORTUNIDADES
CREATE PROCEDURE sp_oportunidades_listar()
BEGIN
    SELECT * FROM oportunidades ORDER BY id_oportunidad DESC;
END //

CREATE PROCEDURE sp_oportunidades_listar_con_relaciones()
BEGIN
    SELECT o.*, c.*, u.*
    FROM oportunidades o
    LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
    LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
    ORDER BY o.id_oportunidad DESC;
END //

CREATE PROCEDURE sp_oportunidades_obtener_con_relaciones(IN p_id_oportunidad INT)
BEGIN
    SELECT o.*, c.*, u.*
    FROM oportunidades o
    LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
    LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
    WHERE o.id_oportunidad = p_id_oportunidad;
END //

CREATE PROCEDURE sp_oportunidades_obtener_por_id(IN p_id_oportunidad INT)
BEGIN
    SELECT * FROM oportunidades WHERE id_oportunidad = p_id_oportunidad;
END //

CREATE PROCEDURE sp_oportunidades_insertar(
    IN p_nombre VARCHAR(150),
    IN p_descripcion VARCHAR(255),
    IN p_etapa VARCHAR(100),
    IN p_probabilidad DECIMAL(5,2),
    IN p_valor_estimado DECIMAL(18,2),
    IN p_estado VARCHAR(50),
    IN p_id_cliente INT,
    IN p_id_usuario INT
)
BEGIN
    INSERT INTO oportunidades (nombre, descripcion, etapa, probabilidad, valor_estimado, fecha_creacion, estado, id_cliente, id_usuario)
    VALUES (p_nombre, p_descripcion, p_etapa, p_probabilidad, p_valor_estimado, NOW(), p_estado, p_id_cliente, p_id_usuario);
    SELECT LAST_INSERT_ID() AS id_oportunidad;
END //

CREATE PROCEDURE sp_oportunidades_actualizar(
    IN p_id_oportunidad INT,
    IN p_nombre VARCHAR(150),
    IN p_descripcion VARCHAR(255),
    IN p_etapa VARCHAR(100),
    IN p_probabilidad DECIMAL(5,2),
    IN p_valor_estimado DECIMAL(18,2),
    IN p_estado VARCHAR(50),
    IN p_id_cliente INT,
    IN p_id_usuario INT
)
BEGIN
    UPDATE oportunidades
    SET nombre = p_nombre,
        descripcion = p_descripcion,
        etapa = p_etapa,
        probabilidad = p_probabilidad,
        valor_estimado = p_valor_estimado,
        estado = p_estado,
        id_cliente = p_id_cliente,
        id_usuario = p_id_usuario
    WHERE id_oportunidad = p_id_oportunidad;
END //

CREATE PROCEDURE sp_oportunidades_eliminar(IN p_id_oportunidad INT)
BEGIN
    DELETE FROM oportunidades WHERE id_oportunidad = p_id_oportunidad;
END //

-- CONTACTOS
CREATE PROCEDURE sp_contactos_listar_por_cliente(IN p_id_cliente INT)
BEGIN
    SELECT * FROM contacto_cliente WHERE id_cliente = p_id_cliente;
END //

CREATE PROCEDURE sp_contactos_listar_con_cliente()
BEGIN
    SELECT co.*, cl.*
    FROM contacto_cliente co
    LEFT JOIN clientes cl ON co.id_cliente = cl.id_cliente;
END //

CREATE PROCEDURE sp_contactos_insertar(
    IN p_id_cliente INT,
    IN p_nombre VARCHAR(150),
    IN p_apellido VARCHAR(150),
    IN p_puesto VARCHAR(100),
    IN p_telefono VARCHAR(50),
    IN p_correo VARCHAR(100)
)
BEGIN
    INSERT INTO contacto_cliente (id_cliente, nombre, apellido, puesto, telefono, correo)
    VALUES (p_id_cliente, p_nombre, p_apellido, p_puesto, p_telefono, p_correo);
    SELECT LAST_INSERT_ID() AS id_contacto;
END //

CREATE PROCEDURE sp_contactos_eliminar(IN p_id_contacto INT)
BEGIN
    DELETE FROM contacto_cliente WHERE id_contacto = p_id_contacto;
END //

-- NOTAS
CREATE PROCEDURE sp_notas_listar_por_cliente(IN p_id_cliente INT)
BEGIN
    SELECT * FROM nota_cliente WHERE id_cliente = p_id_cliente ORDER BY fecha_creacion DESC;
END //

CREATE PROCEDURE sp_notas_insertar(
    IN p_id_cliente INT,
    IN p_comentario TEXT,
    IN p_id_usuario INT
)
BEGIN
    INSERT INTO nota_cliente (id_cliente, comentario, fecha_creacion, id_usuario)
    VALUES (p_id_cliente, p_comentario, NOW(), p_id_usuario);
    SELECT LAST_INSERT_ID() AS id_nota;
END //

CREATE PROCEDURE sp_notas_eliminar(IN p_id_nota INT)
BEGIN
    DELETE FROM nota_cliente WHERE id_nota = p_id_nota;
END //

-- NOTIFICACIONES
CREATE PROCEDURE sp_notificaciones_listar_por_usuario(IN p_id_usuario INT)
BEGIN
    SELECT * FROM notificaciones WHERE id_usuario = p_id_usuario ORDER BY fecha DESC;
END //

CREATE PROCEDURE sp_notificaciones_existe_alerta(IN p_id_referencia INT, IN p_tipo VARCHAR(50))
BEGIN
    SELECT COUNT(*) FROM notificaciones WHERE id_referencia = p_id_referencia AND tipo = p_tipo;
END //

CREATE PROCEDURE sp_notificaciones_insertar(
    IN p_mensaje VARCHAR(255),
    IN p_id_usuario INT,
    IN p_tipo VARCHAR(50),
    IN p_id_referencia INT
)
BEGIN
    INSERT INTO notificaciones (mensaje, fecha, leida, id_usuario, tipo, id_referencia)
    VALUES (p_mensaje, NOW(), 0, p_id_usuario, p_tipo, p_id_referencia);
    SELECT LAST_INSERT_ID() AS id_notificacion;
END //

CREATE PROCEDURE sp_notificaciones_marcar_leida(IN p_id_notificacion INT)
BEGIN
    UPDATE notificaciones SET leida = 1 WHERE id_notificacion = p_id_notificacion;
END //

-- BITACORA
CREATE PROCEDURE sp_bitacora_insertar(
    IN p_accion VARCHAR(50),
    IN p_tabla_afectada VARCHAR(100),
    IN p_id_registro_afectado INT,
    IN p_valor_anterior TEXT,
    IN p_valor_nuevo TEXT,
    IN p_direccion_ip VARCHAR(100),
    IN p_id_usuario INT
)
BEGIN
    INSERT INTO bitacora (accion, tabla_afectada, id_registro_afectado, valor_anterior, valor_nuevo, fecha_hora, direccion_ip, id_usuario)
    VALUES (p_accion, p_tabla_afectada, p_id_registro_afectado, p_valor_anterior, p_valor_nuevo, NOW(), p_direccion_ip, p_id_usuario);
    SELECT LAST_INSERT_ID() AS id_registro;
END //

CREATE PROCEDURE sp_bitacora_listar()
BEGIN
    SELECT * FROM bitacora ORDER BY fecha_hora DESC;
END //

CREATE PROCEDURE sp_bitacora_listar_con_usuario()
BEGIN
    SELECT b.*, u.*
    FROM bitacora b
    LEFT JOIN usuarios u ON b.id_usuario = u.id_usuario
    ORDER BY b.fecha_hora DESC;
END //

CREATE PROCEDURE sp_usuarios_obtener_por_token_recuperacion(IN p_token VARCHAR(255))
BEGIN
    SELECT * FROM usuarios WHERE token_recuperacion = p_token;
END //

//
DELIMITER ;
