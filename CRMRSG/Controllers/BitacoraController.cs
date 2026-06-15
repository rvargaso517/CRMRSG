using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class BitacoraController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        public ActionResult Index()
        {
            var historial = db.bitacoras
                              .OrderByDescending(x => x.fecha_hora)
                              .ToList();

            return View(historial);
        }
    }
}