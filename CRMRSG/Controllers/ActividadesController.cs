using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class ActividadesController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Actividades
        public ActionResult Index(string filtro)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            int usuarioId = (int)Session["UsuarioId"];
            bool isAdmin = Session["RolId"] != null && (int)Session["RolId"] == 1;

            if (string.IsNullOrEmpty(filtro))
            {
                filtro = "todos";
            }

            ViewBag.FiltroActivo = filtro;

            // Determinar rango de fecha
            DateTime desde = DateTime.MinValue;
            if (filtro == "dia") desde = DateTime.Today;
            else if (filtro == "semana") desde = DateTime.Today.AddDays(-7);
            else if (filtro == "mes") desde = DateTime.Today.AddMonths(-1);

            var query = db.citas.Include(c => c.cliente).Include(c => c.usuario).AsQueryable();

            // Filtrar por rol
            if (!isAdmin)
            {
                query = query.Where(c => c.id_usuario == usuarioId);
            }

            // Aplicar rango de fecha
            if (filtro != "todos")
            {
                query = query.Where(c => c.fecha >= desde);
            }

            var listaActividades = query.OrderByDescending(c => c.fecha).ThenByDescending(c => c.hora).ToList();

            // Estadísticas rápidas por estado
            ViewBag.Pendientes = listaActividades.Count(x => x.estado == "Pendiente" || x.estado == "Programada");
            ViewBag.Confirmadas = listaActividades.Count(x => x.estado == "Completada" || x.estado == "Confirmada" || x.estado == "Realizada");
            ViewBag.Canceladas = listaActividades.Count(x => x.estado == "Cancelada" || x.estado == "Aplazada" || x.estado == "Suspendida");

            return View(listaActividades);
        }

        // GET: Actividades/Crear
        public ActionResult Crear()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            int usuarioId = (int)Session["UsuarioId"];
            bool isAdmin = Session["RolId"] != null && (int)Session["RolId"] == 1;

            // Cargar clientes
            if (isAdmin)
            {
                ViewBag.Clientes = db.clientes.ToList();
                ViewBag.Oportunidades = db.oportunidades.ToList();
            }
            else
            {
                ViewBag.Clientes = db.clientes.Where(c => c.id_usuario == usuarioId).ToList();
                ViewBag.Oportunidades = db.oportunidades.Where(o => o.id_usuario == usuarioId).ToList();
            }

            return View();
        }

        // POST: Actividades/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string tipo_actividad, string fecha, string hora, int? id_cliente, string descripcion)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            if (string.IsNullOrWhiteSpace(tipo_actividad) || string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(hora) || string.IsNullOrWhiteSpace(descripcion))
            {
                TempData["Error"] = "Todos los campos obligatorios deben ser completados.";
                return RedirectToAction("Crear");
            }

            try
            {
                var nuevaCita = new cita
                {
                    descripcion = tipo_actividad + ": " + descripcion,
                    fecha = DateTime.Parse(fecha),
                    hora = TimeSpan.Parse(hora),
                    lugar = tipo_actividad, // Guardar el tipo como lugar
                    estado = "Pendiente",
                    id_cliente = id_cliente,
                    id_usuario = (int)Session["UsuarioId"]
                };

                db.citas.Add(nuevaCita);
                db.SaveChanges();

                TempData["Success"] = "Actividad registrada con éxito.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al registrar la actividad: " + ex.Message;
                return RedirectToAction("Crear");
            }
        }

        // POST: Actividades/Completar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Completar(int id)
        {
            if (Session["UsuarioId"] == null)
            {
                return Json(new { success = false, message = "Sesión no válida" });
            }

            var c = db.citas.Find(id);
            if (c != null)
            {
                c.estado = "Completada";
                db.SaveChanges();
                return Json(new { success = true, message = "Actividad marcada como realizada." });
            }
            return Json(new { success = false, message = "Actividad no encontrada." });
        }

        // POST: Actividades/Posponer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Posponer(int id, string razon, string nuevaFecha)
        {
            if (Session["UsuarioId"] == null)
            {
                return Json(new { success = false, message = "Sesión no válida" });
            }

            var c = db.citas.Find(id);
            if (c != null)
            {
                c.estado = "Aplazada";
                if (!string.IsNullOrWhiteSpace(razon))
                {
                    c.descripcion = (c.descripcion ?? "") + $" [Aplazada: {razon}]";
                }
                if (!string.IsNullOrWhiteSpace(nuevaFecha))
                {
                    c.fecha = DateTime.Parse(nuevaFecha);
                }
                db.SaveChanges();
                return Json(new { success = true, message = "Actividad aplazada correctamente." });
            }
            return Json(new { success = false, message = "Actividad no encontrada." });
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