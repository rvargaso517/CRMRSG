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
            using (crm_rsgEntities db = new crm_rsgEntities())
            {
               
                ViewBag.ClientesList = new SelectList(db.cliente.Where(c => c.estado == "Activo").ToList(), "id_cliente", "empresa");
                return View();
            }
        }

        // Tareas agrupadas por prioridad
        public ActionResult Prioridades()
        {
            ViewBag.Alta = db.tareas.Count(x => x.prioridad == "Alta");
            ViewBag.Media = db.tareas.Count(x => x.prioridad == "Media");
            ViewBag.Baja = db.tareas.Count(x => x.prioridad == "Baja");

            return View();
        }

        
        // Tareas agrupadas por categor�as (estados)
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