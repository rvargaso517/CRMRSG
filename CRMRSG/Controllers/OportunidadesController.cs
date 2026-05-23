using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace CRMRSG.Controllers
{
    public class OportunidadesController : Controller
    {
        // GET: Oportunidades
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Crear()
        {
            return View();
        }

        public ActionResult Editar(int id)
        {
            return View();
        }

        public ActionResult Detalle(int id)
        {
            return View();
        }
    }
}
