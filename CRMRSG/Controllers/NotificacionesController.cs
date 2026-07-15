using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class NotificacionesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

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
            var citasProximas = db.citas
                .Where(c => c.estado != "Completada" 
                         && c.estado != "Realizada" 
                         && c.estado != "Cancelada"
                         && c.fecha <= limiteAlerta)
                .ToList();

            bool huboCambios = false;
            foreach (var cita in citasProximas)
            {
                bool exists = db.notificaciones.Any(n => n.tipo == "Alerta de Cita" && n.id_referencia == cita.id_cita);
                if (!exists)
                {
                    string msg = $"Cita/Evento comercial programado: '{cita.descripcion}' el {cita.fecha.ToString("dd/MM/yyyy")} a las {cita.hora.ToString(@"hh\:mm")}. Lugar: {cita.lugar}";
                    var noti = new notificacione
                    {
                        mensaje = msg,
                        fecha = DateTime.Now,
                        leida = false,
                        id_usuario = cita.id_usuario ?? 1,
                        tipo = "Alerta de Cita",
                        id_referencia = cita.id_cita
                    };
                    db.notificaciones.Add(noti);
                    huboCambios = true;
                }
            }
            if (huboCambios) db.SaveChanges();
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
                var notificacionesCrudas = db.notificaciones
                    .Where(n => n.id_usuario == usuarioId)
                    .OrderByDescending(n => n.fecha)
                    .ToList();

                var listado = notificacionesCrudas.Select(n => new {
                    n.id_notificacion,
                    n.mensaje,
                    leida = n.leida ?? false,
                    FechaRegistro = n.fecha.HasValue ? n.fecha.Value.ToString("dd/MM/yyyy hh:mm tt") : ""
                }).ToList();

                return Json(new { success = true, datos = listado }, JsonRequestBehavior.AllowGet);
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
                var noti = db.notificaciones.FirstOrDefault(n => n.id_notificacion == id);
                if (noti != null)
                {
                    noti.leida = true;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, mensaje = "Notificación no encontrada" });
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

                var unreadNotifications = db.notificaciones.Where(n => n.leida == false || n.leida == null).ToList();

                if (unreadNotifications.Any())
                {
                    foreach (var noti in unreadNotifications)
                    {
                        noti.leida = true;
                    }
                    db.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}