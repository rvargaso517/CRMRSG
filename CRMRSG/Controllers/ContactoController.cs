using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class ContactosController : Controller
    {
        // GET: Contactos
        public ActionResult Index()
        {
            using (var db = DbConnectionFactory.GetConnection())
            {
                var contactos = db.Query<contacto_cliente, cliente, contacto_cliente>(
                    "sp_contactos_listar_con_cliente",
                    (co, cl) => {
                        co.cliente = cl;
                        return co;
                    },
                    splitOn: "id_cliente",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(contactos);
            }
        }

        // POST: Contactos/Eliminar/5
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                using (var db = DbConnectionFactory.GetConnection())
                {
                    db.Execute(
                        "sp_contactos_eliminar",
                        new { p_id_contacto = id },
                        commandType: CommandType.StoredProcedure
                    );
                    return Json(new { success = true, message = "Contacto eliminado correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}