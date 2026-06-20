using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;

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
                using (CRM_RSGEntities db = new CRM_RSGEntities())
                {
                    var nuevaNota = new nota_cliente
                    {
                        id_cliente = id_cliente,
                        comentario = comentario,
                        fecha_creacion = DateTime.Now
                    };

                    db.nota_cliente.Add(nuevaNota);
                    db.SaveChanges();

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