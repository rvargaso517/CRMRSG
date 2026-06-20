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
        [ValidateAntiForgeryToken]
        public ActionResult Agendar(int id_cliente, string asunto, DateTime fecha_cita)
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                var nuevaCita = new cita
                {
                    id_cliente = id_cliente,
                    descripcion = asunto,
                    fecha = fecha_cita.Date,
                    hora = fecha_cita.TimeOfDay
                };
                db.citas.Add(nuevaCita);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}