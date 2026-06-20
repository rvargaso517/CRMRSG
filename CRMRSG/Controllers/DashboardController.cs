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
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Dashboard
        public ActionResult Index()
        {
            ViewBag.TotalClientes = db.clientes.Count();
            ViewBag.TotalOportunidades = db.oportunidades.Count();
            ViewBag.TotalTareas = db.tareas.Count();
            ViewBag.TotalUsuarios = db.usuarios.Count();

            // HU-035 - Rendimiento de vendedores
            var vendedores = db.usuarios.Select(u => new
            {
                Nombre = u.nombre + " " + u.apellido,
                Clientes = u.clientes.Count(),
                Oportunidades = u.oportunidades.Count(),
                Tareas = u.tareas.Count()
            }).ToList();

            ViewBag.Vendedores = vendedores;

            return View();
        }

        public ActionResult Calendar()
        {
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