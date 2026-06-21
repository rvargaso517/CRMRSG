using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Security.Cryptography;
using System.Text;

namespace CRMRSG.Controllers
{
    public class UsuariosController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Usuarios
        public ActionResult Index()
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                TempData["Error"] = "No tiene permisos para acceder a la administración de usuarios.";
                return RedirectToAction("Index", "Dashboard");
            }

            var listaUsuarios = db.usuarios.Include(u => u.role).ToList();
            ViewBag.Roles = db.roles.ToList();
            return View(listaUsuarios);
        }

        // POST: Usuarios/ToggleEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleEstado(int id)
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            var usuario = db.usuarios.Find(id);
            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            // Evitar que el administrador se desactive a sí mismo
            if (usuario.id_usuario == (int)Session["UsuarioId"])
            {
                return Json(new { success = false, message = "No puede desactivar su propia cuenta" });
            }

            usuario.estado = !(usuario.estado ?? false);
            db.SaveChanges();

            return Json(new { success = true, nuevoEstado = usuario.estado, message = "Estado actualizado con éxito" });
        }

        // POST: Usuarios/CambiarRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRol(int id_usuario, int id_rol)
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                TempData["Error"] = "No autorizado.";
                return RedirectToAction("Index");
            }

            var usuario = db.usuarios.Find(id_usuario);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            // Evitar que el administrador se cambie el rol a sí mismo
            if (usuario.id_usuario == (int)Session["UsuarioId"])
            {
                TempData["Error"] = "No puede cambiar el rol de su propia cuenta.";
                return RedirectToAction("Index");
            }

            usuario.id_rol = id_rol;
            db.SaveChanges();

            TempData["Success"] = "Rol actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // POST: Usuarios/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string nombre, string apellido, string correo, string password, int id_rol)
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                TempData["Error"] = "No autorizado.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Los campos Nombre, Correo y Contraseña son obligatorios.";
                return RedirectToAction("Index");
            }

            if (db.usuarios.Any(u => u.correo == correo))
            {
                TempData["Error"] = "El correo electrónico ya está registrado.";
                return RedirectToAction("Index");
            }

            var nuevoUsuario = new usuario
            {
                nombre = nombre,
                apellido = apellido,
                correo = correo,
                password_hash = HashPassword(password),
                estado = true,
                correo_verificado = true,
                fecha_creacion = DateTime.Now,
                id_rol = id_rol
            };

            db.usuarios.Add(nuevoUsuario);
            db.SaveChanges();

            TempData["Success"] = "Usuario creado con éxito.";
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
