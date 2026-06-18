using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework; 

namespace CRMRSG.Controllers
{
    public class PerfilController : Controller
    {
        // GET: Perfil
        public ActionResult Index()
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                // Simulamos jalar el usuario que está logueado en la sesión.
                // Reemplazamos provisionalmente con el primer usuario para que no te tire nulo.
                var usuario = db.usuarios.FirstOrDefault();

                if (usuario == null)
                {
                    return HttpNotFound("No se encontró ningún usuario en la base de datos");
                }

                return View(usuario);
            }
        }

        // POST: Perfil/Actualizar
        [HttpPost]
        [AntiForgeryToken]
        public ActionResult Actualizar(usuarios datosActualizados)
        {
            using (CRM_RSGEntities db = new CRM_RSGEntities())
            {
                try
                {
                    // Buscamos el registro real en la base de datos
                    var usuarioDb = db.usuarios.Find(datosActualizados.id_usuario);

                    if (usuarioDb != null)
                    {
                        // Actualizamos solo los campos de información personal básicos
                        usuarioDb.nombre = datosActualizados.nombre;
                        usuarioDb.correo = datosActualizados.correo;

                        // Si manejás teléfono u otros campos en tu tabla, podés agregarlos aquí abajo:
                        // usuarioDb.telefono = datosActualizados.telefono;

                        db.SaveChanges();
                        TempData["MensajeExito"] = "¡Perfil actualizado con éxito";
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo encontrar el usuario para actualizar.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar el perfil: " + ex.Message);
                }

                return RedirectToAction("Index");
            }
        }
    }
}