using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ClientesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        private bool TienePermiso(string permiso)
        {
            if (Session["UsuarioId"] == null) return false;
            if (Session["RolId"] != null && (int)Session["RolId"] == 1) return true;
            if (Session["Permisos"] == null) return false;
            string perms = Session["Permisos"].ToString();
            return perms.Split(',').Contains(permiso) || perms.Split(',').Contains("Admin:Acceso");
        }

        // GET: Clientes
        public ActionResult Index()
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Clientes.";
                return RedirectToAction("Index", "Dashboard");
            }

            var listaClientes = db.clientes.ToList();
            return View(listaClientes);
        }

        // GET: Clientes/Detalle/5
        public ActionResult Detalle(int? id)
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Clientes.";
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var clienteDetalle = db.clientes
                .Include(c => c.contacto_cliente)
                .Include(c => c.nota_cliente)
                .FirstOrDefault(c => c.id_cliente == id);

            if (clienteDetalle == null)
            {
                return HttpNotFound();
            }

            return View(clienteDetalle);
        }

        // GET: Clientes/Crear
        public ActionResult Crear()
        {
            if (!TienePermiso("Clientes:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Clientes.";
                return RedirectToAction("Index");
            }
            return View();
        }

        // POST: Clientes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(cliente nuevoCliente)
        {
            if (!TienePermiso("Clientes:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Clientes.";
                return RedirectToAction("Index");
            }

            // HU-022: Validación Automática de Datos de Clientes
            if (string.IsNullOrWhiteSpace(nuevoCliente.nombre))
            {
                ModelState.AddModelError("nombre", "El nombre del contacto principal es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(nuevoCliente.correo) || !System.Text.RegularExpressions.Regex.IsMatch(nuevoCliente.correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("correo", "Debe proporcionar un correo electrónico válido.");
            }
            else if (db.clientes.Any(c => c.correo == nuevoCliente.correo))
            {
                ModelState.AddModelError("correo", "Ya existe un cliente registrado con este correo electrónico.");
            }

            if (!string.IsNullOrWhiteSpace(nuevoCliente.telefono) && !System.Text.RegularExpressions.Regex.IsMatch(nuevoCliente.telefono, @"^\+?[0-9\s\-]{8,15}$"))
            {
                ModelState.AddModelError("telefono", "El teléfono debe contener entre 8 y 15 dígitos (solo números, espacios, '+' o guiones).");
            }

            if (ModelState.IsValid)
            {
                nuevoCliente.fecha_registro = DateTime.Now;
                nuevoCliente.id_usuario = Session["UsuarioId"] != null
                    ? (int)Session["UsuarioId"]
                    : 1;

                db.clientes.Add(nuevoCliente);
                db.SaveChanges();

                // HU-030: Automatización de Tareas - Crear tarea de bienvenida automática al crear un cliente
                var tareaAuto = new tarea
                {
                    titulo = $"Llamada de Bienvenida: {nuevoCliente.empresa}",
                    descripcion = $"Realizar llamada de introducción al contacto principal {nuevoCliente.nombre}.",
                    prioridad = "Media",
                    estado = "Pendiente",
                    fecha_limite = DateTime.Today.AddDays(2),
                    id_cliente = nuevoCliente.id_cliente,
                    id_usuario = nuevoCliente.id_usuario
                };
                db.tareas.Add(tareaAuto);
                db.SaveChanges();

                var notiCliente = new notificacione
                {
                    mensaje = $"Cliente Creado: Se ha registrado el cliente '{nuevoCliente.nombre}' de la empresa '{nuevoCliente.empresa}'.",
                    fecha = DateTime.Now,
                    leida = false,
                    id_usuario = nuevoCliente.id_usuario ?? 1,
                    tipo = "Cliente Creado",
                    id_referencia = nuevoCliente.id_cliente
                };
                db.notificaciones.Add(notiCliente);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(nuevoCliente);
        }

        // GET: Clientes/Editar/5
        public ActionResult Editar(int? id)
        {
            if (!TienePermiso("Clientes:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para editar Clientes.";
                return RedirectToAction("Index");
            }

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
            if (!TienePermiso("Clientes:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para editar Clientes.";
                return RedirectToAction("Index");
            }

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
            if (!TienePermiso("Clientes:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

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

        // GET: clientes/ExportarClientesCSV (HU-034)
        public void ExportarClientesCSV()
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                Response.Clear();
                Response.Write("No autorizado");
                Response.End();
                return;
            }

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

        // POST: Clientes/AgregarContacto
        [HttpPost]
        public JsonResult AgregarContacto(int id_cliente, string nombre, string telefono, string correo, string puesto)
        {
            try
            {
                if (!TienePermiso("Clientes:Gestionar"))
                {
                    return Json(new { success = false, message = "No autorizado" });
                }

                if (string.IsNullOrEmpty(nombre))
                {
                    return Json(new { success = false, message = "El nombre del contacto es obligatorio, mae." });
                }

                using (CRM_RSGEntities db = new CRM_RSGEntities())
                {
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
