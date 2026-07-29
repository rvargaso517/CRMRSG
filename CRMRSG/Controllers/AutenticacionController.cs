using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;
using System.Configuration;
using System.IO;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class AutenticacionController : Controller
    {
        // GET: Autenticacion/Login
        public ActionResult Login()
        {
            if (Session["UsuarioId"] != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        // POST: Autenticacion/Login
        [HttpPost]
        public ActionResult Login(string correo, string password)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "El correo y la contraseña son requeridos.";
                return View();
            }

            string hashedPassword = HashPassword(password);

            usuario usuario = null;
            using (var conn = DbConnectionFactory.GetConnection())
            {
                var dict = conn.QueryFirstOrDefault<dynamic>(
                    "sp_usuarios_obtener_por_correo",
                    new { p_correo = correo },
                    commandType: CommandType.StoredProcedure
                );

                if (dict != null)
                {
                    usuario = new usuario
                    {
                        id_usuario = dict.id_usuario,
                        nombre = dict.nombre,
                        apellido = dict.apellido,
                        correo = dict.correo,
                        password_hash = dict.password_hash,
                        estado = dict.estado,
                        correo_verificado = dict.correo_verificado,
                        id_rol = dict.id_rol,
                        role = new role { nombre = dict.RolNombre, descripcion = dict.RolDescripcion }
                    };
                }
            }

            if (usuario == null)
            {
                ViewBag.Error = "El correo electrónico no está registrado.";
                return View();
            }

            if (usuario.estado == false)
            {
                ViewBag.Error = "Esta cuenta se encuentra inactiva. Contacte al administrador.";
                return View();
            }

            if (!usuario.password_hash.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Contraseña incorrecta.";
                return View();
            }
            
            using (var conn = DbConnectionFactory.GetConnection())
            {
                conn.Execute(
                    "sp_usuarios_actualizar_ultimo_login",
                    new { p_id_usuario = usuario.id_usuario },
                    commandType: CommandType.StoredProcedure
                );
            }

            Session["UsuarioId"] = usuario.id_usuario;
            Session["NombreCompleto"] = $"{usuario.nombre} {usuario.apellido}".Trim();
            Session["Nombre"] = usuario.nombre;
            Session["RolId"] = usuario.id_rol;
            Session["Correo"] = usuario.correo;
            Session["Permisos"] = usuario.role != null ? usuario.role.descripcion : "";

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: Autenticacion/Registro
        public ActionResult Registro()
        {
            if (Session["UsuarioId"] != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // POST: Autenticacion/Registro
        [HttpPost]
        public ActionResult Registro(string nombreCompleto, string correo, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(correo) || 
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View();
            }

            if (password.Length < 8 || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[\W_]"))
            {
                ViewBag.Error = "La contraseña debe tener al menos 8 caracteres, una mayúscula, un número y un carácter especial.";
                return View();
            }
            
            if (!correo.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Debe registrarse utilizando un correo @gmail.com válido.";
                return View();
            }
            
            if (password != confirmPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View();
            }

            bool correoExiste = false;
            using (var conn = DbConnectionFactory.GetConnection())
            {
                var existing = conn.QueryFirstOrDefault<dynamic>(
                    "sp_usuarios_obtener_por_correo",
                    new { p_correo = correo },
                    commandType: CommandType.StoredProcedure
                );
                correoExiste = (existing != null);
            }

            if (correoExiste)
            {
                ViewBag.Error = "El correo electrónico ya está registrado.";
                return View();
            }

            string nombre = "";
            string apellido = "";
            if (!string.IsNullOrWhiteSpace(nombreCompleto))
            {
                var partes = nombreCompleto.Trim().Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                nombre = partes[0];
                if (partes.Length > 1)
                {
                    apellido = partes[1];
                }
            }

            try
            {
                using (var conn = DbConnectionFactory.GetConnection())
                {
                    conn.Execute(
                        "sp_usuarios_insertar",
                        new {
                            p_nombre = nombre,
                            p_apellido = apellido,
                            p_correo = correo,
                            p_password_hash = HashPassword(password),
                            p_telefono = (string)null,
                            p_id_rol = 2
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                TempData["Success"] = "Registro exitoso. Por favor, inicie sesión con su nueva cuenta.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error al registrar el usuario: " + ex.Message;
                return View();
            }
        }

        // GET: Autenticacion/CambiarContrasena (vista para solicitar recuperación)
        public ActionResult CambiarContrasena()
        {
            return View();
        }

        // POST: Autenticacion/CambiarContrasena
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarContrasena(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                ViewBag.Error = "Por favor ingrese su correo electrónico.";
                return View();
            }

            usuario usuario = null;
            using (var conn = DbConnectionFactory.GetConnection())
            {
                var dict = conn.QueryFirstOrDefault<dynamic>(
                    "sp_usuarios_obtener_por_correo",
                    new { p_correo = correo },
                    commandType: CommandType.StoredProcedure
                );

                if (dict != null)
                {
                    usuario = new usuario
                    {
                        id_usuario = dict.id_usuario,
                        correo = dict.correo
                    };
                }
            }

            if (usuario != null)
            {
                string token = Guid.NewGuid().ToString("N");
                var fechaExp = DateTime.Now.AddHours(2);

                using (var conn = DbConnectionFactory.GetConnection())
                {
                    conn.Execute(
                        "sp_usuarios_actualizar_token_recuperacion",
                        new {
                            p_id_usuario = usuario.id_usuario,
                            p_token = token,
                            p_fecha_expiracion = fechaExp
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                string scheme = Request.Url != null ? Request.Url.Scheme : "http";
                string resetUrl = Url.Action("Restablecer", "Autenticacion", new { token = token }, scheme);

                try
                {
                    var templatePath = Server.MapPath("~/Views/Emails/RecoverPasswordTemplate.cshtml");
                    string body = null;
                    if (System.IO.File.Exists(templatePath))
                    {
                        body = System.IO.File.ReadAllText(templatePath);
                        var logoUrl = Url.Content("~/Content/images/logo-light-text3.png");
                        body = body.Replace("@@resetUrl", resetUrl).Replace("@@logoUrl", logoUrl);
                        SendRecoveryEmail(usuario.correo, resetUrl, body);
                    }
                    else
                    {
                        SendRecoveryEmail(usuario.correo, resetUrl);
                    }
                }
                catch
                {
                }
            }

            ViewBag.Success = "Si el correo existe en nuestro sistema, se ha enviado un enlace para restablecer la contraseña.";
            return View();
        }

        // GET: Autenticacion/Restablecer?token=...
        public ActionResult Restablecer(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Token inválido.";
                return View();
            }

            usuario usuario = null;
            using (var conn = DbConnectionFactory.GetConnection())
            {
                usuario = conn.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_token_recuperacion",
                    new { p_token = token },
                    commandType: CommandType.StoredProcedure
                );
            }

            if (usuario == null || usuario.fecha_expiracion_recuperacion == null || usuario.fecha_expiracion_recuperacion < DateTime.Now)
            {
                ViewBag.Error = "El enlace ha expirado o es inválido.";
                return View();
            }

            ViewBag.Token = token;
            return View();
        }

        // POST: Autenticacion/Restablecer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restablecer(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Token inválido.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Por favor ingrese la nueva contraseña y su confirmación.";
                ViewBag.Token = token;
                return View();
            }

            if (password.Length < 8 || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[\W_]"))
            {
                ViewBag.Error = "La contraseña debe tener al menos 8 caracteres, una mayúscula, un número y un carácter especial.";
                ViewBag.Token = token;
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                ViewBag.Token = token;
                return View();
            }

            usuario usuario = null;
            using (var conn = DbConnectionFactory.GetConnection())
            {
                usuario = conn.QueryFirstOrDefault<usuario>(
                    "sp_usuarios_obtener_por_token_recuperacion",
                    new { p_token = token },
                    commandType: CommandType.StoredProcedure
                );
            }

            if (usuario == null || usuario.fecha_expiracion_recuperacion == null || usuario.fecha_expiracion_recuperacion < DateTime.Now)
            {
                ViewBag.Error = "El enlace ha expirado o es inválido.";
                return View();
            }

            using (var conn = DbConnectionFactory.GetConnection())
            {
                conn.Execute(
                    "sp_usuarios_actualizar_contrasena",
                    new {
                        p_id_usuario = usuario.id_usuario,
                        p_password_hash = HashPassword(password)
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            TempData["Success"] = "Contraseña restaurada con éxito. Ahora puede iniciar sesión con su nueva contraseña.";
            return RedirectToAction("Login");
        }

        // GET: Autenticacion/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // Método auxiliar para hashear la contraseña con SHA256
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

        // Envío de correo de recuperación (lee configuración desde web.config appSettings)
        private void SendRecoveryEmail(string toEmail, string resetUrl)
        {
            SendRecoveryEmail(toEmail, resetUrl, null);
        }

        private void SendRecoveryEmail(string toEmail, string resetUrl, string htmlBody)
        {
            // Configuración esperada en web.config (appSettings):
            // SmtpHost, SmtpPort, SmtpUser, SmtpPass, SmtpFrom, SmtpEnableSsl
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var portStr = ConfigurationManager.AppSettings["SmtpPort"];
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPass"];
            var from = ConfigurationManager.AppSettings["SmtpFrom"] ?? "no-reply@example.com";
            var enableSslStr = ConfigurationManager.AppSettings["SmtpEnableSsl"];

            int port = 25;
            bool enableSsl = false;
            int.TryParse(portStr, out port);
            bool.TryParse(enableSslStr, out enableSsl);

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("SMTP host is not configured.");
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(from, "CRM-RSG");
                message.To.Add(new MailAddress(toEmail));
                message.Subject = "Recuperación de contraseña - CRM RSG";
                message.IsBodyHtml = true;
                if (!string.IsNullOrWhiteSpace(htmlBody))
                {
                    message.Body = htmlBody;
                }
                else
                {
                    message.Body = $"<p>Se solicitó restablecer la contraseña. Haga clic en el siguiente enlace para elegir una nueva contraseña:</p>" +
                                   $"<p><a href=\"{resetUrl}\">{resetUrl}</a></p>" +
                                   "<p>Si no solicitó este cambio, puede ignorar este mensaje.</p>";
                }

                using (var client = new SmtpClient(host, port))
                {
                    if (!string.IsNullOrWhiteSpace(user))
                    {
                        client.Credentials = new NetworkCredential(user, pass);
                    }
                    client.EnableSsl = enableSsl;
                    client.Send(message);
                }
            }
        }
    }
}
