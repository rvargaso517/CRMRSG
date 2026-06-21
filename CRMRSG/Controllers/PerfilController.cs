using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Security.Cryptography;
using System.Text;

namespace CRMRSG.Controllers
{
    public class PerfilController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Perfil
        public ActionResult Index()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            int usuarioId = (int)Session["UsuarioId"];
            var usuario = db.usuarios.Find(usuarioId);

            if (usuario == null)
            {
                return HttpNotFound("No se encontró el usuario en la base de datos.");
            }

            return View(usuario);
        }

        // POST: Perfil/Actualizar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(usuario datosActualizados, string nuevaPassword)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            try
            {
                var usuarioDb = db.usuarios.Find(datosActualizados.id_usuario);

                if (usuarioDb != null)
                {
                    // Actualizar campos personales
                    usuarioDb.nombre = datosActualizados.nombre;
                    usuarioDb.apellido = datosActualizados.apellido;
                    usuarioDb.correo = datosActualizados.correo;
                    usuarioDb.telefono = datosActualizados.telefono;

                    // Actualizar contraseña si se proporcionó una nueva
                    if (!string.IsNullOrWhiteSpace(nuevaPassword))
                    {
                        if (nuevaPassword.Length < 8)
                        {
                            TempData["MensajeError"] = "La nueva contraseña debe tener al menos 8 caracteres.";
                            return RedirectToAction("Index");
                        }
                        usuarioDb.password_hash = HashPassword(nuevaPassword);
                    }

                    db.SaveChanges();

                    // Actualizar variables de sesión relacionadas con la información del usuario
                    Session["NombreCompleto"] = $"{usuarioDb.nombre} {usuarioDb.apellido}".Trim();
                    Session["Nombre"] = usuarioDb.nombre;
                    Session["Correo"] = usuarioDb.correo;

                    TempData["MensajeExito"] = "¡Perfil actualizado con éxito!";
                }
                else
                {
                    TempData["MensajeError"] = "No se pudo encontrar el usuario para actualizar.";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al actualizar el perfil: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
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