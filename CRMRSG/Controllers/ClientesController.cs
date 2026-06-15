using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ClientesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Clientes
        public ActionResult Index()
        {
            var listaClientes = db.clientes.ToList();
            return View(listaClientes);
        }

        // GET: Clientes/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: Clientes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(cliente nuevoCliente)
        {
            if (ModelState.IsValid)
            {
                nuevoCliente.fecha_registro = DateTime.Now;
                nuevoCliente.id_usuario = Session["UsuarioId"] != null
                    ? (int)Session["UsuarioId"]
                    : 1;

                db.clientes.Add(nuevoCliente);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(nuevoCliente);
        }

        // GET: Clientes/Editar/5
        public ActionResult Editar(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var clienteEditar = db.clientes.Find(id);

            if (clienteEditar == null)
            {
                return HttpNotFound();
            }

            return View(clienteEditar);
        }

        // POST: Clientes/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(cliente clienteModificado)
        {
            if (ModelState.IsValid)
            {
                var clienteDb = db.clientes.Find(clienteModificado.id_cliente);

                if (clienteDb != null)
                {
                    clienteDb.nombre = clienteModificado.nombre;
                    clienteDb.empresa = clienteModificado.empresa;
                    clienteDb.telefono = clienteModificado.telefono;
                    clienteDb.correo = clienteModificado.correo;
                    clienteDb.direccion = clienteModificado.direccion;
                    clienteDb.estado = clienteModificado.estado;

                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(clienteModificado);
        }

        // POST: Clientes/Eliminar/5
        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            var clienteEliminar = db.clientes.Find(id);

            if (clienteEliminar != null)
            {
                db.clientes.Remove(clienteEliminar);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cliente eliminado correctamente."
                });
            }

            return Json(new
            {
                success = false,
                message = "No se pudo encontrar el cliente."
            });
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