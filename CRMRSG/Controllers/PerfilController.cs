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
    public class PerfilController : Controller
    {
        // GET: Perfil
        public ActionResult Index()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            int usuarioId = (int)Session["UsuarioId"];
            using (var db = DbConnectionFactory.GetConnection())
            {
                var usuario = db.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_id",
                    new { p_id_usuario = usuarioId },
                    commandType: CommandType.StoredProcedure
                );

                if (usuario == null)
                {
                    return HttpNotFound("No se encontró el usuario en la base de datos.");
                }

                return View(usuario);
            }
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
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var usuarioDb = db.QueryFirstOrDefault<usuario>(
                        "sp_usuarios_obtener_por_id",
                        new { p_id_usuario = datosActualizados.id_usuario },
                        commandType: CommandType.StoredProcedure
                    );

                    if (usuarioDb != null)
                    {
                        // Actualizar campos personales
                        db.Execute(
                            "sp_usuarios_actualizar",
                            new {
                                p_id_usuario = datosActualizados.id_usuario,
                                p_nombre = datosActualizados.nombre,
                                p_apellido = datosActualizados.apellido,
                                p_correo = datosActualizados.correo,
                                p_telefono = datosActualizados.telefono,
                                p_estado = usuarioDb.estado,
                                p_id_rol = usuarioDb.id_rol
                            },
                            commandType: CommandType.StoredProcedure
                        );

                        // Actualizar contraseña si se proporcionó una nueva
                        if (!string.IsNullOrWhiteSpace(nuevaPassword))
                        {
                            if (nuevaPassword.Length < 8)
                            {
                                TempData["MensajeError"] = "La nueva contraseña debe tener al menos 8 caracteres.";
                                return RedirectToAction("Index");
                            }
                            
                            db.Execute(
                                "sp_usuarios_actualizar_contrasena",
                                new {
                                    p_id_usuario = datosActualizados.id_usuario,
                                    p_password_hash = HashPassword(nuevaPassword)
                                },
                                commandType: CommandType.StoredProcedure
                            );
                        }

                        // Actualizar variables de sesión relacionadas con la información del usuario
                        Session["NombreCompleto"] = $"{datosActualizados.nombre} {datosActualizados.apellido}".Trim();
                        Session["Nombre"] = datosActualizados.nombre;
                        Session["Correo"] = datosActualizados.correo;

                        TempData["MensajeExito"] = "¡Perfil actualizado con éxito!";
                    }
                    else
                    {
                        TempData["MensajeError"] = "No se pudo encontrar el usuario para actualizar.";
                    }
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
    }
}