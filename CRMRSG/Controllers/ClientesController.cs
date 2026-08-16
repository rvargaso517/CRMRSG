using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class ClientesController : Controller
    {
        private bool TienePermiso(string permiso)
        {
            if (Session["UsuarioId"] == null) return false;
            if (Session["RolId"] != null && (int)Session["RolId"] == 1) return true;
            if (Session["Permisos"] == null) return false;
            string perms = Session["Permisos"].ToString();
            return perms.Split(',').Contains(permiso) || perms.Split(',').Contains("Admin:Acceso");
        }

        // GET: Clientes
        public ActionResult Index(string search)
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Clientes.";
                return RedirectToAction("Index", "Dashboard");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listaClientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    listaClientes = listaClientes.Where(c =>
                        (c.nombre != null && c.nombre.ToLower().Contains(search)) ||
                        (c.empresa != null && c.empresa.ToLower().Contains(search)) ||
                        (c.correo != null && c.correo.ToLower().Contains(search)) ||
                        (c.telefono != null && c.telefono.Contains(search))
                    ).ToList();
                    ViewBag.SearchQuery = search;
                }

                return View(listaClientes);
            }
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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var clienteDetalle = db.QueryFirstOrDefault<cliente>(
                    "sp_clientes_obtener_por_id",
                    new { p_id_cliente = id.Value },
                    commandType: CommandType.StoredProcedure
                );

                if (clienteDetalle == null)
                {
                    return HttpNotFound();
                }

                clienteDetalle.contacto_cliente = db.Query<contacto_cliente>(
                    "sp_contactos_listar_por_cliente",
                    new { p_id_cliente = id.Value },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                clienteDetalle.nota_cliente = db.Query<nota_cliente>(
                    "sp_notas_listar_por_cliente",
                    new { p_id_cliente = id.Value },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(clienteDetalle);
            }
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

            if (string.IsNullOrWhiteSpace(nuevoCliente.nombre))
            {
                ModelState.AddModelError("nombre", "El nombre del contacto principal es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(nuevoCliente.correo) || !System.Text.RegularExpressions.Regex.IsMatch(nuevoCliente.correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("correo", "Debe proporcionar un correo electrónico válido.");
            }
            else
            {
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var existing = db.QueryFirstOrDefault<cliente>(
                        "SELECT * FROM clientes WHERE correo = @Correo",
                        new { Correo = nuevoCliente.correo }
                    );
                    if (existing != null)
                    {
                        ModelState.AddModelError("correo", "Ya existe un cliente registrado con este correo electrónico.");
                    }
                }
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

                using (var db = DbConnectionFactory.GetConnection())
                {
                    // Insertar cliente
                    var id_cliente = db.QuerySingle<int>(
                        "sp_clientes_insertar",
                        new {
                            p_nombre = nuevoCliente.nombre,
                            p_empresa = nuevoCliente.empresa,
                            p_telefono = nuevoCliente.telefono,
                            p_correo = nuevoCliente.correo,
                            p_direccion = nuevoCliente.direccion,
                            p_estado = nuevoCliente.estado ?? "Activo",
                            p_id_usuario = nuevoCliente.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );
                    nuevoCliente.id_cliente = id_cliente;

                    // Crear tarea de bienvenida automática
                    db.Execute(
                        "sp_tareas_insertar",
                        new {
                            p_titulo = $"Llamada de Bienvenida: {nuevoCliente.empresa}",
                            p_descripcion = $"Realizar llamada de introducción al contacto principal {nuevoCliente.nombre}.",
                            p_prioridad = "Media",
                            p_estado = "Pendiente",
                            p_fecha_limite = DateTime.Today.AddDays(2),
                            p_id_cliente = nuevoCliente.id_cliente,
                            p_id_usuario = nuevoCliente.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    // Insertar notificación
                    db.Execute(
                        "sp_notificaciones_insertar",
                        new {
                            p_mensaje = $"Cliente Creado: Se ha registrado el cliente '{nuevoCliente.nombre}' de la empresa '{nuevoCliente.empresa}'.",
                            p_id_usuario = nuevoCliente.id_usuario ?? 1,
                            p_tipo = "Cliente Creado",
                            p_id_referencia = nuevoCliente.id_cliente
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                TempData["Success"] = "Cliente registrado con éxito.";
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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var clienteEditar = db.QueryFirstOrDefault<cliente>(
                    "sp_clientes_obtener_por_id",
                    new { p_id_cliente = id.Value },
                    commandType: CommandType.StoredProcedure
                );

                if (clienteEditar == null)
                {
                    return HttpNotFound();
                }

                return View(clienteEditar);
            }
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
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var clienteDb = db.QueryFirstOrDefault<cliente>(
                        "sp_clientes_obtener_por_id",
                        new { p_id_cliente = clienteModificado.id_cliente },
                        commandType: CommandType.StoredProcedure
                    );

                    if (clienteDb != null)
                    {
                        db.Execute(
                            "sp_clientes_actualizar",
                            new {
                                p_id_cliente = clienteModificado.id_cliente,
                                p_nombre = clienteModificado.nombre,
                                p_empresa = clienteModificado.empresa,
                                p_telefono = clienteModificado.telefono,
                                p_correo = clienteModificado.correo,
                                p_direccion = clienteModificado.direccion,
                                p_estado = clienteModificado.estado,
                                p_id_usuario = clienteDb.id_usuario
                            },
                            commandType: CommandType.StoredProcedure
                        );

                        TempData["Success"] = "Cambios guardados con éxito.";
                        return RedirectToAction("Index");
                    }
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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var clienteEliminar = db.QueryFirstOrDefault<cliente>(
                    "sp_clientes_obtener_por_id",
                    new { p_id_cliente = id },
                    commandType: CommandType.StoredProcedure
                );

                if (clienteEliminar != null)
                {
                    db.Execute(
                        "sp_clientes_eliminar",
                        new { p_id_cliente = id },
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new
                    {
                        success = true,
                        message = "Cliente eliminado correctamente."
                    });
                }
            }

            return Json(new
            {
                success = false,
                message = "No se pudo encontrar el cliente."
            });
        }

        // GET: clientes/ExportarClientesCSV
        public void ExportarClientesCSV()
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                Response.Clear();
                Response.Write("No autorizado");
                Response.End();
                return;
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listaClientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

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
        }

        // GET: clientes/ExportarClientesExcel
        public void ExportarClientesExcel()
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                Response.Clear();
                Response.Write("No autorizado");
                Response.End();
                return;
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listaClientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
                sb.AppendLine("<head>");
                sb.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
                sb.AppendLine("</head>");
                sb.AppendLine("<body style=\"font-family: Calibri, Arial, sans-serif;\">");
                sb.AppendLine("  <table border=\"0\" style=\"border-collapse: collapse;\">");
                
                // Título Principal
                sb.AppendLine("    <tr>");
                sb.AppendLine("      <td colspan=\"8\" style=\"font-size: 16pt; font-weight: bold; color: #1d3557; height: 35px; vertical-align: middle;\">Reporte de Clientes - CRM RSG</td>");
                sb.AppendLine("    </tr>");
                
                // Fecha de Generación
                sb.AppendLine("    <tr>");
                sb.AppendLine($"      <td colspan=\"8\" style=\"font-size: 10pt; color: #64748b; height: 20px;\">Generado el: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</td>");
                sb.AppendLine("    </tr>");
                
                // Fila vacía de separación
                sb.AppendLine("    <tr><td colspan=\"8\" style=\"height: 15px;\"></td></tr>");

                // Encabezados de Tabla
                sb.AppendLine("    <tr style=\"background-color: #1d3557; height: 28px;\">");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: center; width: 60px;\">ID</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: left; width: 180px;\">Nombre Completo</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: left; width: 160px;\">Empresa</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: left; width: 110px;\">Teléfono</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: left; width: 220px;\">Correo Electrónico</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: left; width: 240px;\">Dirección</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: center; width: 100px;\">Estado</th>");
                sb.AppendLine("      <th style=\"background-color: #1d3557; color: #ffffff; font-weight: bold; border: 1px solid #1d3557; padding: 6px; text-align: center; width: 130px;\">Fecha Registro</th>");
                sb.AppendLine("    </tr>");

                foreach (var c in listaClientes)
                {
                    string estadoColor = (c.estado ?? "Activo").Equals("Activo", StringComparison.OrdinalIgnoreCase) ? "#16a34a" : "#dc2626";
                    sb.AppendLine("    <tr style=\"height: 24px;\">");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; text-align: center; padding: 5px;\">{c.id_cliente}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; padding: 5px;\">{HttpUtility.HtmlEncode(c.nombre ?? "N/A")}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; padding: 5px;\">{HttpUtility.HtmlEncode(c.empresa ?? "N/A")}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; padding: 5px;\">{HttpUtility.HtmlEncode(c.telefono ?? "N/A")}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; padding: 5px;\">{HttpUtility.HtmlEncode(c.correo ?? "N/A")}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; padding: 5px;\">{HttpUtility.HtmlEncode(c.direccion ?? "N/A")}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; text-align: center; font-weight: bold; color: {estadoColor}; padding: 5px;\">{HttpUtility.HtmlEncode(c.estado ?? "Activo")}</td>");
                    sb.AppendLine($"      <td style=\"border: 1px solid #cbd5e1; text-align: center; padding: 5px;\">{(c.fecha_registro.HasValue ? c.fecha_registro.Value.ToString("dd/MM/yyyy") : "N/A")}</td>");
                    sb.AppendLine("    </tr>");
                }

                sb.AppendLine("  </table>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
                byte[] archivoFinal = bom.Concat(buffer).ToArray();

                Response.Clear();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", "attachment;filename=Reporte_Clientes_CRM.xls");
                Response.Charset = "UTF-8";
                Response.ContentType = "application/vnd.ms-excel";
                Response.BinaryWrite(archivoFinal);
                Response.End();
            }
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

                using (var db = DbConnectionFactory.GetConnection())
                {
                    db.Execute(
                        "sp_contactos_insertar",
                        new {
                            p_id_cliente = id_cliente,
                            p_nombre = nombre,
                            p_apellido = (string)null,
                            p_puesto = puesto,
                            p_telefono = telefono,
                            p_correo = correo
                        },
                        commandType: CommandType.StoredProcedure
                    );

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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var usuarios = db.Query<usuario>(
                    "sp_usuarios_listar",
                    commandType: CommandType.StoredProcedure
                ).Where(u => u.estado == true).ToList();

                ViewBag.Usuarios = usuarios;
                return View();
            }
        }

        // GET: Clientes/ObtenerClientesPorAsesor
        [HttpGet]
        public JsonResult ObtenerClientesPorAsesor(int? idUsuario)
        {
            if (!TienePermiso("Clientes:Ver"))
            {
                return Json(new { success = false, message = "No autorizado" }, JsonRequestBehavior.AllowGet);
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var lista = db.Query<cliente>(
                    "sp_clientes_listar_por_usuario",
                    new { p_id_usuario = idUsuario },
                    commandType: CommandType.StoredProcedure
                ).Select(c => new {
                    id_cliente = c.id_cliente,
                    nombre = c.nombre ?? "",
                    empresa = c.empresa ?? "",
                    correo = c.correo ?? "",
                    telefono = c.telefono ?? "",
                    estado = c.estado ?? "Activo"
                }).ToList();

                return Json(new { success = true, clientes = lista }, JsonRequestBehavior.AllowGet);
            }
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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var destino = db.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_id",
                    new { p_id_usuario = idUsuarioDestino },
                    commandType: CommandType.StoredProcedure
                );

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
                    var cli = db.QueryFirstOrDefault<cliente>(
                        "sp_clientes_obtener_por_id",
                        new { p_id_cliente = idC },
                        commandType: CommandType.StoredProcedure
                    );

                    if (cli != null)
                    {
                        int? prevVal = cli.id_usuario;
                        if (prevVal != idUsuarioDestino)
                        {
                            // Actualizar cliente
                            db.Execute(
                                "sp_clientes_actualizar",
                                new {
                                    p_id_cliente = cli.id_cliente,
                                    p_nombre = cli.nombre,
                                    p_empresa = cli.empresa,
                                    p_telefono = cli.telefono,
                                    p_correo = cli.correo,
                                    p_direccion = cli.direccion,
                                    p_estado = cli.estado,
                                    p_id_usuario = idUsuarioDestino
                                },
                                commandType: CommandType.StoredProcedure
                            );

                            // Registrar en bitácora
                            db.Execute(
                                "sp_bitacora_insertar",
                                new {
                                    p_accion = "Reasignación",
                                    p_tabla_afectada = "clientes",
                                    p_id_registro_afectado = cli.id_cliente,
                                    p_valor_anterior = prevVal.HasValue ? prevVal.Value.ToString() : "NULL",
                                    p_valor_nuevo = idUsuarioDestino.ToString(),
                                    p_direccion_ip = ipAddress,
                                    p_id_usuario = currentUserId
                                },
                                commandType: CommandType.StoredProcedure
                            );
                            exitos++;
                        }
                    }
                }

                return Json(new { success = true, message = $"Se han reasignado con éxito {exitos} de {total} clientes." });
            }
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

            using (var db = DbConnectionFactory.GetConnection())
            {
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
                        usr = db.QueryFirstOrDefault<usuario>(
                            "sp_usuarios_obtener_por_id",
                            new { p_id_usuario = idU },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                    else
                    {
                        usr = db.QueryFirstOrDefault<usuario>(
                            "SELECT * FROM usuarios WHERE correo = @Correo OR CONCAT(nombre, ' ', apellido) = @Val OR nombre = @Val",
                            new { Correo = valUsuario, Val = valUsuario }
                        );
                    }

                    if (usr == null)
                    {
                        int fallbackId = currentUserId ?? 1;
                        usr = db.QueryFirstOrDefault<usuario>(
                            "sp_usuarios_obtener_por_id",
                            new { p_id_usuario = fallbackId },
                            commandType: CommandType.StoredProcedure
                        );
                    }

                    cliente cli = null;
                    if (int.TryParse(valCliente, out int idC))
                    {
                        cli = db.QueryFirstOrDefault<cliente>(
                            "sp_clientes_obtener_por_id",
                            new { p_id_cliente = idC },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                    else
                    {
                        cli = db.QueryFirstOrDefault<cliente>(
                            "SELECT * FROM clientes WHERE correo = @Val OR nombre = @Val OR empresa = @Val",
                            new { Val = valCliente }
                        );
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

                        var newId = db.QuerySingle<int>(
                            "sp_clientes_insertar",
                            new {
                                p_nombre = cli.nombre,
                                p_empresa = cli.empresa,
                                p_telefono = cli.telefono,
                                p_correo = cli.correo,
                                p_direccion = cli.direccion,
                                p_estado = cli.estado,
                                p_id_usuario = cli.id_usuario
                            },
                            commandType: CommandType.StoredProcedure
                        );
                        cli.id_cliente = newId;
                        esNuevo = true;
                    }

                    try
                    {
                        int? prevVal = cli.id_usuario;
                        if (prevVal != usr.id_usuario)
                        {
                            db.Execute(
                                "sp_clientes_actualizar",
                                new {
                                    p_id_cliente = cli.id_cliente,
                                    p_nombre = cli.nombre,
                                    p_empresa = cli.empresa,
                                    p_telefono = cli.telefono,
                                    p_correo = cli.correo,
                                    p_direccion = cli.direccion,
                                    p_estado = cli.estado,
                                    p_id_usuario = usr.id_usuario
                                },
                                commandType: CommandType.StoredProcedure
                            );

                            db.Execute(
                                "sp_bitacora_insertar",
                                new {
                                    p_accion = esNuevo ? "Importación y Asignación" : "Reasignación Masiva Archivo",
                                    p_tabla_afectada = "clientes",
                                    p_id_registro_afectado = cli.id_cliente,
                                    p_valor_anterior = prevVal.HasValue ? prevVal.Value.ToString() : "NULL",
                                    p_valor_nuevo = usr.id_usuario.ToString(),
                                    p_direccion_ip = ipAddress,
                                    p_id_usuario = currentUserId
                                },
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        if (indexContactoNombre != -1 && indexContactoNombre < fila.Length && !string.IsNullOrWhiteSpace(fila[indexContactoNombre]))
                        {
                            string sNombre = fila[indexContactoNombre];
                            string sCorreo = (indexContactoCorreo != -1 && indexContactoCorreo < fila.Length) ? fila[indexContactoCorreo] : null;
                            string sTelefono = (indexContactoTelefono != -1 && indexContactoTelefono < fila.Length) ? fila[indexContactoTelefono] : null;
                            string sPuesto = (indexContactoPuesto != -1 && indexContactoPuesto < fila.Length) ? fila[indexContactoPuesto] : null;

                            db.Execute(
                                "sp_contactos_insertar",
                                new {
                                    p_id_cliente = cli.id_cliente,
                                    p_nombre = sNombre,
                                    p_apellido = (string)null,
                                    p_puesto = sPuesto,
                                    p_telefono = sTelefono,
                                    p_correo = sCorreo
                                },
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        if (indexTarea != -1 && indexTarea < fila.Length && !string.IsNullOrWhiteSpace(fila[indexTarea]))
                        {
                            db.Execute(
                                "sp_tareas_insertar",
                                new {
                                    p_titulo = fila[indexTarea],
                                    p_descripcion = "Creada automáticamente mediante importación masiva.",
                                    p_prioridad = "Media",
                                    p_estado = "Pendiente",
                                    p_fecha_limite = DateTime.Today.AddDays(7),
                                    p_id_cliente = cli.id_cliente,
                                    p_id_usuario = usr.id_usuario
                                },
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        if (indexOportunidad != -1 && indexOportunidad < fila.Length && !string.IsNullOrWhiteSpace(fila[indexOportunidad]))
                        {
                            decimal valor = 0;
                            if (indexOportunidadValor != -1 && indexOportunidadValor < fila.Length)
                            {
                                decimal.TryParse(fila[indexOportunidadValor], out valor);
                            }
                            
                            db.Execute(
                                "sp_oportunidades_insertar",
                                new {
                                    p_nombre = fila[indexOportunidad],
                                    p_descripcion = "Creada automáticamente mediante importación masiva.",
                                    p_etapa = "Nuevo",
                                    p_probabilidad = (decimal)10.00,
                                    p_valor_estimado = valor,
                                    p_estado = "Activo",
                                    p_id_cliente = cli.id_cliente,
                                    p_id_usuario = usr.id_usuario
                                },
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        exitos++;
                    }
                    catch (Exception ex)
                    {
                        errores++;
                        detallesErrores.Add($"Fila {i + 1}: Error al registrar. {ex.Message}");
                    }
                }
            }

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
