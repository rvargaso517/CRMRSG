using System.Collections.Generic;
using System.Web;

namespace CRMRSG.Models
{
    public class ImportacionClientesViewModel
    {
        public HttpPostedFileBase Archivo { get; set; }

        public int TotalProcesados { get; set; }

        public int TotalCreados { get; set; }

        public int TotalActualizados { get; set; }

        public int TotalErrores { get; set; }

        public bool ProcesoFinalizado { get; set; }

        public List<string> Errores { get; set; }

        public ImportacionClientesViewModel()
        {
            Errores = new List<string>();
        }
    }
}