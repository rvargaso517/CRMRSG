using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class TareasController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

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

            if (string.IsNullOrEmpty(filtroFecha))
            {
                filtroFecha = "todos";
            }
            ViewBag.FiltroFechaActivo = filtroFecha;

            var query = db.tareas.AsQueryable();

            if (isAdmin)
            {
                if (usuarioId.HasValue)
                {
                    query = query.Where(t => t.id_usuario == usuarioId.Value);
                }
                ViewBag.Usuarios = db.usuarios.ToList();
            }
            else
            {
                query = query.Where(t => t.id_usuario == currentUserId);
                ViewBag.Usuarios = db.usuarios.Where(u => u.id_usuario == currentUserId).ToList();
                usuarioId = currentUserId;
            }

            // Filtrar por fecha_limite
            DateTime today = DateTime.Today;
            if (filtroFecha == "hoy")
            {
                query = query.Where(t => t.fecha_limite.HasValue && DbFunctions.TruncateTime(t.fecha_limite.Value) == today);
            }
            else if (filtroFecha == "manana")
            {
                DateTime tomorrow = today.AddDays(1);
                query = query.Where(t => t.fecha_limite.HasValue && DbFunctions.TruncateTime(t.fecha_limite.Value) == tomorrow);
            }
            else if (filtroFecha == "semana")
            {
                DateTime endOfWeek = today.AddDays(7);
                query = query.Where(t => t.fecha_limite.HasValue && DbFunctions.TruncateTime(t.fecha_limite.Value) >= today && DbFunctions.TruncateTime(t.fecha_limite.Value) <= endOfWeek);
            }
            else if (filtroFecha == "mes")
            {
                DateTime endOfMonth = today.AddMonths(1);
                query = query.Where(t => t.fecha_limite.HasValue && DbFunctions.TruncateTime(t.fecha_limite.Value) >= today && DbFunctions.TruncateTime(t.fecha_limite.Value) <= endOfMonth);
            }

            var tareas = query.ToList();
            ViewBag.SelectedUsuarioId = usuarioId;

            // 1. Tareas por Usuario (Pie Chart) - Si se filtra por un usuario específico, mostramos su progreso por estado
            if (usuarioId.HasValue || !isAdmin)
            {
                var userStats = query
                    .GroupBy(t => t.estado ?? "Pendiente")
                    .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                    .ToList();
                ViewBag.UserLabels = userStats.Select(x => x.Nombre).ToArray();
                ViewBag.UserValues = userStats.Select(x => x.Cantidad).ToArray();
                ViewBag.UserChartTitle = "Mi Progreso de Tareas";
            }
            else
            {
                var userStats = query
                    .GroupBy(t => t.usuario != null ? t.usuario.nombre + " " + t.usuario.apellido : "Sin asignar")
                    .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                    .ToList();
                ViewBag.UserLabels = userStats.Select(x => x.Nombre).ToArray();
                ViewBag.UserValues = userStats.Select(x => x.Cantidad).ToArray();
                ViewBag.UserChartTitle = "Carga por Usuario";
            }

            // 2. Tareas por Estado / Categoría (Donut Chart)
            var catStats = query
                .GroupBy(t => t.estado ?? "Pendiente")
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToList();
            ViewBag.CategoryLabels = catStats.Select(x => x.Estado).ToArray();
            ViewBag.CategoryValues = catStats.Select(x => x.Cantidad).ToArray();

            // 3. Tareas por Prioridad (Bar Chart)
            var prioStats = query
                .GroupBy(t => t.prioridad ?? "Media")
                .Select(g => new { Prioridad = g.Key, Cantidad = g.Count() })
                .ToList();
            ViewBag.PriorityLabels = prioStats.Select(x => x.Prioridad).ToArray();
            ViewBag.PriorityValues = prioStats.Select(x => x.Cantidad).ToArray();

            return View(tareas);
        }

        // GET: Tareas/Crear
        public ActionResult Crear()
        {
            if (!TienePermiso("Tareas:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Tareas.";
                return RedirectToAction("Index");
            }
            ViewBag.Clientes = db.clientes.ToList();
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
                nuevaTarea.id_usuario = Session["UsuarioId"] != null
                    ? (int)Session["UsuarioId"]
                    : 1;

                db.tareas.Add(nuevaTarea);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.Clientes = db.clientes.ToList();
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

            ViewBag.Alta = db.tareas.Count(x => x.prioridad == "Alta");
            ViewBag.Media = db.tareas.Count(x => x.prioridad == "Media");
            ViewBag.Baja = db.tareas.Count(x => x.prioridad == "Baja");

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

            ViewBag.Pendientes = db.tareas.Count(x => x.estado == "Pendiente");
            ViewBag.EnProceso = db.tareas.Count(x => x.estado == "En Proceso");
            ViewBag.Completadas = db.tareas.Count(x => x.estado == "Completada");

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

            var t = db.tareas.Find(id);
            if (t != null)
            {
                t.estado = "Completada";
                db.SaveChanges();
                return Json(new { success = true, message = "Tarea marcada como completada." });
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

            var t = db.tareas.Find(id);
            if (t != null)
            {
                t.estado = "Aplazada";
                if (!string.IsNullOrWhiteSpace(razon))
                {
                    t.descripcion = (t.descripcion ?? "") + $" [Aplazada: {razon}]";
                }
                if (!string.IsNullOrWhiteSpace(nuevaFecha))
                {
                    t.fecha_limite = DateTime.Parse(nuevaFecha);
                }
                db.SaveChanges();
                return Json(new { success = true, message = "Tarea aplazada correctamente." });
            }
            return Json(new { success = false, message = "Tarea no encontrada." });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}