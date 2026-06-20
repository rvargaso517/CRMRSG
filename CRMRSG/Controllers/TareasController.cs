using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class TareasController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Tareas
        public ActionResult Index()
        {
            var tareas = db.tareas.ToList();
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