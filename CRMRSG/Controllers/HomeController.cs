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
            // Redirect to Login by default
            return RedirectToAction("Login", "Home");
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        public ActionResult RecoverPassword()
        {
            return View();
        }
    }
}