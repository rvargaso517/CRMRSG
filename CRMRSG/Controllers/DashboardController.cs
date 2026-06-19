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
            return View();
        }

        public ActionResult Calendar()
        {
            return View();
        }

        // =========================================================
        // NUEVA LOGICA: HU-025 - Estadísticas de Eventos para Supervisor
        // =========================================================
        // GET: Dashboard/ObtenerEstadisticasEventos
        // GET: Dashboard/ObtenerEstadisticasEventos
        [HttpGet]
        public JsonResult ObtenerEstadisticasEventos()
        {
            try
            {
                int anioActual = DateTime.Now.Year;

                // Como 'fecha' no es nullable, filtramos y agrupamos directamente sobre la propiedad
                var datosEventos = db.citas
                    .Where(c => c.fecha.Year == anioActual)
                    .GroupBy(c => c.fecha.Month)
                    .Select(g => new
                    {
                        Mes = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToList();

                return Json(datosEventos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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