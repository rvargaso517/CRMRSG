namespace CRMRSG.EntityFramework
{
    using System;
    using System.Collections.Generic;
    
    public partial class tarea_cliente
    {
        public int id_tarea { get; set; }
        public int id_cliente { get; set; }
        public string tipo_trabajo { get; set; }
        public string descripcion { get; set; }
        public decimal ganancia { get; set; }
        public System.DateTime fecha_inicio { get; set; }
        public Nullable<System.DateTime> fecha_fin { get; set; }
        public string estado { get; set; }
    
        public virtual cliente cliente { get; set; }
    }
}
