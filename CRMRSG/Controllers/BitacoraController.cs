using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

namespace CRMRSG.Controllers
{
    public class BitacoraController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        public ActionResult Index(int? usuarioId)
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                TempData["Error"] = "No tiene permisos para acceder a la bitácora.";
                return RedirectToAction("Index", "Dashboard");
            }

            var query = db.bitacoras.AsQueryable();

            if (usuarioId.HasValue)
            {
                query = query.Where(x => x.id_usuario == usuarioId.Value);
            }

            var historial = query.OrderByDescending(x => x.fecha_hora).ToList();

            ViewBag.Usuarios = db.usuarios.ToList();
            ViewBag.SelectedUsuarioId = usuarioId;

            // Datos para el gráfico de actividad por usuario (Top 5 usuarios más activos)
            var stats = db.bitacoras
                .GroupBy(x => x.usuario != null ? x.usuario.nombre + " " + x.usuario.apellido : "Sistema/Anónimo")
                .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToList();

            ViewBag.ChartLabels = stats.Select(s => s.Nombre).ToArray();
            ViewBag.ChartData = stats.Select(s => s.Cantidad).ToArray();

            return View(historial);
        }
    }

}