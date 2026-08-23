using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;
using System;

namespace CRMRSG.Controllers
{
    public class BitacoraController : Controller
    {
        public ActionResult Index(int? usuarioId, int page = 1)
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                TempData["Error"] = "No tiene permisos para acceder a la bitácora.";
                return RedirectToAction("Index", "Dashboard");
            }

            int pageSize = 10; // Máximo 10 tablas/registros por página

            using (var db = DbConnectionFactory.GetConnection())
            {
                var historial = db.Query<bitacora, usuario, bitacora>(
                    "sp_bitacora_listar_con_usuario",
                    (b, u) => {
                        b.usuario = u;
                        return b;
                    },
                    splitOn: "id_usuario",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (usuarioId.HasValue)
                {
                    historial = historial.Where(x => x.id_usuario == usuarioId.Value).ToList();
                }

                int totalRecords = historial.Count;
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                // Asegurar que la página esté en un rango válido
                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages) page = totalPages;

                // Aplicar Paginación (10 elementos por página)
                var historialPaginado = historial
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var usuarios = db.Query<usuario>(
                    "sp_usuarios_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                ViewBag.Usuarios = usuarios;
                ViewBag.SelectedUsuarioId = usuarioId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                // Datos para el gráfico de actividad por usuario (Top 5 usuarios más activos)
                var stats = historial
                    .GroupBy(x => x.usuario != null ? x.usuario.nombre + " " + x.usuario.apellido : "Sistema/Anónimo")
                    .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                    .OrderByDescending(x => x.Cantidad)
                    .Take(5)
                    .ToList();

                ViewBag.ChartLabels = stats.Select(s => s.Nombre).ToArray();
                ViewBag.ChartData = stats.Select(s => s.Cantidad).ToArray();

                return View(historialPaginado);
            }
        }
    }
}