using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ClientesController : Controller
    {
        private crm_rsgEntities db = new crm_rsgEntities();

    public ActionResult Index()
        {
            var clientes = db.clientes.ToList();
            return View(clientes);
        }

        public ActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Crear(clientes cliente)
        {
            try
            {
                if (db.clientes.Any(x => x.correo == cliente.correo))
                {
                    ViewBag.Error = "Ya existe un cliente con ese correo.";
                    return View(cliente);
                }

                cliente.fecha_registro = DateTime.Now;

                if (string.IsNullOrEmpty(cliente.estado))
                    cliente.estado = "Activo";

                db.clientes.Add(cliente);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View(cliente);
            }
        }

        public ActionResult Editar(int id)
        {
            var cliente = db.clientes.Find(id);

            if (cliente == null)
                return HttpNotFound();

            return View(cliente);
        }

        [HttpPost]
        public ActionResult Editar(clientes cliente)
        {
            var clienteDB = db.clientes.Find(cliente.id_cliente);

            if (clienteDB == null)
                return HttpNotFound();

            clienteDB.nombre = cliente.nombre;
            clienteDB.empresa = cliente.empresa;
            clienteDB.telefono = cliente.telefono;
            clienteDB.correo = cliente.correo;
            clienteDB.direccion = cliente.direccion;
            clienteDB.estado = cliente.estado;

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Detalle(int id)
        {
            var cliente = db.clientes.Find(id);

            if (cliente == null)
                return HttpNotFound();

            return View(cliente);
        }

        public ActionResult Eliminar(int id)
        {
            var cliente = db.clientes.Find(id);

            if (cliente != null)
            {
                db.clientes.Remove(cliente);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }


}

