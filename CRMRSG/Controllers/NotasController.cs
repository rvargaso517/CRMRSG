using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class NotasController : Controller
    {
        // POST: Notas/Guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Guardar(int id_cliente, string comentario)
        {
            if (string.IsNullOrEmpty(comentario))
            {
                TempData["ErrorNota"] = "El contenido de la nota no puede estar vacío.";
                return RedirectToAction("Detalle", "Clientes", new { id = id_cliente });
            }

            try
            {
                using (var db = DbConnectionFactory.GetConnection())
                {
                    int idUsuario = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;

                    db.Execute(
                        "sp_notas_insertar",
                        new {
                            p_id_cliente = id_cliente,
                            p_comentario = comentario,
                            p_id_usuario = idUsuario
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    TempData["ExitoNota"] = "Nota registrada correctamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorNota"] = "Error al guardar la nota: " + ex.Message;
            }

            return RedirectToAction("Detalle", "Clientes", new { id = id_cliente });
        }
    }
}