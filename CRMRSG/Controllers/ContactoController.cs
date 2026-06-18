using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework; 

namespace CRMRSG.Controllers
{
    public class ContactosController : Controller
    {
        // GET: Contactos
        public ActionResult Index()
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                // Jalamos los contactos e incluimos la empresa a la que pertenecen
                var contactos = db.contacto_cliente.Include(c => c.cliente).ToList();
                return View(contactos);
            }
        }

        // POST: Contactos/Eliminar/5
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                using (CRM_RSGEntities db = new CRM_RSGEntities())
                {
                    var contacto = db.contacto_cliente.Find(id);
                    if (contacto == null)
                    {
                        return Json(new { success = false, message = "Contacto no encontrado." });
                    }

                    db.contacto_cliente.Remove(contacto);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Contacto eliminado correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}