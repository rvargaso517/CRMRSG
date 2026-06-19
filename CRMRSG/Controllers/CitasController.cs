using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class CitasController : Controller
    {
        // GET: Citas
        public ActionResult Index()
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                var citas = db.citas.Include(c => c.cliente).ToList();
                return View(citas);
            }
        }

        // POST: Citas/Agendar
        [HttpPost]
        [AntiForgeryToken]
        public ActionResult Agendar(int id_cliente, string asunto, DateTime fecha_cita)
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                var nuevaCita = new citas
                {
                    id_cliente = id_cliente,
                    asunto = asunto,
                    fecha_cita = fecha_cita
                };
                db.citas.Add(nuevaCita);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}