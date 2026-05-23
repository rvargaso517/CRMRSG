using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace CRMRSG.Controllers
{
    public class ActividadesController : Controller
    {
        // GET: Actividades
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Crear()
        {
            return View();
        }
    }
}
