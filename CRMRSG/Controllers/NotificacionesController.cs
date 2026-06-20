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

        [HttpGet]
        public JsonResult ObtenerListaCompletas()
        {
            try
            {
                if (Session["UsuarioId"] == null)
                    return Json(new { success = false, mensaje = "Sesión no válida" }, JsonRequestBehavior.AllowGet);

                var notificacionesCrudas = db.notificaciones.OrderByDescending(n => n.fecha).ToList();

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