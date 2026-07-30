using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class OportunidadesController : Controller
    {
        private bool TienePermiso(string permiso)
        {
            if (Session["UsuarioId"] == null) return false;
            if (Session["RolId"] != null && (int)Session["RolId"] == 1) return true;
            if (Session["Permisos"] == null) return false;
            string perms = Session["Permisos"].ToString();
            return perms.Split(',').Contains(permiso) || perms.Split(',').Contains("Admin:Acceso");
        }

        // GET: Oportunidades
        public ActionResult Index()
        {
            if (!TienePermiso("Oportunidades:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Oportunidades.";
                return RedirectToAction("Index", "Dashboard");
            }

            int usuarioId = (int)Session["UsuarioId"];
            int rolId = (int)Session["RolId"];

            using (var db = DbConnectionFactory.GetConnection())
            {
                var lista = db.Query<oportunidade, cliente, usuario, oportunidade>(
                    "sp_oportunidades_listar_con_relaciones",
                    (op, cl, usr) => {
                        op.cliente = cl;
                        op.usuario = usr;
                        return op;
                    },
                    splitOn: "id_cliente,id_usuario",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (rolId != 1) // Si no es admin, filtrar por usuario
                {
                    lista = lista.Where(o => o.id_usuario == usuarioId).ToList();
                }

                return View(lista);
            }
        }

        // GET: Oportunidades/Kanban
        public ActionResult Kanban()
        {
            if (!TienePermiso("Oportunidades:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver Oportunidades.";
                return RedirectToAction("Index", "Dashboard");
            }

            int usuarioId = (int)Session["UsuarioId"];
            int rolId = (int)Session["RolId"];

            using (var db = DbConnectionFactory.GetConnection())
            {
                var lista = db.Query<oportunidade, cliente, usuario, oportunidade>(
                    "sp_oportunidades_listar_con_relaciones",
                    (op, cl, usr) => {
                        op.cliente = cl;
                        op.usuario = usr;
                        return op;
                    },
                    splitOn: "id_cliente,id_usuario",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (rolId != 1)
                {
                    lista = lista.Where(o => o.id_usuario == usuarioId).ToList();
                }

                return View(lista);
            }
        }

        // GET: Oportunidades/Crear
        public ActionResult Crear()
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Oportunidades.";
                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Clientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();
            }
            return View();
        }

        // POST: Oportunidades/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(oportunidade op, string fechaClose)
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Oportunidades.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                op.id_usuario = (int)Session["UsuarioId"];
                op.estado = "Activo";
                if (!string.IsNullOrEmpty(fechaClose))
                {
                    op.fecha_creacion = DateTime.Parse(fechaClose);
                }
                else
                {
                    op.fecha_creacion = DateTime.Now;
                }

                op.probabilidad = GetProbabilidadPorEtapa(op.etapa);

                using (var db = DbConnectionFactory.GetConnection())
                {
                    var id_op = db.QuerySingle<int>(
                        "sp_oportunidades_insertar",
                        new
                        {
                            p_nombre = op.nombre,
                            p_descripcion = op.descripcion,
                            p_etapa = op.etapa,
                            p_probabilidad = op.probabilidad,
                            p_valor_estimado = op.valor_estimado ?? 0,
                            p_estado = op.estado,
                            p_id_cliente = op.id_cliente,
                            p_id_usuario = op.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );
                    op.id_oportunidad = id_op;

                    db.Execute(
                        "sp_notificaciones_insertar",
                        new
                        {
                            p_mensaje = $"Nueva Oportunidad: Se ha creado la oportunidad '{op.nombre}' con probabilidad del {op.probabilidad}%.",
                            p_id_usuario = op.id_usuario ?? 1,
                            p_tipo = "Oportunidad Creada",
                            p_id_referencia = op.id_oportunidad
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Clientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();
            }
            return View(op);
        }

        // GET: Oportunidades/Editar/5
        public ActionResult Editar(int id)
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para editar Oportunidades.";
                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var op = db.QueryFirstOrDefault<oportunidade>(
                    "sp_oportunidades_obtener_por_id",
                    new { p_id_oportunidad = id },
                    commandType: CommandType.StoredProcedure
                );

                if (op == null) return HttpNotFound();

                int rolId = (int)Session["RolId"];
                if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
                {
                    return RedirectToAction("Index");
                }

                ViewBag.Clientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(op);
            }
        }

        // POST: Oportunidades/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(oportunidade op, string fechaClose)
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para editar Oportunidades.";
                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var opDb = db.QueryFirstOrDefault<oportunidade>(
                    "sp_oportunidades_obtener_por_id",
                    new { p_id_oportunidad = op.id_oportunidad },
                    commandType: CommandType.StoredProcedure
                );

                if (opDb == null) return HttpNotFound();

                int rolId = (int)Session["RolId"];
                if (rolId != 1 && opDb.id_usuario != (int)Session["UsuarioId"])
                {
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    bool esNuevaPropuesta = (op.etapa == "Propuesta" && opDb.etapa != "Propuesta");

                    DateTime fechaCreacion = opDb.fecha_creacion ?? DateTime.Now;
                    if (!string.IsNullOrEmpty(fechaClose))
                    {
                        fechaCreacion = DateTime.Parse(fechaClose);
                    }

                    var prob = GetProbabilidadPorEtapa(op.etapa);

                    db.Execute(
                        "sp_oportunidades_actualizar",
                        new
                        {
                            p_id_oportunidad = op.id_oportunidad,
                            p_nombre = op.nombre,
                            p_descripcion = op.descripcion,
                            p_etapa = op.etapa,
                            p_probabilidad = prob,
                            p_valor_estimado = op.valor_estimado ?? 0,
                            p_estado = opDb.estado ?? "Activo",
                            p_id_cliente = op.id_cliente,
                            p_id_usuario = opDb.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    // Generar tarea automática si pasa a Propuesta
                    if (esNuevaPropuesta)
                    {
                        db.Execute(
                            "sp_tareas_insertar",
                            new
                            {
                                p_titulo = $"Seguimiento de Propuesta: {op.nombre}",
                                p_descripcion = $"Automatización Comercial: La oportunidad avanzó a etapa de Propuesta. Revisar requerimientos y enviar cotización formal.",
                                p_prioridad = "Alta",
                                p_estado = "Pendiente",
                                p_fecha_limite = DateTime.Now.AddDays(3),
                                p_id_cliente = op.id_cliente,
                                p_id_usuario = opDb.id_usuario
                            },
                            commandType: CommandType.StoredProcedure
                        );
                    }

                    return RedirectToAction("Index");
                }

                ViewBag.Clientes = db.Query<cliente>(
                    "sp_clientes_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(op);
            }
        }

        // GET: Oportunidades/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (!TienePermiso("Oportunidades:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver detalles de Oportunidades.";
                return RedirectToAction("Index");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var op = db.Query<oportunidade, cliente, usuario, oportunidade>(
                    "sp_oportunidades_obtener_con_relaciones",
                    (o, cl, usr) => {
                        o.cliente = cl;
                        o.usuario = usr;
                        return o;
                    },
                    new { p_id_oportunidad = id },
                    splitOn: "id_cliente,id_usuario",
                    commandType: CommandType.StoredProcedure
                ).FirstOrDefault();

                if (op == null) return HttpNotFound();

                int rolId = (int)Session["RolId"];
                if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
                {
                    return RedirectToAction("Index");
                }

                return View(op);
            }
        }

        // POST: Oportunidades/Eliminar/5
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var op = db.QueryFirstOrDefault<oportunidade>(
                        "sp_oportunidades_obtener_por_id",
                        new { p_id_oportunidad = id },
                        commandType: CommandType.StoredProcedure
                    );

                    if (op == null)
                    {
                        return Json(new { success = false, message = "Oportunidad no encontrada" });
                    }

                    int rolId = (int)Session["RolId"];
                    if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
                    {
                        return Json(new { success = false, message = "No tiene permisos para eliminar esta oportunidad" });
                    }

                    db.Execute(
                        "sp_oportunidades_eliminar",
                        new { p_id_oportunidad = id },
                        commandType: CommandType.StoredProcedure
                    );

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Oportunidades/CambiarEtapa
        [HttpPost]
        public JsonResult CambiarEtapa(int id, string etapa, string razon)
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var op = db.QueryFirstOrDefault<oportunidade>(
                        "sp_oportunidades_obtener_por_id",
                        new { p_id_oportunidad = id },
                        commandType: CommandType.StoredProcedure
                    );

                    if (op == null)
                    {
                        return Json(new { success = false, message = "Oportunidad no encontrada" });
                    }

                    int rolId = (int)Session["RolId"];
                    if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
                    {
                        return Json(new { success = false, message = "No tiene permisos para modificar esta oportunidad" });
                    }

                    bool esNuevaPropuesta = (etapa == "Propuesta" && op.etapa != "Propuesta");

                    string desc = op.descripcion;
                    if (etapa == "Cerrada Perdida" && !string.IsNullOrWhiteSpace(razon))
                    {
                        desc = (op.descripcion ?? "") + "\n[Motivo Pérdida: " + razon + "]";
                    }

                    var prob = GetProbabilidadPorEtapa(etapa);

                    db.Execute(
                        "sp_oportunidades_actualizar",
                        new
                        {
                            p_id_oportunidad = id,
                            p_nombre = op.nombre,
                            p_descripcion = desc,
                            p_etapa = etapa,
                            p_probabilidad = prob,
                            p_valor_estimado = op.valor_estimado ?? 0,
                            p_estado = op.estado,
                            p_id_cliente = op.id_cliente,
                            p_id_usuario = op.id_usuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    if (esNuevaPropuesta)
                    {
                        db.Execute(
                            "sp_tareas_insertar",
                            new
                            {
                                p_titulo = $"Seguimiento de Propuesta Rápido: {op.nombre}",
                                p_descripcion = $"Automatización Comercial: Oportunidad movida a Propuesta de forma rápida. Gestionar cotización formal.",
                                p_prioridad = "Alta",
                                p_estado = "Pendiente",
                                p_fecha_limite = DateTime.Now.AddDays(2),
                                p_id_cliente = op.id_cliente,
                                p_id_usuario = op.id_usuario
                            },
                            commandType: CommandType.StoredProcedure
                        );
                    }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private decimal GetProbabilidadPorEtapa(string etapa)
        {
            if (string.IsNullOrEmpty(etapa)) return 10;
            switch (etapa.ToLower())
            {
                case "prospección":
                case "prospeccion":
                    return 10;
                case "calificación":
                case "calificacion":
                    return 30;
                case "propuesta":
                    return 50;
                case "negociación":
                case "negociacion":
                    return 70;
                case "cerrada ganada":
                    return 100;
                case "cerrada perdida":
                    return 0;
                default:
                    return 50;
            }
        }
    }
}