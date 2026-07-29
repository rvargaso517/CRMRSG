using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class CitasController : Controller
    {
        // GET: Citas
        public ActionResult Index()
        {
            using (var db = DbConnectionFactory.GetConnection())
            {
                var citas = db.Query<cita, cliente, cita>(
                    "sp_citas_listar_con_cliente",
                    (c, cl) => {
                        c.cliente = cl;
                        return c;
                    },
                    splitOn: "id_cliente",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(citas);
            }
        }

        // POST: Citas/Agendar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Agendar(int id_cliente, string asunto, DateTime fecha_cita)
        {
            int? currentUserId = Session["UsuarioId"] != null ? (int?)Session["UsuarioId"] : null;

            using (var db = DbConnectionFactory.GetConnection())
            {
                db.Execute(
                    "sp_citas_insertar",
                    new {
                        p_fecha = fecha_cita.Date,
                        p_hora = fecha_cita.TimeOfDay,
                        p_descripcion = asunto,
                        p_lugar = "Virtual",
                        p_estado = "Programada",
                        p_id_cliente = id_cliente,
                        p_id_usuario = currentUserId
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
            return RedirectToAction("Index");
        }
    }
}