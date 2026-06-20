using CRMRSG.EntityFramework;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace CRMRSG.Controllers
{
    public class RolesController : Controller
    {
        // GET: Roles
        public ActionResult Index()
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                var roles = db.roles.ToList();
                return View(roles);
            }
        }

        // POST: Roles/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string nombre_rol)
        {
            if (string.IsNullOrEmpty(nombre_rol)) return RedirectToAction("Index");

            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                var nuevoRol = new role { nombre = nombre_rol };
                db.roles.Add(nuevoRol);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}