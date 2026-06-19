using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
// Usamos el namespace correcto para la entidad en singular
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ClientesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: clientes
        public ActionResult Index()
        {
            // Trae todos los clientes de la base de datos (Colección db.clientes)
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
        public ActionResult Crear(cliente nuevoclientes)
        {
            if (ModelState.IsValid)
            {
                nuevoclientes.fecha_registro = DateTime.Now;
                // Asignamos el id del usuario logueado usando la sesión
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
            // Validamos que el ID no venga vacío para evitar la pantalla roja
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var clientesEditar = db.clientes.Find(id);

            if (clientesEditar == null)
            {
                return HttpNotFound();
            }

            return View(clientesEditar);
        }

        // POST: clientes/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(cliente clientesModificado)
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

        // GET: clientes/ExportarClientesCSV (HU-034)
        public void ExportarClientesCSV()
        {
            var listaClientes = db.clientes.ToList();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("ID Cliente;Nombre Completo;Empresa;Telefono;Correo;Direccion;Estado;Fecha Registro");

            foreach (var c in listaClientes)
            {
                sb.AppendLine(string.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                    c.id_cliente,
                    c.nombre ?? "N/A",
                    c.empresa ?? "N/A",
                    c.telefono ?? "N/A",
                    c.correo ?? "N/A",
                    c.direccion ?? "N/A",
                    c.estado ?? "Activo",
                    c.fecha_registro.HasValue ? c.fecha_registro.Value.ToString("dd/MM/yyyy") : "N/A"
                ));
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] archivoFinal = bom.Concat(buffer).ToArray();

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=Reporte_Clientes_CRM.csv");
            Response.Charset = "UTF-8";
            Response.ContentType = "text/csv";
            Response.BinaryWrite(archivoFinal);
            Response.End();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    } // Cierre de la clase ClientesController
} // Cierre del namespace CRMRSG.Controllers
        // POST: Clientes/AgregarContacto
        [HttpPost]
        public JsonResult AgregarContacto(int id_cliente, string nombre, string telefono, string correo, string puesto)
        {
            try
            {
                // Validar que el nombre no venga vacío
                if (string.IsNullOrEmpty(nombre))
                {
                    return Json(new { success = false, message = "El nombre del contacto es obligatorio, mae." });
                }

                using (CRM_RSGEntities db = new CRM_RSGEntities())
                {
                    // Creamos el nuevo objeto de la tabla que creaste con el script
                    var nuevoContacto = new contacto_cliente
                    {
                        id_cliente = id_cliente,
                        nombre = nombre,
                        telefono = telefono,
                        correo = correo,
                        puesto = puesto
                    };

                    db.contacto_cliente.Add(nuevoContacto);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Contacto secundario agregado con éxito." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error en el servidor: " + ex.Message });
            }
        }
    }
}
