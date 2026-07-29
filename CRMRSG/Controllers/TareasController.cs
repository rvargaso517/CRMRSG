using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class TareasController : Controller
    {
        private bool TienePermiso(string permiso)
        {
            if (Session["UsuarioId"] == null) return false;
            if (Session["RolId"] != null && (int)Session["RolId"] == 1) return true;
            if (Session["Permisos"] == null) return false;
            string perms = Session["Permisos"].ToString();
            return perms.Split(',').Contains(permiso) || perms.Split(',').Contains("Admin:Acceso");
        }

        // GET: Tareas
        public ActionResult Index(int? usuarioId, string filtroFecha)
        {
            if (!TienePermiso("Tareas:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Tareas.";
                return RedirectToAction("Index", "Dashboard");
            }

            int currentUserId = (int)Session["UsuarioId"];
            bool isAdmin = Session["RolId"] != null && (int)Session["RolId"] == 1;

            VerificarYGenerarAlertas(currentUserId, isAdmin);

            if (string.IsNullOrEmpty(filtroFecha))
            {
                filtroFecha = "todos";
            }
            ViewBag.FiltroFechaActivo = filtroFecha;

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listado = db.Query<tarea, usuario, cliente, tarea>(
                    "sp_tareas_listar_con_contacto",
                    (t, u, c) => {
                        t.usuario = u;
                        t.cliente = c;
                        return t;
                    },
                    splitOn: "id_usuario,id_cliente",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                // Filtrar por rol y usuario
                if (isAdmin)
                {
                    if (usuarioId.HasValue)
                    {
                        listado = listado.Where(t => t.id_usuario == usuarioId.Value).ToList();
                    }
                    ViewBag.Usuarios = db.Query<usuario>(
                        "sp_usuarios_listar",
                        commandType: CommandType.StoredProcedure
                    ).ToList();
                }
                else
                {
                    listado = listado.Where(t => t.id_usuario == currentUserId).ToList();
                    ViewBag.Usuarios = db.Query<usuario>(
                        "sp_usuarios_obtener_por_id",
                        new { p_id_usuario = currentUserId },
                        commandType: CommandType.StoredProcedure
                    ).ToList();
                    usuarioId = currentUserId;
                }

                // Filtrar por fecha
                DateTime today = DateTime.Today;
                if (filtroFecha == "hoy")
                {
                    listado = listado.Where(t => t.fecha_limite.HasValue && t.fecha_limite.Value.Date == today).ToList();
                }
                else if (filtroFecha == "manana")
                {
                    DateTime tomorrow = today.AddDays(1);
                    listado = listado.Where(t => t.fecha_limite.HasValue && t.fecha_limite.Value.Date == tomorrow).ToList();
                }
                else if (filtroFecha == "semana")
                {
                    DateTime endOfWeek = today.AddDays(7);
                    listado = listado.Where(t => t.fecha_limite.HasValue && t.fecha_limite.Value.Date >= today && t.fecha_limite.Value.Date <= endOfWeek).ToList();
                }
                else if (filtroFecha == "mes")
                {
                    DateTime endOfMonth = today.AddMonths(1);
                    listado = listado.Where(t => t.fecha_limite.HasValue && t.fecha_limite.Value.Date >= today && t.fecha_limite.Value.Date <= endOfMonth).ToList();
                }

                ViewBag.SelectedUsuarioId = usuarioId;

                // 1. Tareas por Usuario (Pie Chart)
                if (usuarioId.HasValue || !isAdmin)
                {
                    var userStats = listado
                        .GroupBy(t => t.estado ?? "Pendiente")
                        .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                        .ToList();
                    ViewBag.UserLabels = userStats.Select(x => x.Nombre).ToArray();
                    ViewBag.UserValues = userStats.Select(x => x.Cantidad).ToArray();
                    ViewBag.UserChartTitle = "Mi Progreso de Tareas";
                }
                else
                {
                    // Cargar usuario completo para los nombres en el gráfico
                    var usuariosMap = db.Query<usuario>("sp_usuarios_listar", commandType: CommandType.StoredProcedure)
                                        .ToDictionary(u => u.id_usuario, u => $"{u.nombre} {u.apellido}");

                    var userStats = listado
                        .GroupBy(t => t.id_usuario.HasValue && usuariosMap.ContainsKey(t.id_usuario.Value) ? usuariosMap[t.id_usuario.Value] : "Sin asignar")
                        .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                        .ToList();
                    ViewBag.UserLabels = userStats.Select(x => x.Nombre).ToArray();
                    ViewBag.UserValues = userStats.Select(x => x.Cantidad).ToArray();
                    ViewBag.UserChartTitle = "Carga por Usuario";
                }

                // 2. Tareas por Estado / Categoría (Donut Chart)
                var catStats = listado
                    .GroupBy(t => t.estado ?? "Pendiente")
                    .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                    .ToList();
                ViewBag.CategoryLabels = catStats.Select(x => x.Estado).ToArray();
                ViewBag.CategoryValues = catStats.Select(x => x.Cantidad).ToArray();

                // 3. Tareas por Prioridad (Bar Chart)
                var prioStats = listado
                    .GroupBy(t => t.prioridad ?? "Media")
                    .Select(g => new { Prioridad = g.Key, Cantidad = g.Count() })
                    .ToList();
                ViewBag.PriorityLabels = prioStats.Select(x => x.Prioridad).ToArray();
                ViewBag.PriorityValues = prioStats.Select(x => x.Cantidad).ToArray();

                return View(listado);
            }
        }

        private void VerificarYGenerarAlertas(int currentUserId, bool isAdmin)
        {
            DateTime limiteAlerta = DateTime.Today.AddDays(2);
            DateTime hoy = DateTime.Today;

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listado = db.Query<tarea>(
                    "sp_tareas_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var tareasProximas = listado
                    .Where(t => t.estado != "Completada"
                             && t.fecha_limite.HasValue
                             && t.fecha_limite.Value.Date <= limiteAlerta
                             && (isAdmin || t.id_usuario == currentUserId))
                    .ToList();

                foreach (var tarea in tareasProximas)
                {
                    if (tarea.alerta_disparada == null || tarea.alerta_disparada == false)
                    {
                        string diasRestantesMsg = "";
                        if (tarea.fecha_limite.Value.Date < hoy)
                        {
                            diasRestantesMsg = "¡Está VENCIDA desde el " + tarea.fecha_limite.Value.ToString("dd/MM/yyyy") + "!";
                        }
                        else if (tarea.fecha_limite.Value.Date == hoy)
                        {
                            diasRestantesMsg = "Vence HOY.";
                        }
                        else
                        {
                            int dias = (tarea.fecha_limite.Value.Date - hoy).Days;
                            diasRestantesMsg = $"Vence en {dias} días ({tarea.fecha_limite.Value.ToString("dd/MM/yyyy")}).";
                        }

                        // Insertar notificación
                        db.Execute(
                            "sp_notificaciones_insertar",
                            new {
                                p_mensaje = $"Alerta de Seguimiento: La tarea '{tarea.titulo}' requiere atención. {diasRestantesMsg}",
                                p_id_usuario = tarea.id_usuario,
                                p_tipo = "Alerta de Seguimiento",
                                p_id_referencia = tarea.id_tarea
                            },
                            commandType: CommandType.StoredProcedure
                        );

                        // Actualizar tarea alerta_disparada
                        db.Execute(
                            "sp_tareas_actualizar_alerta",
                            new { p_id_tarea = tarea.id_tarea, p_alerta = 1 },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
            }
        }

        // GET: Tareas/Crear
        public ActionResult Crear()
        {
            if (!TienePermiso("Tareas:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Tareas.";
                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Clientes = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Responsables = db.Query<usuario>("sp_usuarios_listar", commandType: CommandType.StoredProcedure).ToList();
                
                // Contactos secundarios
                ViewBag.Contactos = db.Query<contacto_cliente>("SELECT * FROM contacto_cliente").ToList();
            }
            return View();
        }

        // POST: Tareas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(tarea nuevaTarea)
        {
            if (!TienePermiso("Tareas:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Tareas.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                nuevaTarea.estado = "Pendiente";
                if (nuevaTarea.id_usuario == null || nuevaTarea.id_usuario == 0)
                {
                    nuevaTarea.id_usuario = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;
                }

                using (var db = DbConnectionFactory.GetConnection())
                {
                    var id_tarea = db.QuerySingle<int>(
                        "sp_tareas_insertar",
                        new {
                            p_titulo = nuevaTarea.titulo,
                            p_descripcion = nuevaTarea.descripcion,
                            p_prioridad = nuevaTarea.prioridad,
                            p_estado = nuevaTarea.estado,
                            p_fecha_limite = nuevaTarea.fecha_limite,
                            p_id_cliente = nuevaTarea.id_cliente,
                            p_id_usuario = nuevaTarea.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );
                    nuevaTarea.id_tarea = id_tarea;

                    if (nuevaTarea.id_contacto.HasValue && nuevaTarea.id_contacto.Value > 0)
                    {
                        db.Execute("UPDATE tareas SET id_contacto = @IdContacto WHERE id_tarea = @IdTarea", new { IdContacto = nuevaTarea.id_contacto.Value, IdTarea = nuevaTarea.id_tarea });
                    }

                    if (nuevaTarea.id_usuario.HasValue)
                    {
                        db.Execute(
                            "sp_notificaciones_insertar",
                            new {
                                p_mensaje = $"Nueva Tarea: Se te ha asignado la tarea '{nuevaTarea.titulo}' con fecha límite {(nuevaTarea.fecha_limite.HasValue ? nuevaTarea.fecha_limite.Value.ToString("dd/MM/yyyy") : "Sin definir")}.",
                                p_id_usuario = nuevaTarea.id_usuario.Value,
                                p_tipo = "Tarea Creada",
                                p_id_referencia = nuevaTarea.id_tarea
                            },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }

                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Clientes = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Responsables = db.Query<usuario>("sp_usuarios_listar", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Contactos = db.Query<contacto_cliente>("SELECT * FROM contacto_cliente").ToList();
            }
            return View(nuevaTarea);
        }

        // Tareas agrupadas por prioridad
        public ActionResult Prioridades()
        {
            if (!TienePermiso("Tareas:Ver"))
            {
                TempData["Error"] = "No autorizado.";
                return RedirectToAction("Index", "Dashboard");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Alta = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE prioridad = 'Alta'");
                ViewBag.Media = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE prioridad = 'Media'");
                ViewBag.Baja = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE prioridad = 'Baja'");
            }

            return View();
        }

        // Tareas agrupadas por categorías (estados)
        public ActionResult Categorias()
        {
            if (!TienePermiso("Tareas:Ver"))
            {
                TempData["Error"] = "No autorizado.";
                return RedirectToAction("Index", "Dashboard");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Pendientes = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE estado = 'Pendiente'");
                ViewBag.EnProceso = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE estado = 'En Proceso'");
                ViewBag.Completadas = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE estado = 'Completada'");
            }

            return View();
        }

        // POST: Tareas/Completar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Completar(int id)
        {
            if (!TienePermiso("Tareas:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var t = db.QueryFirstOrDefault<tarea>(
                    "sp_tareas_obtener_por_id",
                    new { p_id_tarea = id },
                    commandType: CommandType.StoredProcedure
                );

                if (t != null)
                {
                    db.Execute(
                        "sp_tareas_actualizar",
                        new {
                            p_id_tarea = id,
                            p_titulo = t.titulo,
                            p_descripcion = t.descripcion,
                            p_prioridad = t.prioridad,
                            p_estado = "Completada",
                            p_fecha_limite = t.fecha_limite,
                            p_id_cliente = t.id_cliente,
                            p_id_usuario = t.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { success = true, message = "Tarea marcada como completada." });
                }
            }
            return Json(new { success = false, message = "Tarea no encontrada." });
        }

        // POST: Tareas/Posponer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Posponer(int id, string razon, string nuevaFecha)
        {
            if (!TienePermiso("Tareas:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var t = db.QueryFirstOrDefault<tarea>(
                    "sp_tareas_obtener_por_id",
                    new { p_id_tarea = id },
                    commandType: CommandType.StoredProcedure
                );

                if (t != null)
                {
                    string desc = t.descripcion;
                    if (!string.IsNullOrWhiteSpace(razon))
                    {
                        desc = (t.descripcion ?? "") + $" [Aplazada: {razon}]";
                    }

                    DateTime? fLim = t.fecha_limite;
                    if (!string.IsNullOrWhiteSpace(nuevaFecha))
                    {
                        fLim = DateTime.Parse(nuevaFecha);
                    }

                    db.Execute(
                        "sp_tareas_actualizar",
                        new {
                            p_id_tarea = id,
                            p_titulo = t.titulo,
                            p_descripcion = desc,
                            p_prioridad = t.prioridad,
                            p_estado = "Aplazada",
                            p_fecha_limite = fLim,
                            p_id_cliente = t.id_cliente,
                            p_id_usuario = t.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { success = true, message = "Tarea aplazada correctamente." });
                }
            }
            return Json(new { success = false, message = "Tarea no encontrada." });
        }
    }
}