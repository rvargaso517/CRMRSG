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

namespace CRMRSG.Controllers
{
    public class AutenticacionController : Controller
    {
        private CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: Autenticacion/Login
        public ActionResult Login()
        {
            if (Session["UsuarioId"] != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"].ToString();
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

            // Buscar al usuario por correo
            var usuario = db.usuarios.FirstOrDefault(u => u.correo == correo);

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

            // Comparar la contraseña hasheada
            if (!usuario.password_hash.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
              ViewBag.Error = "Contraseña incorrecta.";
                return View();
            }
            
            // Actualizar fecha de último login
            usuario.ultimo_login = DateTime.Now;
            db.SaveChanges();

            // Configurar variables de sesión
            Session["UsuarioId"] = usuario.id_usuario;
            Session["NombreCompleto"] = $"{usuario.nombre} {usuario.apellido}".Trim();
            Session["Nombre"] = usuario.nombre;
            Session["RolId"] = usuario.id_rol;
            Session["Correo"] = usuario.correo;

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

            // Verificar si el correo ya está registrado
            if (db.usuarios.Any(u => u.correo == correo))
            {
                ViewBag.Error = "El correo electrónico ya está registrado.";
                return View();
            }

            // Dividir nombre completo en nombre y apellido
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

            // Crear el nuevo usuario
            var nuevoUsuario = new usuarios
            {
                nombre = nombre,
                apellido = apellido,
                correo = correo,
                password_hash = HashPassword(password),
                estado = true,
                correo_verificado = false,
                fecha_creacion = DateTime.Now,
                id_rol = 2 // Rol por defecto (Usuario / Staff)
            };

            try
            {
                db.usuarios.Add(nuevoUsuario);
                db.SaveChanges();

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

            // No revelar si el correo existe o no: mostrar siempre el mismo mensaje
            var usuario = db.usuarios.FirstOrDefault(u => u.correo == correo);

            if (usuario != null)
            {
                // Generar token
                string token = Guid.NewGuid().ToString("N");
                usuario.token_recuperacion = token;
                usuario.fecha_expiracion_recuperacion = DateTime.Now.AddHours(2); // token válido por 2 horas
                db.SaveChanges();

                // Construir URL de restablecimiento
                string scheme = Request.Url != null ? Request.Url.Scheme : "http";
                string resetUrl = Url.Action("Restablecer", "Autenticacion", new { token = token }, scheme);

                // Enviar correo (si falla, no exponer detalles al usuario)
                try
                {
                    // Leer template y reemplazar placeholders
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
                    // Loggear en real app. Aquí se silencia para no filtrar información.
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

            var usuario = db.usuarios.FirstOrDefault(u => u.token_recuperacion == token);

            if (usuario == null || usuario.fecha_expiracion_recuperacion == null || usuario.fecha_expiracion_recuperacion < DateTime.Now)
            {
                ViewBag.Error = "El enlace ha expirado o es inválido.";
                return View();
            }

            ViewBag.Token = token;
            return View(); // View must provide form to set new password and post to Restablecer (POST)
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

            var usuario = db.usuarios.FirstOrDefault(u => u.token_recuperacion == token);

            if (usuario == null || usuario.fecha_expiracion_recuperacion == null || usuario.fecha_expiracion_recuperacion < DateTime.Now)
            {
                ViewBag.Error = "El enlace ha expirado o es inválido.";
                return View();
            }

            // Actualizar contraseña
            usuario.password_hash = HashPassword(password);
            usuario.token_recuperacion = null;
            usuario.fecha_expiracion_recuperacion = null;
            db.SaveChanges();

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
