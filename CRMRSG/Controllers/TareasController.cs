using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class TareasController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Tareas
        public ActionResult Index(int? usuarioId)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            int currentUserId = (int)Session["UsuarioId"];
            bool isAdmin = Session["RolId"] != null && (int)Session["RolId"] == 1;

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

            var tareas = query.ToList();
            ViewBag.SelectedUsuarioId = usuarioId;

            // 1. Tareas por Usuario (Pie Chart)
            var userStats = db.tareas
                .GroupBy(t => t.usuario != null ? t.usuario.nombre + " " + t.usuario.apellido : "Sin asignar")
                .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                .ToList();
            ViewBag.UserLabels = userStats.Select(x => x.Nombre).ToArray();
            ViewBag.UserValues = userStats.Select(x => x.Cantidad).ToArray();

            // 2. Tareas por Estado / Categoría (Donut Chart)
            var catStats = db.tareas
                .GroupBy(t => t.estado ?? "Pendiente")
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToList();
            ViewBag.CategoryLabels = catStats.Select(x => x.Estado).ToArray();
            ViewBag.CategoryValues = catStats.Select(x => x.Cantidad).ToArray();

            // 3. Tareas por Prioridad (Bar Chart)
            var prioStats = db.tareas
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
            ViewBag.Clientes = db.clientes.ToList();
            return View();
        }

        // POST: Tareas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(tarea nuevaTarea)
        {
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
            ViewBag.Alta = db.tareas.Count(x => x.prioridad == "Alta");
            ViewBag.Media = db.tareas.Count(x => x.prioridad == "Media");
            ViewBag.Baja = db.tareas.Count(x => x.prioridad == "Baja");

            return View();
        }


        // Tareas agrupadas por categorías (estados)
        public ActionResult Categorias()
        {
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
            if (Session["UsuarioId"] == null)
            {
                return Json(new { success = false, message = "Sesión no válida" });
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
            if (Session["UsuarioId"] == null)
            {
                return Json(new { success = false, message = "Sesión no válida" });
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
                return Json(new { success = true, message = "Tarea aplazada con éxito." });
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