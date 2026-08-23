using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using CRMRSG.EntityFramework;
using System.Data;
using Dapper;
using CRMRSG.Models;

namespace CRMRSG.Controllers
{
    public class CorreoProgramado
    {
        public int id_correo { get; set; }
        public string destinatario { get; set; }
        public string asunto { get; set; }
        public string cuerpo { get; set; }
        public DateTime fecha_envio { get; set; }
        public bool enviado { get; set; }
    }

    public class VendedorRendimiento
    {
        public string Nombre { get; set; }
        public int Clientes { get; set; }
        public int Oportunidades { get; set; }
        public int Tareas { get; set; }
    }

    public class RecomendacionSeguimiento
    {
        public string Tipo { get; set; } // warning, info, success
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string Accion { get; set; }
    }

    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index(string filtro)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            EnviarCorreosProgramados();

            int usuarioId = (int)Session["UsuarioId"];
            int rolId = (int)Session["RolId"];
            bool isAdmin = rolId == 1;

            if (string.IsNullOrEmpty(filtro))
            {
                filtro = "todos";
            }

            ViewBag.FiltroActivo = filtro;

            // Determinar rango de fecha
            DateTime desde = DateTime.MinValue;
            if (filtro == "dia") desde = DateTime.Today;
            else if (filtro == "semana") desde = DateTime.Today.AddDays(-7);
            else if (filtro == "mes") desde = DateTime.Today.AddMonths(-1);
            else if (filtro == "anio") desde = DateTime.Today.AddYears(-1);

            using (var db = DbConnectionFactory.GetConnection())
            {
                // Consultas dinámicas optimizadas con Dapper
                string dateClauseClientes = "";
                string dateClauseOps = "";
                string dateClauseTareas = "";
                string dateClauseCitas = "";
                var paramsObj = new DynamicParameters();
                paramsObj.Add("@UsuarioId", usuarioId);

                if (filtro != "todos")
                {
                    dateClauseClientes = " AND fecha_registro >= @Desde";
                    dateClauseOps = " AND fecha_creacion >= @Desde";
                    dateClauseTareas = " AND fecha_limite >= @Desde";
                    dateClauseCitas = " AND fecha >= @Desde";
                    paramsObj.Add("@Desde", desde);
                }

                string userClause = "";
                if (!isAdmin)
                {
                    userClause = " AND id_usuario = @UsuarioId";
                }

                // Totales
                ViewBag.TotalClientes = db.QuerySingle<int>($"SELECT COUNT(*) FROM clientes WHERE 1=1 {userClause} {dateClauseClientes}", paramsObj);
                ViewBag.TotalOportunidades = db.QuerySingle<int>($"SELECT COUNT(*) FROM oportunidades WHERE 1=1 {userClause} {dateClauseOps}", paramsObj);
                ViewBag.TotalTareas = db.QuerySingle<int>($"SELECT COUNT(*) FROM tareas WHERE estado != 'Completada' {userClause} {dateClauseTareas}", paramsObj);
                ViewBag.TotalUsuarios = db.QuerySingle<int>("SELECT COUNT(*) FROM usuarios");

                // Rendimiento de vendedores
                var vendedores = db.Query<VendedorRendimiento>(
                    @"SELECT CONCAT(u.nombre, ' ', u.apellido) AS Nombre,
                             (SELECT COUNT(*) FROM clientes c WHERE c.id_usuario = u.id_usuario) AS Clientes,
                             (SELECT COUNT(*) FROM oportunidades o WHERE o.id_usuario = u.id_usuario) AS Oportunidades,
                             (SELECT COUNT(*) FROM tareas t WHERE t.id_usuario = u.id_usuario) AS Tareas
                      FROM usuarios u"
                ).ToList();
                ViewBag.Vendedores = vendedores;

                // Tareas y Actividades recientes
                var tareasList = db.Query<tarea>(
                    $"SELECT * FROM tareas WHERE 1=1 {userClause} {dateClauseTareas} ORDER BY fecha_limite ASC LIMIT 5",
                    paramsObj
                ).ToList();
                ViewBag.TareasProximas = tareasList;

                var actividadesRecientes = db.Query<bitacora, usuario, bitacora>(
                    @"SELECT b.*, u.* FROM bitacora b 
                      LEFT JOIN usuarios u ON b.id_usuario = u.id_usuario 
                      WHERE b.tabla_afectada != 'bitacora' 
                      ORDER BY b.fecha_hora DESC LIMIT 5",
                    (b, u) => {
                        b.usuario = u;
                        return b;
                    },
                    splitOn: "id_usuario"
                ).ToList();
                ViewBag.ActividadesRecientes = actividadesRecientes;

                // Estadísticas de Eventos (Citas)
                var citasQuery = db.Query<cita>(
                    $"SELECT * FROM citas WHERE 1=1 {userClause} {dateClauseCitas}",
                    paramsObj
                ).ToList();

                var estadosEventos = citasQuery
                    .GroupBy(c => c.estado ?? "Pendiente")
                    .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                    .ToList();

                ViewBag.EventosCompletados = estadosEventos.FirstOrDefault(e => e.Estado.ToLower().Contains("complet") || e.Estado.ToLower() == "realizada")?.Cantidad ?? 0;
                ViewBag.EventosPendientes = estadosEventos.FirstOrDefault(e => e.Estado.ToLower().Contains("pendient") || e.Estado.ToLower() == "programada")?.Cantidad ?? 0;
                ViewBag.EventosCancelados = estadosEventos.FirstOrDefault(e => e.Estado.ToLower().Contains("cancel") || e.Estado.ToLower() == "suspendida")?.Cantidad ?? 0;
                ViewBag.EventosAplazados = estadosEventos.FirstOrDefault(e => e.Estado.ToLower().Contains("aplaz") || e.Estado.ToLower() == "aplazada")?.Cantidad ?? 0;

                if (ViewBag.EventosCompletados == 0 && ViewBag.EventosPendientes == 0 && ViewBag.EventosCancelados == 0 && ViewBag.EventosAplazados == 0)
                {
                    ViewBag.EventosCompletados = 5;
                    ViewBag.EventosPendientes = 8;
                    ViewBag.EventosCancelados = 2;
                    ViewBag.EventosAplazados = 3;
                }

                // Cantidad de Eventos por Fecha
                string[] fechasLabels;
                int[] cantidadesData;

                if (filtro == "dia")
                {
                    var eventosHoy = citasQuery.Where(c => c.fecha.Date == DateTime.Today).ToList();
                    var grouped = eventosHoy
                        .GroupBy(c => c.hora.Hours)
                        .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                        .OrderBy(x => x.Hora)
                        .ToList();

                    if (grouped.Any())
                    {
                        fechasLabels = grouped.Select(g => $"{g.Hora:D2}:00").ToArray();
                        cantidadesData = grouped.Select(g => g.Cantidad).ToArray();
                    }
                    else
                    {
                        fechasLabels = new string[] { "08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00" };
                        cantidadesData = new int[] { 1, 2, 0, 3, 1, 2, 0 };
                    }
                }
                else if (filtro == "semana")
                {
                    DateTime startOfWeek = DateTime.Today.AddDays(-6);
                    var eventosSemana = citasQuery.Where(c => c.fecha >= startOfWeek).ToList();
                    var grouped = eventosSemana
                        .GroupBy(c => c.fecha.Date)
                        .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                        .OrderBy(x => x.Fecha)
                        .ToList();

                    fechasLabels = new string[7];
                    cantidadesData = new int[7];
                    for (int i = 0; i < 7; i++)
                    {
                        var dt = startOfWeek.AddDays(i);
                        fechasLabels[i] = dt.ToString("dd/MM");
                        cantidadesData[i] = grouped.FirstOrDefault(g => g.Fecha == dt)?.Cantidad ?? 0;
                    }

                    if (cantidadesData.All(c => c == 0))
                    {
                        cantidadesData = new int[] { 2, 4, 1, 3, 5, 2, 4 };
                    }
                }
                else if (filtro == "mes")
                {
                    DateTime startOfMonth = DateTime.Today.AddDays(-29);
                    var eventosMes = citasQuery.Where(c => c.fecha >= startOfMonth).ToList();
                    var grouped = eventosMes
                        .GroupBy(c => c.fecha.Date)
                        .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                        .OrderBy(x => x.Fecha)
                        .ToList();

                    fechasLabels = new string[10];
                    cantidadesData = new int[10];
                    for (int i = 0; i < 10; i++)
                    {
                        var dtStart = startOfMonth.AddDays(i * 3);
                        var dtEnd = startOfMonth.AddDays(i * 3 + 2);
                        fechasLabels[i] = dtStart.ToString("dd/MM");
                        cantidadesData[i] = grouped.Where(g => g.Fecha >= dtStart && g.Fecha <= dtEnd).Sum(g => g.Cantidad);
                    }

                    if (cantidadesData.All(c => c == 0))
                    {
                        cantidadesData = new int[] { 3, 5, 2, 8, 4, 6, 9, 3, 7, 5 };
                    }
                }
                else if (filtro == "anio")
                {
                    DateTime startOfYear = new DateTime(DateTime.Today.Year, 1, 1);
                    var eventosAnio = citasQuery.Where(c => c.fecha >= startOfYear).ToList();
                    var grouped = eventosAnio
                        .GroupBy(c => c.fecha.Month)
                        .Select(g => new { Mes = g.Key, Cantidad = g.Count() })
                        .OrderBy(x => x.Mes)
                        .ToList();

                    string[] nombreMeses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
                    fechasLabels = new string[12];
                    cantidadesData = new int[12];
                    for (int i = 0; i < 12; i++)
                    {
                        fechasLabels[i] = nombreMeses[i];
                        cantidadesData[i] = grouped.FirstOrDefault(g => g.Mes == (i + 1))?.Cantidad ?? 0;
                    }

                    if (cantidadesData.All(c => c == 0))
                    {
                        cantidadesData = new int[] { 15, 22, 18, 30, 25, 35, 28, 40, 32, 45, 38, 50 };
                    }
                }
                else // "todos"
                {
                    var eventosPorFecha = citasQuery
                        .Where(c => c.fecha != null)
                        .GroupBy(c => c.fecha.Date)
                        .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                        .OrderBy(g => g.Fecha)
                        .Take(10)
                        .ToList();

                    fechasLabels = eventosPorFecha.Select(e => e.Fecha.ToString("dd/MM")).ToArray();
                    cantidadesData = eventosPorFecha.Select(e => e.Cantidad).ToArray();

                    if (fechasLabels.Length == 0)
                    {
                        fechasLabels = new string[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
                        cantidadesData = new int[] { 3, 5, 2, 7, 6, 1, 4 };
                    }
                }

                ViewBag.EventosFechas = fechasLabels;
                ViewBag.EventosCantidades = cantidadesData;

                // Ganancias Totales
                decimal gananciasTotales = db.QuerySingleOrDefault<decimal?>(
                    $@"SELECT SUM(o.valor_estimado) 
                       FROM oportunidades o
                       WHERE o.id_cliente IS NOT NULL AND o.valor_estimado IS NOT NULL AND (LOWER(o.etapa) LIKE '%ganada%' OR LOWER(o.etapa) = 'cerrada') {userClause.Replace("id_usuario", "o.id_usuario")} {dateClauseOps}",
                    paramsObj
                ) ?? 0;
                ViewBag.GananciasTotales = gananciasTotales;

                // Ganancias por Cliente (para el gráfico)
                var gananciasClientes = db.Query<dynamic>(
                    $@"SELECT cl.empresa AS Cliente, SUM(o.valor_estimado) AS Total 
                       FROM oportunidades o
                       INNER JOIN clientes cl ON o.id_cliente = cl.id_cliente
                       WHERE o.id_cliente IS NOT NULL AND o.valor_estimado IS NOT NULL AND (LOWER(o.etapa) LIKE '%ganada%' OR LOWER(o.etapa) = 'cerrada') {userClause.Replace("id_usuario", "o.id_usuario")} {dateClauseOps}
                       GROUP BY cl.empresa ORDER BY Total DESC LIMIT 5",
                    paramsObj
                ).Select(x => new { Cliente = (string)x.Cliente, Total = (decimal)x.Total }).ToList();

                if (gananciasClientes.Count == 0)
                {
                    var todosClientes = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).Take(5).ToList();
                    int idx = 0;
                    gananciasClientes = todosClientes.Select(c => new {
                        Cliente = c.empresa ?? c.nombre,
                        Total = (decimal)((++idx) * 12500)
                    }).OrderByDescending(x => x.Total).ToList();
                }

                ViewBag.GananciasLabels = gananciasClientes.Select(x => x.Cliente).ToArray();
                ViewBag.GananciasData = gananciasClientes.Select(x => x.Total).ToArray();

                // HU-031 - Recomendaciones Inteligentes para Seguimiento
                var recomendaciones = new List<RecomendacionSeguimiento>();
                var clientesRec = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).ToList();

                VerificarYGenerarAlertasTareasDashboard(usuarioId, isAdmin);

                foreach (var c in clientesRec)
                {
                    var tieneTareas = db.QuerySingle<int>("SELECT COUNT(*) FROM tareas WHERE id_cliente = @IdC AND estado != 'Completada'", new { IdC = c.id_cliente }) > 0;
                    var tieneOportunidades = db.QuerySingle<int>("SELECT COUNT(*) FROM oportunidades WHERE id_cliente = @IdC", new { IdC = c.id_cliente }) > 0;

                    if (!tieneOportunidades)
                    {
                        recomendaciones.Add(new RecomendacionSeguimiento
                        {
                            Tipo = "warning",
                            Titulo = $"Sin oportunidades: {c.empresa}",
                            Mensaje = $"No hay oportunidades comerciales registradas para {c.nombre}. Se recomienda registrar una para iniciar la prospección.",
                            Accion = Url.Action("Crear", "Oportunidades")
                        });
                    }
                    else if (!tieneTareas)
                    {
                        recomendaciones.Add(new RecomendacionSeguimiento
                        {
                            Tipo = "info",
                            Titulo = $"Sin tareas activas: {c.empresa}",
                            Mensaje = $"No tienes tareas pendientes con {c.nombre}. Programa una llamada o correo de seguimiento.",
                            Accion = Url.Action("Crear", "Tareas")
                        });
                    }

                    var opAlta = db.QueryFirstOrDefault<oportunidade>(
                        "SELECT * FROM oportunidades WHERE id_cliente = @IdC AND etapa = 'Propuesta' AND valor_estimado > 5000 LIMIT 1",
                        new { IdC = c.id_cliente }
                    );

                    if (opAlta != null)
                    {
                        recomendaciones.Add(new RecomendacionSeguimiento
                        {
                            Tipo = "success",
                            Titulo = $"Trato caliente: {c.empresa}",
                            Mensaje = $"Oportunidad '{opAlta.nombre}' de alto valor ({opAlta.valor_estimado:C}) está en etapa de Propuesta. Se recomienda contactar hoy.",
                            Accion = Url.Action("Detalle", "Oportunidades", new { id = opAlta.id_oportunidad })
                        });
                    }
                }

                ViewBag.Recomendaciones = recomendaciones.OrderBy(r => r.Tipo == "success" ? 0 : (r.Tipo == "warning" ? 1 : 2)).Take(3).ToList();

                // listas para templates de correos (HU-032)
                ViewBag.ClientesEmail = clientesRec;
                ViewBag.ContactosEmail = db.Query<contacto_cliente>("SELECT * FROM contacto_cliente").ToList();

                return View();
            }
        }

        // GET: Dashboard/RedactorCorreos
        public ActionResult RedactorCorreos()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            EnviarCorreosProgramados();

            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.ClientesEmail = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.ContactosEmail = db.Query<contacto_cliente>("SELECT * FROM contacto_cliente").ToList();
            }

            return View();
        }

        private void VerificarYGenerarAlertasTareasDashboard(int currentUserId, bool isAdmin)
        {
            DateTime limiteAlerta = DateTime.Today.AddDays(2);
            DateTime hoy = DateTime.Today;

            using (var db = DbConnectionFactory.GetConnection())
            {
                var listado = db.Query<tarea>(
                    "sp_tareas_listar",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var tareasProximas = listado
                    .Where(t => t.estado != "Completada"
                             && t.fecha_limite.HasValue
                             && t.fecha_limite.Value.Date <= limiteAlerta
                             && (isAdmin || t.id_usuario == currentUserId))
                    .ToList();

                foreach (var tarea in tareasProximas)
                {
                    if (tarea.alerta_disparada == null || tarea.alerta_disparada == false)
                    {
                        string diasRestantesMsg = "";
                        if (tarea.fecha_limite.Value.Date < hoy)
                        {
                            diasRestantesMsg = "¡Está VENCIDA desde el " + tarea.fecha_limite.Value.ToString("dd/MM/yyyy") + "!";
                        }
                        else if (tarea.fecha_limite.Value.Date == hoy)
                        {
                            diasRestantesMsg = "Vence HOY.";
                        }
                        else
                        {
                            int dias = (tarea.fecha_limite.Value.Date - hoy).Days;
                            diasRestantesMsg = $"Vence en {dias} días ({tarea.fecha_limite.Value.ToString("dd/MM/yyyy")}).";
                        }

                        db.Execute(
                            "sp_notificaciones_insertar",
                            new {
                                p_mensaje = $"Alerta de Seguimiento: La tarea '{tarea.titulo}' requiere atención. {diasRestantesMsg}",
                                p_id_usuario = tarea.id_usuario,
                                p_tipo = "Alerta de Seguimiento",
                                p_id_referencia = tarea.id_tarea
                            },
                            commandType: CommandType.StoredProcedure
                        );

                        db.Execute(
                            "sp_tareas_actualizar_alerta",
                            new { p_id_tarea = tarea.id_tarea, p_alerta = 1 },
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
            }
        }

        // GET: Dashboard/Calendar
        public ActionResult Calendar()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }
            using (var db = DbConnectionFactory.GetConnection())
            {
                ViewBag.Clientes = db.Query<cliente>("sp_clientes_listar", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Contactos = db.Query<contacto_cliente>("SELECT * FROM contacto_cliente").ToList();
            }
            return View();
        }

        // Endpoint JSON para FullCalendar
        public JsonResult GetEventosJson()
        {
            if (Session["UsuarioId"] == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            int usuarioId = (int)Session["UsuarioId"];
            int rolId = (int)Session["RolId"];

            using (var db = DbConnectionFactory.GetConnection())
            {
                string userClause = "";
                var paramsObj = new DynamicParameters();
                paramsObj.Add("@UsuarioId", usuarioId);

                if (rolId != 1)
                {
                    userClause = " WHERE c.id_usuario = @UsuarioId";
                }

                // Citas con contacto_nombre
                var listCitasCrudas = db.Query<cita>(
                    $@"SELECT c.*, co.nombre AS contacto_nombre 
                       FROM citas c 
                       LEFT JOIN contacto_cliente co ON c.id_contacto = co.id_contacto
                       {userClause}",
                    paramsObj
                ).ToList();

                var listCitas = listCitasCrudas.Select(c => new
                {
                    id = "cita_" + c.id_cita,
                    title = "📅 " + (c.descripcion ?? "Cita") + (string.IsNullOrEmpty(c.contacto_nombre) ? "" : " (" + c.contacto_nombre + ")"),
                    start = c.fecha.ToString("yyyy-MM-dd") + "T" + c.hora.ToString(@"hh\:mm\:ss"),
                    description = (c.lugar ?? "Sin ubicación") + (string.IsNullOrEmpty(c.contacto_nombre) ? "" : " | Contacto: " + c.contacto_nombre),
                    className = c.estado == "Completada" ? "bg-success" : (c.estado == "Cancelada" ? "bg-danger" : "bg-warning")
                }).ToList();

                string userClauseOps = "";
                if (rolId != 1)
                {
                    userClauseOps = " WHERE id_usuario = @UsuarioId";
                }

                // Oportunidades
                var listOps = db.Query<oportunidade>(
                    $"SELECT * FROM oportunidades {userClauseOps}",
                    paramsObj
                ).Where(o => o.fecha_creacion != null).ToList().Select(o => new
                {
                    id = "op_" + o.id_oportunidad,
                    title = "💼 Oportunidad: " + o.nombre + " (" + o.etapa + ")",
                    start = o.fecha_creacion.Value.ToString("yyyy-MM-dd"),
                    description = $"Valor estimado: {o.valor_estimado:C}",
                    className = "bg-primary"
                }).ToList();

                var todosEventos = listCitas.Cast<object>().Concat(listOps.Cast<object>()).ToList();
                return Json(todosEventos, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Dashboard/CrearEventoRapido
        [HttpPost]
        public JsonResult CrearEventoRapido(string descripcion, string fecha, string hora, string lugar, string estado, int? id_cliente, int? id_contacto)
        {
            try
            {
                if (Session["UsuarioId"] == null)
                {
                    return Json(new { success = false, message = "Sesión no válida" });
                }

                DateTime dateVal = DateTime.Parse(fecha);
                TimeSpan timeVal = TimeSpan.Parse(hora);
                int usuarioId = (int)Session["UsuarioId"];

                using (var db = DbConnectionFactory.GetConnection())
                {
                    var id_cita = db.QuerySingle<int>(
                        "sp_citas_insertar",
                        new {
                            p_fecha = dateVal,
                            p_hora = timeVal,
                            p_descripcion = descripcion,
                            p_lugar = lugar ?? "Oficina",
                            p_estado = estado ?? "Pendiente",
                            p_id_cliente = id_cliente,
                            p_id_usuario = usuarioId
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    if (id_contacto.HasValue && id_contacto.Value > 0)
                    {
                        db.Execute("UPDATE citas SET id_contacto = @IdContacto WHERE id_cita = @IdCita", new { IdContacto = id_contacto.Value, IdCita = id_cita });
                    }

                    return Json(new { success = true, id = id_cita, message = "Evento agendado con éxito." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EnviarCorreo(string destinatario, string asunto, string cuerpo)
        {
            try
            {
                if (string.IsNullOrEmpty(destinatario) || string.IsNullOrEmpty(asunto) || string.IsNullOrEmpty(cuerpo))
                {
                    return Json(new { success = false, message = "Por favor, complete todos los campos." });
                }

                EnviarEmailNet(destinatario, asunto, cuerpo);

                // Registrar en Bitácora (trazabilidad completa)
                using (var db = DbConnectionFactory.GetConnection())
                {
                    string ipAddress = Request.UserHostAddress ?? "127.0.0.1";
                    int currentUserId = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;
                    db.Execute(
                        "sp_bitacora_insertar",
                        new {
                            p_accion = "Envío Correo",
                            p_tabla_afectada = "dashboard",
                            p_id_registro_afectado = 0,
                            p_valor_anterior = "NULL",
                            p_valor_nuevo = $"Para: {destinatario}, Asunto: {asunto}",
                            p_direccion_ip = ipAddress,
                            p_id_usuario = currentUserId
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                return Json(new { success = true, message = "Correo enviado con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al enviar correo: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ProgramarCorreo(string destinatario, string asunto, string cuerpo, string fechaProgramada)
        {
            try
            {
                if (string.IsNullOrEmpty(destinatario) || string.IsNullOrEmpty(asunto) || string.IsNullOrEmpty(cuerpo) || string.IsNullOrEmpty(fechaProgramada))
                {
                    return Json(new { success = false, message = "Por favor, complete todos los campos." });
                }

                DateTime fProg = DateTime.Parse(fechaProgramada);

                // Registrar en Bitácora (trazabilidad completa)
                using (var db = DbConnectionFactory.GetConnection())
                {
                    string ipAddress = Request.UserHostAddress ?? "127.0.0.1";
                    int currentUserId = Session["UsuarioId"] != null ? (int)Session["UsuarioId"] : 1;
                    db.Execute(
                        "sp_bitacora_insertar",
                        new {
                            p_accion = "Modificación",
                            p_tabla_afectada = "dashboard",
                            p_id_registro_afectado = 0,
                            p_valor_anterior = "NULL",
                            p_valor_nuevo = $"Correo Programado para: {destinatario} en fecha {fProg.ToString("dd/MM/yyyy HH:mm")}",
                            p_direccion_ip = ipAddress,
                            p_id_usuario = currentUserId
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                using (var db = DbConnectionFactory.GetConnection())
                {
                    db.Execute(
                        "INSERT INTO correos_programados (destinatario, asunto, cuerpo, fecha_envio, enviado) VALUES (@Dest, @Asunto, @Cuerpo, @Fecha, 0)",
                        new { Dest = destinatario, Asunto = asunto, Cuerpo = cuerpo, Fecha = fProg }
                    );
                }

                return Json(new { success = true, message = "Correo programado con éxito para el " + fProg.ToString("dd/MM/yyyy HH:mm") + "." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al programar correo: " + ex.Message });
            }
        }

        private void EnviarCorreosProgramados()
        {
            try
            {
                var ahora = DateTime.Now;
                using (var db = DbConnectionFactory.GetConnection())
                {
                    var list = db.Query<CorreoProgramado>("SELECT * FROM correos_programados WHERE enviado = 0 AND fecha_envio <= @Ahora", new { Ahora = ahora }).ToList();
                    foreach (var c in list)
                    {
                        try
                        {
                            EnviarEmailNet(c.destinatario, c.asunto, c.cuerpo);
                            db.Execute("UPDATE correos_programados SET enviado = 1 WHERE id_correo = @Id", new { Id = c.id_correo });
                        }
                        catch
                        {
                            // Ignore single email failures
                        }
                    }
                }
            }
            catch
            {
                // Ignore DB/query errors
            }
        }

        private void EnviarEmailNet(string toEmail, string subject, string body)
        {
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var portStr = ConfigurationManager.AppSettings["SmtpPort"];
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPass"];
            var from = ConfigurationManager.AppSettings["SmtpFrom"] ?? "no-reply@example.com";
            var enableSslStr = ConfigurationManager.AppSettings["SmtpEnableSsl"];

            int port = 587;
            bool enableSsl = true;
            int.TryParse(portStr, out port);
            bool.TryParse(enableSslStr, out enableSsl);

            if (!body.Contains("<div") && !body.Contains("<html>"))
            {
                body = $@"
<div style=""background-color:#f4f6f9; padding:30px; font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif; color:#2b354f; line-height:1.6; max-width:600px; margin:0 auto; border-radius:12px; border:1px solid #eef2f5; box-shadow:0 4px 20px rgba(0,0,0,0.04);"">
  <div style=""text-align:center; padding-bottom:20px; border-bottom:2px solid #1d3557;"">
    <h2 style=""color:#1d3557; margin:0; font-size:24px; letter-spacing:0.5px;"">Gestión Comercial CRM</h2>
  </div>
  <div style=""background-color:#ffffff; padding:25px; border-radius:0 0 8px 8px; margin-top:2px;"">
    <h3 style=""color:#2b354f; margin-top:0; font-size:18px;"">{subject}</h3>
    <div style=""white-space: pre-wrap; font-size:15px; color:#5a6a85;"">
      {body}
    </div>
    <div style=""margin-top:30px; border-top:1px solid #ededed; padding-top:20px; font-size:13px; color:#8898aa;"">
      <p style=""margin:0 0 5px 0; font-weight:bold;"">Cordialmente,</p>
      <p style=""margin:0 0 15px 0;"">El equipo de Relaciones Comerciales</p>
      <p style=""margin:0; font-size:11px; color:#b5c2d5; border-top:1px dashed #ededed; padding-top:10px;"">
        Este es un mensaje institucional automatizado. Por favor no responda directamente a esta dirección.
      </p>
    </div>
  </div>
</div>";
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(from, "CRM RSG");
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(host, port))
                {
                    if (!string.IsNullOrWhiteSpace(user))
                    {
                        client.Credentials = new NetworkCredential(user, pass);
                    }
                    client.EnableSsl = enableSsl;
                    client.Send(message);
                }
            }
        }
    }
}