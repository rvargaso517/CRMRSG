using System;

namespace CRMRSG.Models
{
    /// <summary>
    /// DTO para leer notas de cliente directamente desde SQL crudo.
    /// La tabla real en BD usa 'id_note' (sin 'a') y no tiene 'id_usuario'.
    /// </summary>
    public class NotaClienteDTO
    {
        public int IdNota { get; set; }
        public string comentario { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }
}
