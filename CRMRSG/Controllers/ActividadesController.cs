using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ActividadesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        public ActionResult Index()
        {
            ViewBag.Pendientes = db.citas.Count(x => x.estado == "Pendiente");
            ViewBag.Confirmadas = db.citas.Count(x => x.estado == "Confirmada");
            ViewBag.Canceladas = db.citas.Count(x => x.estado == "Cancelada");

            return View();
        }

        public ActionResult Crear()
        {
            return View();
        }
    }
}