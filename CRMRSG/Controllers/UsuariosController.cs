using System;
using System.Linq;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class UsuariosController : Controller
    {
        // GET: Usuarios
        public ActionResult Index()
        {
            if (Session["RolId"] == null || (int)Session["RolId"] != 1)
            {
                TempData["Error"] = "No tiene permisos para acceder a la administración de usuarios.";
                return RedirectToAction("Index", "Dashboard");
            }

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listaUsuarios = db.Query<usuario, role, usuario>(
                    "sp_usuarios_listar",
                    (u, r) => {
                        u.role = r;
                        return u;
                    },
                    splitOn: "RolNombre",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                ViewBag.Roles = db.Query<role>(
                    "sp_roles_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(listaUsuarios);
            }
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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var user = db.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_id",
                    new { p_id_usuario = id },
                    commandType: CommandType.StoredProcedure
                );

                if (user == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                // Evitar que el administrador se desactive a sí mismo
                if (user.id_usuario == (int)Session["UsuarioId"])
                {
                    return Json(new { success = false, message = "No puede desactivar su propia cuenta" });
                }

                bool nuevoEstado = !(user.estado ?? false);

                db.Execute(
                    "sp_usuarios_actualizar",
                    new {
                        p_id_usuario = user.id_usuario,
                        p_nombre = user.nombre,
                        p_apellido = user.apellido,
                        p_correo = user.correo,
                        p_telefono = user.telefono,
                        p_estado = nuevoEstado ? 1 : 0,
                        p_id_rol = user.id_rol // wait, the SP parameter is p_id_rol not p_id_role! Let's check: p_id_rol. Yes!
                    },
                    commandType: CommandType.StoredProcedure
                );

                return Json(new { success = true, nuevoEstado = nuevoEstado, message = "Estado actualizado con éxito" });
            }
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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var user = db.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_id",
                    new { p_id_usuario = id_usuario },
                    commandType: CommandType.StoredProcedure
                );

                if (user == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                // Evitar que el administrador se cambie el rol a sí mismo
                if (user.id_usuario == (int)Session["UsuarioId"])
                {
                    TempData["Error"] = "No puede cambiar el rol de su propia cuenta.";
                    return RedirectToAction("Index");
                }

                db.Execute(
                    "sp_usuarios_actualizar",
                    new {
                        p_id_usuario = id_usuario,
                        p_nombre = user.nombre,
                        p_apellido = user.apellido,
                        p_correo = user.correo,
                        p_telefono = user.telefono,
                        p_estado = user.estado ?? true ? 1 : 0,
                        p_id_rol = id_rol
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

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

            using (var db = DbConnectionFactory.GetConnection())
            {
                var existing = db.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_correo",
                    new { p_correo = correo },
                    commandType: CommandType.StoredProcedure
                );

                if (existing != null)
                {
                    TempData["Error"] = "El correo electrónico ya está registrado.";
                    return RedirectToAction("Index");
                }

                db.Execute(
                    "sp_usuarios_insertar",
                    new {
                        p_nombre = nombre,
                        p_apellido = apellido,
                        p_correo = correo,
                        p_password_hash = HashPassword(password),
                        p_telefono = (string)null,
                        p_id_rol = id_rol
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

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
    }
}
