using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dapper;
using CRMRSG.Models;
using System.Security.Cryptography;
using System.Text;
using System.Data;

namespace CRMRSG.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Login", "Autenticacion");
        }

        public ActionResult Login()
        {
            return RedirectToAction("Login", "Autenticacion");
        }

        public ActionResult Register()
        {
            return RedirectToAction("Registro", "Autenticacion");
        }

        public ActionResult RecoverPassword()
        {
            // Redirect to the correct action name in AutenticacionController (without accent)
            return RedirectToAction("CambiarContrasena", "Autenticacion");
        }

        // GET: Home/SeedUsers
        public ActionResult SeedUsers()
        {
            try
            {
                string passwordHash = HashPassword("Password123!");
                using (var conn = DbConnectionFactory.GetConnection())
                {
                    // Ensure roles exist in roles table
                    var roles = conn.Query("SELECT id_rol, nombre FROM roles").ToDictionary(r => (int)r.id_rol, r => (string)r.nombre);
                    if (!roles.ContainsKey(1)) conn.Execute("INSERT INTO roles (id_rol, nombre, descripcion) VALUES (1, 'Administrador', 'Admin:Acceso,Clientes:Ver,Clientes:Gestionar,Usuarios:Gestionar')");
                    if (!roles.ContainsKey(2)) conn.Execute("INSERT INTO roles (id_rol, nombre, descripcion) VALUES (2, 'Vendedor', 'Clientes:Ver,Clientes:Gestionar')");
                    if (!roles.ContainsKey(3)) conn.Execute("INSERT INTO roles (id_rol, nombre, descripcion) VALUES (3, 'Cliente', 'Clientes:Ver')");

                    // Seed Admin
                    var admin = conn.QueryFirstOrDefault("SELECT * FROM usuarios WHERE correo = 'admin.crm@gmail.com'");
                    if (admin == null)
                    {
                        conn.Execute("INSERT INTO usuarios (nombre, apellido, correo, password_hash, estado, id_rol) VALUES ('Admin', 'CRM', 'admin.crm@gmail.com', @pwd, 1, 1)", new { pwd = passwordHash });
                    }
                    else
                    {
                        conn.Execute("UPDATE usuarios SET password_hash = @pwd, estado = 1, id_rol = 1 WHERE correo = 'admin.crm@gmail.com'", new { pwd = passwordHash });
                    }

                    // Seed Vendedor
                    var vendedor = conn.QueryFirstOrDefault("SELECT * FROM usuarios WHERE correo = 'vendedor.test@gmail.com'");
                    if (vendedor == null)
                    {
                        conn.Execute("INSERT INTO usuarios (nombre, apellido, correo, password_hash, estado, id_rol) VALUES ('Vendedor', 'Test', 'vendedor.test@gmail.com', @pwd, 1, 2)", new { pwd = passwordHash });
                    }
                    else
                    {
                        conn.Execute("UPDATE usuarios SET password_hash = @pwd, estado = 1, id_rol = 2 WHERE correo = 'vendedor.test@gmail.com'", new { pwd = passwordHash });
                    }

                    // Seed Cliente User
                    var clienteUser = conn.QueryFirstOrDefault("SELECT * FROM usuarios WHERE correo = 'cliente.test@gmail.com'");
                    if (clienteUser == null)
                    {
                        conn.Execute("INSERT INTO usuarios (nombre, apellido, correo, password_hash, estado, id_rol) VALUES ('Cliente', 'Test', 'cliente.test@gmail.com', @pwd, 1, 3)", new { pwd = passwordHash });
                    }
                    else
                    {
                        conn.Execute("UPDATE usuarios SET password_hash = @pwd, estado = 1, id_rol = 3 WHERE correo = 'cliente.test@gmail.com'", new { pwd = passwordHash });
                    }
                }
                return Content("Database seeded successfully with test credentials!");
            }
            catch (Exception ex)
            {
                return Content("Error seeding database: " + ex.Message + "\n" + ex.StackTrace);
            }
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