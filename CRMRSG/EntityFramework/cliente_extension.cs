using System;
using System.Linq;

namespace CRMRSG.EntityFramework
{
    public partial class cliente
    {
        public int LeadScore
        {
            get
            {
                int score = 0;
                
                // 1. Oportunidades: +20 puntos por cada una (Máximo 40)
                int opCount = this.oportunidades != null ? this.oportunidades.Count : 0;
                score += Math.Min(opCount * 20, 40);

                // 2. Valor total de oportunidades: +10 puntos por cada $10,000 (Máximo 30)
                decimal totalValue = this.oportunidades != null ? this.oportunidades.Sum(o => o.valor_estimado ?? 0) : 0;
                score += Math.Min((int)(totalValue / 10000) * 10, 30);

                // 3. Citas/Reuniones agendadas: +15 puntos por cada una (Máximo 30)
                int citasCount = this.citas != null ? this.citas.Count : 0;
                score += Math.Min(citasCount * 15, 30);

                // 4. Tareas completadas: +10 puntos por cada tarea (Máximo 20)
                int tareasCompletadas = this.tareas != null ? this.tareas.Count(t => t.estado == "Completada") : 0;
                score += Math.Min(tareasCompletadas * 10, 20);

                // 5. Estado activo: +10 puntos
                if (this.estado == "Activo")
                {
                    score += 10;
                }

                // Ajustar entre 0 y 100
                return Math.Max(0, Math.Min(score, 100));
            }
        }

        public string Clasificacion
        {
            get
            {
                int score = this.LeadScore;
                if (score >= 70) return "VIP";
                if (score >= 35) return "Medio";
                return "Bajo";
            }
        }
    }
}
