using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class DashboardController : Controller
    {
        private crm_rsgEntities db = new crm_rsgEntities();

        // GET: Dashboard
        public ActionResult Index()
        {
            ViewBag.TotalClientes = db.clientes.Count();
            ViewBag.TotalOportunidades = db.oportunidades.Count();
            ViewBag.TotalTareas = db.tareas.Count();
            ViewBag.TotalUsuarios = db.usuarios.Count();

            return View();
        }

        public ActionResult Calendar()
        {
            return View();
        }
    }
}