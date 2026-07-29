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
    public class NotificacionesController : Controller
    {
        public ActionResult Index()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }
            return View();
        }

        private void VerificarYGenerarAlertasCitas()
        {
            DateTime limiteAlerta = DateTime.Today.AddDays(1);

            using (var db = DbConnectionFactory.GetConnection())
            {
                var citasProximas = db.Query<cita>(
                    "sp_citas_listar_proximas_alertas",
                    new { p_limite = limiteAlerta },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                foreach (var cita in citasProximas)
                {
                    int existsCount = db.QuerySingle<int>(
                        "sp_notificaciones_existe_alerta",
                        new { p_id_referencia = cita.id_cita, p_tipo = "Alerta de Cita" },
                        commandType: CommandType.StoredProcedure
                    );

                    if (existsCount == 0)
                    {
                        string msg = $"Cita/Evento comercial programado: '{cita.descripcion}' el {cita.fecha.ToString("dd/MM/yyyy")} a las {cita.hora.ToString(@"hh\:mm")}. Lugar: {cita.lugar}";
                        db.Execute(
                            "sp_notificaciones_insertar",
                            new {
                                p_mensaje = msg,
                                p_id_usuario = cita.id_usuario ?? 1,
                                p_tipo = "Alerta de Cita",
                                p_id_referencia = cita.id_cita
                            },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
            }
        }

        [HttpGet]
        public JsonResult ObtenerListaCompletas()
        {
            try
            {
                if (Session["UsuarioId"] == null)
                    return Json(new { success = false, mensaje = "Sesión no válida" }, JsonRequestBehavior.AllowGet);

                VerificarYGenerarAlertasCitas();

                int usuarioId = (int)Session["UsuarioId"];
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var listado = db.Query<notificacione>(
                        "sp_notificaciones_listar_por_usuario",
                        new { p_id_usuario = usuarioId },
                        commandType: CommandType.StoredProcedure
                    ).Select(n => new {
                        id_notificacion = n.id_notificacion,
                        mensaje = n.mensaje,
                        leida = n.leida ?? false,
                        FechaRegistro = n.fecha.HasValue ? n.fecha.Value.ToString("dd/MM/yyyy hh:mm tt") : ""
                    }).ToList();

                    return Json(new { success = true, datos = listado }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult MarcarComoLeida(int id)
        {
            try
            {
                using (var db = DbConnectionFactory.GetConnection())
                {
                    db.Execute(
                        "sp_notificaciones_marcar_leida",
                        new { p_id_notificacion = id },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult MarcarTodasComoLeidas()
        {
            try
            {
                if (Session["UsuarioId"] == null)
                    return Json(new { success = false, mensaje = "Sesión no válida" });

                int usuarioId = (int)Session["UsuarioId"];
                using (var db = DbConnectionFactory.GetConnection())
                {
                    db.Execute(
                        "UPDATE notificaciones SET leida = 1 WHERE id_usuario = @IdUsuario AND (leida = 0 OR leida IS NULL)",
                        new { IdUsuario = usuarioId }
                    );
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}