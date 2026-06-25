using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class OportunidadesController : Controller
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

            List<oportunidade> lista;
            if (rolId == 1) // Admin
            {
                lista = db.oportunidades.Include(o => o.cliente).Include(o => o.usuario).ToList();
            }
            else
            {
                lista = db.oportunidades.Include(o => o.cliente).Include(o => o.usuario).Where(o => o.id_usuario == usuarioId).ToList();
            }
            return View(lista);
        }

        // GET: Oportunidades/Crear
        public ActionResult Crear()
        {
            if (!TienePermiso("Oportunidades:Gestionar"))
            {
                TempData["Error"] = "No tiene permisos para crear Oportunidades.";
                return RedirectToAction("Index");
            }
            ViewBag.Clientes = db.clientes.ToList();
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

                db.oportunidades.Add(op);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Clientes = db.clientes.ToList();
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

            var op = db.oportunidades.Find(id);
            if (op == null) return HttpNotFound();

            int rolId = (int)Session["RolId"];
            if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
            {
                return RedirectToAction("Index");
            }

            ViewBag.Clientes = db.clientes.ToList();
            return View(op);
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

            var opDb = db.oportunidades.Find(op.id_oportunidad);
            if (opDb == null) return HttpNotFound();

            int rolId = (int)Session["RolId"];
            if (rolId != 1 && opDb.id_usuario != (int)Session["UsuarioId"])
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                opDb.nombre = op.nombre;
                opDb.descripcion = op.descripcion;
                opDb.etapa = op.etapa;
                opDb.valor_estimado = op.valor_estimado;
                opDb.id_cliente = op.id_cliente;
                if (!string.IsNullOrEmpty(fechaClose))
                {
                    opDb.fecha_creacion = DateTime.Parse(fechaClose);
                }
                opDb.probabilidad = GetProbabilidadPorEtapa(op.etapa);

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Clientes = db.clientes.ToList();
            return View(op);
        }

        // GET: Oportunidades/Detalle/5
        public ActionResult Detalle(int id)
        {
            if (!TienePermiso("Oportunidades:Ver"))
            {
                TempData["Error"] = "No tiene permisos para ver detalles de Oportunidades.";
                return RedirectToAction("Index");
            }

            var op = db.oportunidades.Include(o => o.cliente).Include(o => o.usuario).FirstOrDefault(o => o.id_oportunidad == id);
            if (op == null) return HttpNotFound();

            int rolId = (int)Session["RolId"];
            if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
            {
                return RedirectToAction("Index");
            }

            return View(op);
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
                var op = db.oportunidades.Find(id);
                if (op == null)
                {
                    return Json(new { success = false, message = "Oportunidad no encontrada" });
                }

                int rolId = (int)Session["RolId"];
                if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
                {
                    return Json(new { success = false, message = "No tiene permisos para eliminar esta oportunidad" });
                }

                db.oportunidades.Remove(op);
                db.SaveChanges();

                return Json(new { success = true });
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
                var op = db.oportunidades.Find(id);
                if (op == null)
                {
                    return Json(new { success = false, message = "Oportunidad no encontrada" });
                }

                int rolId = (int)Session["RolId"];
                if (rolId != 1 && op.id_usuario != (int)Session["UsuarioId"])
                {
                    return Json(new { success = false, message = "No tiene permisos para modificar esta oportunidad" });
                }

                op.etapa = etapa;
                op.probabilidad = GetProbabilidadPorEtapa(etapa);
                if (etapa == "Cerrada Perdida" && !string.IsNullOrWhiteSpace(razon))
                {
                    op.descripcion = (op.descripcion ?? "") + "\n[Motivo Pérdida: " + razon + "]";
                }
                db.SaveChanges();

                return Json(new { success = true });
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
