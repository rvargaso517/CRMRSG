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
        public ActionResult Crear(string nombre_rol, string[] permisos)
        {
            if (string.IsNullOrEmpty(nombre_rol)) return RedirectToAction("Index");

            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                string nombreTrimmed = nombre_rol.Trim();
                string nombreLower = nombreTrimmed.ToLower();
                if (db.roles.Any(r => r.nombre.ToLower() == nombreLower))
                {
                    TempData["Error"] = "El rol '" + nombreTrimmed + "' ya existe.";
                    return RedirectToAction("Index");
                }

                string desc = permisos != null ? string.Join(",", permisos) : "Sin permisos";
                var nuevoRol = new role { nombre = nombreTrimmed, descripcion = desc };
                db.roles.Add(nuevoRol);
                try
                {
                    db.SaveChanges();
                    TempData["Success"] = "Rol creado con éxito.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al crear el rol: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}