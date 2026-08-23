using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class ActividadesController : Controller
    {
        private bool TienePermiso(string permiso)
        {
            if (Session["UsuarioId"] == null) return false;
            if (Session["RolId"] != null && (int)Session["RolId"] == 1) return true;
            if (Session["Permisos"] == null) return false;
            string perms = Session["Permisos"].ToString();
            return perms.Split(',').Contains(permiso) || perms.Split(',').Contains("Admin:Acceso");
        }

        // GET: Actividades
        public ActionResult Index(string filtro, string estado)
        {
            if (!TienePermiso("Actividades:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Actividades.";
                return RedirectToAction("Index", "Dashboard");
            }

            int usuarioId = (int)Session["UsuarioId"];
            bool isAdmin = Session["RolId"] != null && (int)Session["RolId"] == 1;

            if (string.IsNullOrEmpty(filtro))
            {
                filtro = "todos";
            }
            if (string.IsNullOrEmpty(estado))
            {
                estado = "todos";
            }

            ViewBag.FiltroActivo = filtro;
            ViewBag.EstadoActivo = estado;

            DateTime desde = DateTime.MinValue;
            if (filtro == "dia") desde = DateTime.Today;
            else if (filtro == "semana") desde = DateTime.Today.AddDays(-7);
            else if (filtro == "mes") desde = DateTime.Today.AddMonths(-1);

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listado = db.Query<cita, cliente, usuario, cita>(
                    "sp_citas_listar_con_relaciones",
                    (c, cl, usr) => {
                        c.cliente = cl;
                        c.usuario = usr;
                        return c;
                    },
                    splitOn: "id_cliente,id_usuario",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                // Filtrar por rol
                if (!isAdmin)
                {
                    listado = listado.Where(c => c.id_usuario == usuarioId).ToList();
                }

                // Filtrar por fecha
                if (filtro != "todos")
                {
                    listado = listado.Where(c => c.fecha >= desde).ToList();
                }

                // Estadísticas rápidas antes de filtrar por estado
                ViewBag.Pendientes = listado.Count(x => x.estado == "Pendiente" || x.estado == "Programada");
                ViewBag.Confirmadas = listado.Count(x => x.estado == "Completada" || x.estado == "Confirmada" || x.estado == "Realizada");
                ViewBag.Canceladas = listado.Count(x => x.estado == "Cancelada" || x.estado == "Suspendida");
                ViewBag.Aplazadas = listado.Count(x => x.estado == "Aplazada");

                // Filtrar por estado
                if (estado != "todos")
                {
                    if (estado == "Pendiente")
                    {
                        listado = listado.Where(c => c.estado == "Pendiente" || c.estado == "Programada").ToList();
                    }
                    else if (estado == "Realizada")
                    {
                        listado = listado.Where(c => c.estado == "Completada" || c.estado == "Confirmada" || c.estado == "Realizada").ToList();
                    }
                    else if (estado == "Aplazada")
                    {
                        listado = listado.Where(c => c.estado == "Aplazada").ToList();
                    }
                    else if (estado == "Cancelada")
                    {
                        listado = listado.Where(c => c.estado == "Cancelada" || c.estado == "Suspendida").ToList();
                    }
                }

                return View(listado);
            }
        }

        // GET: Actividades/Crear
        public ActionResult Crear()
        {
            if (!TienePermiso("Actividades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Actividades.";
                return RedirectToAction("Index");
            }

            int usuarioId = (int)Session["UsuarioId"];
            bool isAdmin = Session["RolId"] != null && (int)Session["RolId"] == 1;

            using (var db = DbConnectionFactory.GetConnection())
            {
                if (isAdmin)
                {
                    ViewBag.Clientes = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).ToList();
                    ViewBag.Oportunidades = db.Query<oportunidade>("sp_oportunidades_listar", commandType: CommandType.StoredProcedure).ToList();
                }
                else
                {
                    ViewBag.Clientes = db.Query<cliente>("sp_clientes_listar_por_usuario", new { p_id_usuario = usuarioId }, commandType: CommandType.StoredProcedure).ToList();
                    ViewBag.Oportunidades = db.Query<oportunidade>("sp_oportunidades_listar", commandType: CommandType.StoredProcedure).Where(o => o.id_usuario == usuarioId).ToList();
                }

                ViewBag.Contactos = db.Query<contacto_cliente>("SELECT * FROM contacto_cliente").ToList();
            }

            return View();
        }

        // POST: Actividades/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string tipo_actividad, string fecha, string hora, int? id_cliente, int? id_contacto, string descripcion)
        {
            if (!TienePermiso("Actividades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para registrar Actividades.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(tipo_actividad) || string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(hora) || string.IsNullOrWhiteSpace(descripcion))
            {
                TempData["Error"] = "Todos los campos obligatorios deben ser completados.";
                return RedirectToAction("Crear");
            }

            try
            {
                var desc = tipo_actividad + ": " + descripcion;
                var dtFecha = DateTime.Parse(fecha);
                var tsHora = TimeSpan.Parse(hora);
                int usuarioId = (int)Session["UsuarioId"];

                using (var db = DbConnectionFactory.GetConnection())
                {
                    var id_cita = db.QuerySingle<int>(
                        "sp_citas_insertar",
                        new {
                            p_fecha = dtFecha,
                            p_hora = tsHora,
                            p_descripcion = desc,
                            p_lugar = tipo_actividad,
                            p_estado = "Pendiente",
                            p_id_cliente = id_cliente,
                            p_id_usuario = usuarioId
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    if (id_contacto.HasValue && id_contacto.Value > 0)
                    {
                        db.Execute("UPDATE citas SET id_contacto = @IdContacto WHERE id_cita = @IdCita", new { IdContacto = id_contacto.Value, IdCita = id_cita });
                    }

                    db.Execute(
                        "sp_notificaciones_insertar",
                        new {
                            p_mensaje = $"Nueva Actividad: Se ha registrado la actividad '{desc}' para el {dtFecha.ToString("dd/MM/yyyy")}.",
                            p_id_usuario = usuarioId,
                            p_tipo = "Actividad Creada",
                            p_id_referencia = id_cita
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    // Insertar en Bitácora (trazabilidad completa)
                    string ipAddress = Request.UserHostAddress ?? "127.0.0.1";
                    db.Execute(
                        "sp_bitacora_insertar",
                        new {
                            p_accion = "Creación",
                            p_tabla_afectada = "citas",
                            p_id_registro_afectado = id_cita,
                            p_valor_anterior = "NULL",
                            p_valor_nuevo = $"Tipo: {tipo_actividad}",
                            p_direccion_ip = ipAddress,
                            p_id_usuario = usuarioId
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                TempData["Success"] = "Actividad registrada con éxito.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al registrar la actividad: " + ex.Message;
                return RedirectToAction("Crear");
            }
        }

        // POST: Actividades/Completar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Completar(int id)
        {
            if (!TienePermiso("Actividades:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var c = db.QueryFirstOrDefault<cita>(
                    "sp_citas_obtener_por_id",
                    new { p_id_cita = id },
                    commandType: CommandType.StoredProcedure
                );

                if (c != null)
                {
                    db.Execute(
                        "sp_citas_actualizar",
                        new {
                            p_id_cita = id,
                            p_fecha = c.fecha,
                            p_hora = c.hora,
                            p_descripcion = c.descripcion,
                            p_lugar = c.lugar,
                            p_estado = "Completada",
                            p_id_cliente = c.id_cliente,
                            p_id_usuario = c.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    // Insertar en Bitácora (trazabilidad completa)
                    string ipAddress = Request.UserHostAddress ?? "127.0.0.1";
                    int currentUserId = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;
                    db.Execute(
                        "sp_bitacora_insertar",
                        new {
                            p_accion = "Modificación",
                            p_tabla_afectada = "citas",
                            p_id_registro_afectado = id,
                            p_valor_anterior = c.estado,
                            p_valor_nuevo = "Completada",
                            p_direccion_ip = ipAddress,
                            p_id_usuario = currentUserId
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { success = true, message = "Actividad marcada como realizada." });
                }
            }
            return Json(new { success = false, message = "Actividad no encontrada." });
        }

        // POST: Actividades/Posponer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Posponer(int id, string razon, string nuevaFecha)
        {
            if (!TienePermiso("Actividades:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var c = db.QueryFirstOrDefault<cita>(
                    "sp_citas_obtener_por_id",
                    new { p_id_cita = id },
                    commandType: CommandType.StoredProcedure
                );

                if (c != null)
                {
                    string desc = c.descripcion;
                    if (!string.IsNullOrWhiteSpace(razon))
                    {
                        desc = (c.descripcion ?? "") + $" [Aplazada: {razon}]";
                    }

                    DateTime fVal = c.fecha;
                    if (!string.IsNullOrWhiteSpace(nuevaFecha))
                    {
                        fVal = DateTime.Parse(nuevaFecha);
                    }

                    db.Execute(
                        "sp_citas_actualizar",
                        new {
                            p_id_cita = id,
                            p_fecha = fVal,
                            p_hora = c.hora,
                            p_descripcion = desc,
                            p_lugar = c.lugar,
                            p_estado = "Aplazada",
                            p_id_cliente = c.id_cliente,
                            p_id_usuario = c.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    // Insertar en Bitácora (trazabilidad completa)
                    string ipAddress = Request.UserHostAddress ?? "127.0.0.1";
                    int currentUserId = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;
                    db.Execute(
                        "sp_bitacora_insertar",
                        new {
                            p_accion = "Modificación",
                            p_tabla_afectada = "citas",
                            p_id_registro_afectado = id,
                            p_valor_anterior = c.estado,
                            p_valor_nuevo = "Aplazada",
                            p_direccion_ip = ipAddress,
                            p_id_usuario = currentUserId
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { success = true, message = "Actividad aplazada correctamente." });
                }
            }
            return Json(new { success = false, message = "Actividad no encontrada." });
        }

        // POST: Actividades/Cancelar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(int id)
        {
            if (!TienePermiso("Actividades:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var c = db.QueryFirstOrDefault<cita>(
                    "sp_citas_obtener_por_id",
                    new { p_id_cita = id },
                    commandType: CommandType.StoredProcedure
                );

                if (c != null)
                {
                    db.Execute(
                        "sp_citas_actualizar",
                        new {
                            p_id_cita = id,
                            p_fecha = c.fecha,
                            p_hora = c.hora,
                            p_descripcion = c.descripcion,
                            p_lugar = c.lugar,
                            p_estado = "Cancelada",
                            p_id_cliente = c.id_cliente,
                            p_id_usuario = c.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    // Insertar en Bitácora (trazabilidad completa)
                    string ipAddress = Request.UserHostAddress ?? "127.0.0.1";
                    int currentUserId = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;
                    db.Execute(
                        "sp_bitacora_insertar",
                        new {
                            p_accion = "Modificación",
                            p_tabla_afectada = "citas",
                            p_id_registro_afectado = id,
                            p_valor_anterior = c.estado,
                            p_valor_nuevo = "Cancelada",
                            p_direccion_ip = ipAddress,
                            p_id_usuario = currentUserId
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { success = true, message = "Actividad marcada como cancelada." });
                }
            }
            return Json(new { success = false, message = "Actividad no encontrada." });
        }
    }
}