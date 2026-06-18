using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework; 

namespace CRMRSG.Controllers
{
    public class TareasController : Controller
    {
        // GET: Tareas/Listado
        public ActionResult Index()
        {
            using (crm_rsgEntities db = new crm_rsgEntities())
            {
                
                var tareas = db.tarea_cliente.Include("cliente").ToList();
                return View(tareas);
            }
        }

        // GET: Tareas/Crear
        public ActionResult Crear()
        {
            using (crm_rsgEntities db = new crm_rsgEntities())
            {
               
                ViewBag.ClientesList = new SelectList(db.cliente.Where(c => c.estado == "Activo").ToList(), "id_cliente", "empresa");
                return View();
            }
        }

        // POST: Tareas/Crear
        [HttpPost]
        [AntiForgeryToken]
        public ActionResult Crear(tarea_cliente nuevaTarea)
        {
            using (crm_rsgEntities db = new crm_rsgEntities())
            {
                

                if (nuevaTarea.ganancia < 0)
                {
                    ModelState.AddModelError("ganancia", "La ganancia del trabajo no puede ser menor a cero, mae.");
                }

                
                if (nuevaTarea.fecha_fin != null && nuevaTarea.fecha_fin < nuevaTarea.fecha_inicio)
                {
                    ModelState.AddModelError("fecha_fin", "La fecha de finalización no puede ser antes de la fecha de inicio.");
                }

                
                if (!ModelState.IsValid)
                {
                    ViewBag.ClientesList = new SelectList(db.cliente.Where(c => c.estado == "Activo").ToList(), "id_cliente", "empresa");
                    return View(nuevaTarea);
                }

                try
                {
                    db.tarea_cliente.Add(nuevaTarea);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar la tarea: " + ex.Message);
                    ViewBag.ClientesList = new SelectList(db.cliente.Where(c => c.estado == "Activo").ToList(), "id_cliente", "empresa");
                    return View(nuevaTarea);
                }
            }
        }
    }
}