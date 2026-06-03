using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
    }
}