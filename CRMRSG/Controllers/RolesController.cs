using CRMRSG.EntityFramework;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class RolesController : Controller
    {
        // GET: Roles
        public ActionResult Index()
        {
            using (var db = DbConnectionFactory.GetConnection())
            {
                var roles = db.Query<role>(
                    "sp_roles_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();
                return View(roles);
            }
        }

        // POST: Roles/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string nombre_rol, string[] permisos)
        {
            if (string.IsNullOrEmpty(nombre_rol)) return RedirectToAction("Index");

            string nombreTrimmed = nombre_rol.Trim();
            string nombreLower = nombreTrimmed.ToLower();

            using (var db = DbConnectionFactory.GetConnection())
            {
                var roles = db.Query<role>(
                    "sp_roles_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (roles.Any(r => r.nombre.ToLower() == nombreLower))
                {
                    TempData["Error"] = "El rol '" + nombreTrimmed + "' ya existe.";
                    return RedirectToAction("Index");
                }

                string desc = permisos != null ? string.Join(",", permisos) : "Sin permisos";

                try
                {
                    db.Execute(
                        "sp_roles_insertar",
                        new { p_nombre = nombreTrimmed, p_descripcion = desc },
                        commandType: CommandType.StoredProcedure
                    );
                    TempData["Success"] = "Rol creado con éxito.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al crear el rol: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // POST: Roles/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(int id_rol, string nombre_rol, string[] permisos)
        {
            if (string.IsNullOrEmpty(nombre_rol)) return RedirectToAction("Index");

            using (var db = DbConnectionFactory.GetConnection())
            {
                var rol = db.QueryFirstOrDefault<role>(
                    "sp_roles_obtener_por_id",
                    new { p_id_rol = id_rol },
                    commandType: CommandType.StoredProcedure
                );

                if (rol != null)
                {
                    string nuevoNombre = rol.nombre;
                    if (rol.id_rol != 1) // Evitar renombrar el Administrador del sistema
                    {
                        nuevoNombre = nombre_rol.Trim();
                    }
                    string desc = permisos != null ? string.Join(",", permisos) : "Sin permisos";

                    db.Execute(
                        "sp_roles_actualizar",
                        new { p_id_rol = id_rol, p_nombre = nuevoNombre, p_descripcion = desc },
                        commandType: CommandType.StoredProcedure
                    );
                    TempData["Success"] = "Rol actualizado con éxito.";
                }
                else
                {
                    TempData["Error"] = "Rol no encontrado.";
                }
            }
            return RedirectToAction("Index");
        }
    }
}