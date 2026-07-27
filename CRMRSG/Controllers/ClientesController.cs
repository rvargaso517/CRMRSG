using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
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

            var listaClientes = db.clientes.OrderByDescending(c => c.id_cliente).ToList();
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

        // GET: Clientes/ReasignacionMasiva
        public ActionResult ReasignacionMasiva()
        {
            if (!TienePermiso("Clientes:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para reasignar la cartera de clientes.";
                return RedirectToAction("Index");
            }

            ViewBag.Usuarios = db.usuarios.Where(u => u.estado == true).ToList();
            return View();
        }

        // GET: Clientes/ObtenerClientesPorAsesor
        [HttpGet]
        public JsonResult ObtenerClientesPorAsesor(int? idUsuario)
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                return Json(new { success = false, message = "No autorizado" }, JsonRequestBehavior.AllowGet);
            }

            var clientesQuery = db.clientes.AsQueryable();
            if (idUsuario.HasValue && idUsuario.Value > 0)
            {
                clientesQuery = clientesQuery.Where(c => c.id_usuario == idUsuario.Value);
            }
            else
            {
                clientesQuery = clientesQuery.Where(c => c.id_usuario == null);
            }

            var lista = clientesQuery.ToList().Select(c => new {
                id_cliente = c.id_cliente,
                nombre = c.nombre ?? "",
                empresa = c.empresa ?? "",
                correo = c.correo ?? "",
                telefono = c.telefono ?? "",
                estado = c.estado ?? "Activo"
            }).ToList();

            return Json(new { success = true, clientes = lista }, JsonRequestBehavior.AllowGet);
        }

        // POST: Clientes/ReasignarMasiva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReasignarMasiva(int? idUsuarioOrigen, int idUsuarioDestino, List<int> idsClientes)
        {
            if (!TienePermiso("Clientes:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            if (idsClientes == null || !idsClientes.Any())
            {
                return Json(new { success = false, message = "Debe seleccionar al menos un cliente." });
            }

            var destino = db.usuarios.Find(idUsuarioDestino);
            if (destino == null)
            {
                return Json(new { success = false, message = "El asesor de destino no existe." });
            }

            int exitos = 0;
            int total = idsClientes.Count;
            int? currentUserId = Session["UsuarioId"] != null ? (int?)Session["UsuarioId"] : null;
            string ipAddress = Request.UserHostAddress;

            foreach (var idC in idsClientes)
            {
                var cli = db.clientes.Find(idC);
                if (cli != null)
                {
                    int? prevVal = cli.id_usuario;
                    if (prevVal != idUsuarioDestino)
                    {
                        cli.id_usuario = idUsuarioDestino;
                        
                        // Bitácora
                        var log = new bitacora
                        {
                            accion = "Reasignación",
                            tabla_afectada = "clientes",
                            id_registro_afectado = cli.id_cliente,
                            valor_anterior = prevVal.HasValue ? prevVal.Value.ToString() : "NULL",
                            valor_nuevo = idUsuarioDestino.ToString(),
                            fecha_hora = DateTime.Now,
                            direccion_ip = ipAddress,
                            id_usuario = currentUserId
                        };
                        db.bitacoras.Add(log);
                        exitos++;
                    }
                }
            }

            db.SaveChanges();

            return Json(new { success = true, message = $"Se han reasignado con éxito {exitos} de {total} clientes." });
        }

        // POST: Clientes/ProcesarReasignacionArchivo
        [HttpPost]
        public JsonResult ProcesarReasignacionArchivo(HttpPostedFileBase archivo, bool? forzarProceso)
        {
            if (!TienePermiso("Clientes:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            if (archivo == null || archivo.ContentLength == 0)
            {
                return Json(new { success = false, message = "Por favor, seleccione un archivo válido." });
            }

            List<string> lineas = new List<string>();
            using (var reader = new System.IO.StreamReader(archivo.InputStream))
            {
                string linea;
                while ((linea = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineas.Add(linea);
                    }
                }
            }

            if (lineas.Count < 2)
            {
                return Json(new { success = false, message = "El archivo debe tener al menos una cabecera y una fila de datos." });
            }

            string cabecera = lineas[0];
            char delimitador = ';';
            if (cabecera.Contains(',')) delimitador = ',';
            else if (cabecera.Contains('\t')) delimitador = '\t';

            string[] columnas = cabecera.Split(delimitador).Select(c => c.Trim().ToLower()).ToArray();

            int indexCliente = -1;
            int indexUsuario = -1;
            int indexNombre = -1;
            int indexEmpresa = -1;
            int indexCorreo = -1;
            int indexTelefono = -1;
            int indexDireccion = -1;

            int indexContactoNombre = -1;
            int indexContactoCorreo = -1;
            int indexContactoTelefono = -1;
            int indexContactoPuesto = -1;
            int indexTarea = -1;
            int indexOportunidad = -1;
            int indexOportunidadValor = -1;

            for (int i = 0; i < columnas.Length; i++)
            {
                string col = columnas[i];
                if (col.Contains("id_cliente") || col.Contains("id cliente") || col.Contains("idcliente")) indexCliente = i;
                else if (col.Contains("id_usuario") || col.Contains("id usuario") || col.Contains("idusuario") || col.Contains("vendedor_id")) indexUsuario = i;
                else if (col.Contains("empresa") || col.Contains("compañia") || col.Contains("compania")) indexEmpresa = i;
                else if (col.Contains("correo_cliente") || col.Contains("cliente_correo") || col.Contains("cliente_email")) indexCorreo = i;
                else if (col.Contains("telefono_cliente") || col.Contains("cliente_telefono")) indexTelefono = i;
                else if (col.Contains("direccion")) indexDireccion = i;
                else if (col.Contains("contacto_nombre") || col.Contains("contacto_secundario") || col.Contains("nombre_contacto")) indexContactoNombre = i;
                else if (col.Contains("contacto_correo") || col.Contains("correo_contacto")) indexContactoCorreo = i;
                else if (col.Contains("contacto_telefono") || col.Contains("telefono_contacto")) indexContactoTelefono = i;
                else if (col.Contains("contacto_puesto") || col.Contains("puesto_contacto") || col.Contains("puesto")) indexContactoPuesto = i;
                else if (col.Contains("tarea") || col.Contains("actividad") || col.Contains("titulo_tarea")) indexTarea = i;
                else if (col.Contains("oportunidad") || col.Contains("nombre_oportunidad")) indexOportunidad = i;
                else if (col.Contains("valor") || col.Contains("monto") || col.Contains("precio")) indexOportunidadValor = i;
                else if (col.Contains("nombre") || col.Contains("cliente") || col.Contains("contacto_principal"))
                {
                    if (indexNombre == -1) indexNombre = i;
                }
                else if (col.Contains("correo") || col.Contains("email") || col.Contains("mail"))
                {
                    if (col.Contains("usuario") || col.Contains("vendedor") || col.Contains("asesor"))
                    {
                        if (indexUsuario == -1) indexUsuario = i;
                    }
                    else
                    {
                        if (indexCorreo == -1) indexCorreo = i;
                    }
                }
                else if (col.Contains("usuario") || col.Contains("vendedor") || col.Contains("asesor"))
                {
                    if (indexUsuario == -1) indexUsuario = i;
                }
                else if (col.Contains("telefono") || col.Contains("tel"))
                {
                    if (indexTelefono == -1) indexTelefono = i;
                }
            }

            bool formatoValido = (indexCliente != -1 || indexNombre != -1 || indexEmpresa != -1) && indexUsuario != -1;

            if (!formatoValido && (forzarProceso == null || forzarProceso == false))
            {
                return Json(new
                {
                    success = false,
                    needsConfirmation = true,
                    message = "El archivo no tiene el formato de cabeceras de reasignación esperado. ¿Desea intentar procesarlo asociando las columnas físicamente?",
                    columnasDetectadas = columnas,
                    camposNecesarios = new string[] { "cliente (o nombre/empresa)", "usuario (o asesor/vendedor)" }
                });
            }

            if (indexCliente == -1)
            {
                if (indexNombre != -1) indexCliente = indexNombre;
                else if (indexEmpresa != -1) indexCliente = indexEmpresa;
                else indexCliente = 0;
            }
            if (indexUsuario == -1) indexUsuario = columnas.Length > 1 ? 1 : 0;

            int exitos = 0;
            int errores = 0;
            List<string> detallesErrores = new List<string>();
            int? currentUserId = Session["UsuarioId"] != null ? (int?)Session["UsuarioId"] : null;
            string ipAddress = Request.UserHostAddress;

            for (int i = 1; i < lineas.Count; i++)
            {
                string[] fila = lineas[i].Split(delimitador).Select(f => f.Trim()).ToArray();
                if (fila.Length <= Math.Max(indexCliente, indexUsuario))
                {
                    errores++;
                    detallesErrores.Add($"Fila {i + 1}: Columnas insuficientes.");
                    continue;
                }

                string valCliente = fila[indexCliente];
                string valUsuario = fila[indexUsuario];

                if (string.IsNullOrWhiteSpace(valCliente) || string.IsNullOrWhiteSpace(valUsuario))
                {
                    errores++;
                    detallesErrores.Add($"Fila {i + 1}: Datos del cliente o asesor vacíos.");
                    continue;
                }

                usuario usr = null;
                if (int.TryParse(valUsuario, out int idU))
                {
                    usr = db.usuarios.Find(idU);
                }
                else
                {
                    usr = db.usuarios.FirstOrDefault(u => u.correo == valUsuario || (u.nombre + " " + u.apellido) == valUsuario || u.nombre == valUsuario);
                }

                if (usr == null)
                {
                    int fallbackId = currentUserId ?? 1;
                    usr = db.usuarios.Find(fallbackId);
                }

                cliente cli = null;
                if (int.TryParse(valCliente, out int idC))
                {
                    cli = db.clientes.Find(idC);
                }
                else
                {
                    cli = db.clientes.FirstOrDefault(c => c.correo == valCliente || c.nombre == valCliente || c.empresa == valCliente);
                }

                bool esNuevo = false;
                if (cli == null)
                {
                    cli = new cliente
                    {
                        nombre = valCliente,
                        empresa = valCliente,
                        estado = "Activo",
                        fecha_registro = DateTime.Now,
                        id_usuario = usr.id_usuario
                    };

                    if (indexNombre != -1 && indexNombre < fila.Length && !string.IsNullOrWhiteSpace(fila[indexNombre])) cli.nombre = fila[indexNombre];
                    if (indexEmpresa != -1 && indexEmpresa < fila.Length && !string.IsNullOrWhiteSpace(fila[indexEmpresa])) cli.empresa = fila[indexEmpresa];
                    if (indexCorreo != -1 && indexCorreo < fila.Length && !string.IsNullOrWhiteSpace(fila[indexCorreo])) cli.correo = fila[indexCorreo];
                    if (indexTelefono != -1 && indexTelefono < fila.Length && !string.IsNullOrWhiteSpace(fila[indexTelefono])) cli.telefono = fila[indexTelefono];
                    if (indexDireccion != -1 && indexDireccion < fila.Length && !string.IsNullOrWhiteSpace(fila[indexDireccion])) cli.direccion = fila[indexDireccion];

                    db.clientes.Add(cli);
                    esNuevo = true;
                }

                try
                {
                    int? prevVal = cli.id_usuario;
                    if (prevVal != usr.id_usuario)
                    {
                        cli.id_usuario = usr.id_usuario;

                        var log = new bitacora
                        {
                            accion = esNuevo ? "Importación y Asignación" : "Reasignación Masiva Archivo",
                            tabla_afectada = "clientes",
                            id_registro_afectado = cli.id_cliente,
                            valor_anterior = prevVal.HasValue ? prevVal.Value.ToString() : "NULL",
                            valor_nuevo = usr.id_usuario.ToString(),
                            fecha_hora = DateTime.Now,
                            direccion_ip = ipAddress,
                            id_usuario = currentUserId
                        };
                        db.bitacoras.Add(log);
                    }

                    db.SaveChanges();

                    if (indexContactoNombre != -1 && indexContactoNombre < fila.Length && !string.IsNullOrWhiteSpace(fila[indexContactoNombre]))
                    {
                        var secContacto = new contacto_cliente
                        {
                            id_cliente = cli.id_cliente,
                            nombre = fila[indexContactoNombre]
                        };
                        if (indexContactoCorreo != -1 && indexContactoCorreo < fila.Length) secContacto.correo = fila[indexContactoCorreo];
                        if (indexContactoTelefono != -1 && indexContactoTelefono < fila.Length) secContacto.telefono = fila[indexContactoTelefono];
                        if (indexContactoPuesto != -1 && indexContactoPuesto < fila.Length) secContacto.puesto = fila[indexContactoPuesto];

                        db.contacto_cliente.Add(secContacto);
                    }

                    if (indexTarea != -1 && indexTarea < fila.Length && !string.IsNullOrWhiteSpace(fila[indexTarea]))
                    {
                        var nuevaTarea = new tarea
                        {
                            id_cliente = cli.id_cliente,
                            titulo = fila[indexTarea],
                            descripcion = "Creada automáticamente mediante importación masiva.",
                            prioridad = "Media",
                            estado = "Pendiente",
                            fecha_limite = DateTime.Today.AddDays(7),
                            id_usuario = usr.id_usuario
                        };
                        db.tareas.Add(nuevaTarea);
                    }

                    if (indexOportunidad != -1 && indexOportunidad < fila.Length && !string.IsNullOrWhiteSpace(fila[indexOportunidad]))
                    {
                        decimal valor = 0;
                        if (indexOportunidadValor != -1 && indexOportunidadValor < fila.Length)
                        {
                            decimal.TryParse(fila[indexOportunidadValor], out valor);
                        }
                        var nuevaOp = new oportunidade
                        {
                            id_cliente = cli.id_cliente,
                            nombre = fila[indexOportunidad],
                            valor_estimado = valor,
                            etapa = "Nuevo",
                            estado = "Activo",
                            fecha_creacion = DateTime.Now,
                            id_usuario = usr.id_usuario
                        };
                        db.oportunidades.Add(nuevaOp);
                    }

                    db.SaveChanges();
                    exitos++;
                }
                catch (Exception ex)
                {
                    errores++;
                    detallesErrores.Add($"Fila {i + 1}: Error al registrar. {ex.Message}");
                }
            }

            db.SaveChanges();

            return Json(new
            {
                success = true,
                exitos = exitos,
                errores = errores,
                detallesErrores = detallesErrores,
                message = $"Procesamiento completo. Éxitos: {exitos}, Errores: {errores}."
            });
        }
    }
}
