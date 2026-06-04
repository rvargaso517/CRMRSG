using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
// Asegurate de que este usando apunte a tu carpeta de EntityFramework
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ClientesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: clientes
        public ActionResult Index()
        {
            // Trae todos los clientes de la base de datos
            var listaclientes = db.clientes.ToList();
            return View(listaclientes);
        }

        // GET: clientes/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: clientes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(clientes nuevoclientes)
        {
            if (ModelState.IsValid)
            {
                nuevoclientes.fecha_registro = DateTime.Now;
                // Asignamos el id del usuario logueado usando la sesión que hackeamos
                nuevoclientes.id_usuario = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;

                db.clientes.Add(nuevoclientes);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nuevoclientes);
        }

        // GET: clientes/Editar/5
        public ActionResult Editar(int? id)
        {
            // 1. Validamos que el ID no venga vacío para evitar la pantalla roja
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                // 2. Corregido a "db.clientes" en singular para que calce con tu Entity Framework
                var clientesEditar = db.clientes.Find(id);

                if (clientesEditar == null)
                {
                    return HttpNotFound();
                }

                return View(clientesEditar);
            }
        }

        // POST: clientes/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(clientes clientesModificado)
        {
            if (ModelState.IsValid)
            {
                var clientesDb = db.clientes.Find(clientesModificado.id_cliente);
                if (clientesDb != null)
                {
                    clientesDb.nombre = clientesModificado.nombre;
                    clientesDb.empresa = clientesModificado.empresa;
                    clientesDb.telefono = clientesModificado.telefono;
                    clientesDb.correo = clientesModificado.correo;
                    clientesDb.direccion = clientesModificado.direccion;
                    clientesDb.estado = clientesModificado.estado;

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(clientesModificado);
        }

        // POST: clientes/Eliminar/5
        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            var clientesEliminar = db.clientes.Find(id);
            if (clientesEliminar != null)
            {
                db.clientes.Remove(clientesEliminar);
                db.SaveChanges();
                return Json(new { success = true, message = "clientes eliminado correctamente." });
            }
            return Json(new { success = false, message = "No se pudo encontrar el clientes." });
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