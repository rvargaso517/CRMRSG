using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using CRMRSG.EntityFramework;
using CRMRSG.Models;
using ExcelDataReader;
using Dapper;

namespace CRMRSG.Controllers
{
    public class ImportacionClientesController : Controller
    {
        private readonly CRM_RSGEntities db = new CRM_RSGEntities();

        // GET: ImportacionClientes
        public ActionResult Index()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }
            return View(new ImportacionClientesViewModel());
        }

        // POST: ImportacionClientes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ImportacionClientesViewModel modelo)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Autenticacion");
            }

            if (modelo == null)
            {
                modelo = new ImportacionClientesViewModel();
            }

            if (modelo.Errores == null)
            {
                modelo.Errores = new List<string>();
            }

            if (modelo.Archivo == null ||
                modelo.Archivo.ContentLength == 0)
            {
                ModelState.AddModelError(
                    "Archivo",
                    "Debe seleccionar un archivo para importar."
                );

                return View(modelo);
            }

            string extension = Path
                .GetExtension(modelo.Archivo.FileName)
                .ToLowerInvariant();

            string[] extensionesPermitidas =
            {
                ".csv",
                ".xlsx",
                ".xls"
            };

            if (!extensionesPermitidas.Contains(extension))
            {
                ModelState.AddModelError(
                    "Archivo",
                    "El archivo debe tener formato CSV, XLSX o XLS."
                );

                return View(modelo);
            }

            try
            {
                List<ClienteImportacionDto> filas;

                if (extension == ".csv")
                {
                    filas = LeerCsv(
                        modelo.Archivo.InputStream
                    );
                }
                else
                {
                    filas = LeerExcel(
                        modelo.Archivo.InputStream
                    );
                }

                if (filas == null || filas.Count == 0)
                {
                    ModelState.AddModelError(
                        "Archivo",
                        "El archivo no contiene registros para importar."
                    );

                    return View(modelo);
                }

                ProcesarClientes(filas, modelo);

                modelo.ProcesoFinalizado = true;

                return View(modelo);
            }
            catch (Exception ex)
            {
                modelo.TotalErrores++;
                modelo.ProcesoFinalizado = true;

                modelo.Errores.Add(
                    "No se pudo procesar el archivo: " +
                    ObtenerMensajeError(ex)
                );

                return View(modelo);
            }
        }

        private List<ClienteImportacionDto> LeerCsv(
            Stream archivo)
        {
            var filas =
                new List<ClienteImportacionDto>();

            if (archivo == null)
            {
                return filas;
            }

            if (archivo.CanSeek)
            {
                archivo.Position = 0;
            }

            using (var lector = new StreamReader(
                archivo,
                Encoding.UTF8,
                true,
                1024,
                true))
            {
                string primeraLinea =
                    lector.ReadLine();

                if (string.IsNullOrWhiteSpace(
                    primeraLinea))
                {
                    return filas;
                }

                char separador =
                    DetectarSeparador(primeraLinea);

                string[] encabezadosOriginales =
                    SepararLineaCsv(
                        primeraLinea,
                        separador
                    ).ToArray();

                string[] encabezados =
                    encabezadosOriginales
                        .Select(
                            NormalizarEncabezado
                        )
                        .ToArray();

                ValidarEncabezados(encabezados);

                int numeroFila = 1;

                while (!lector.EndOfStream)
                {
                    numeroFila++;

                    string linea =
                        lector.ReadLine();

                    if (string.IsNullOrWhiteSpace(
                        linea))
                    {
                        continue;
                    }

                    List<string> valoresLista =
                        SepararLineaCsv(
                            linea,
                            separador
                        );

                    string[] valores =
                        valoresLista.ToArray();

                    if (valores.All(
                        string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }

                    filas.Add(
                        CrearDto(
                            encabezados,
                            valores,
                            numeroFila
                        )
                    );
                }
            }

            return filas;
        }

        private char DetectarSeparador(
            string primeraLinea)
        {
            if (string.IsNullOrWhiteSpace(
                primeraLinea))
            {
                return ',';
            }

            int cantidadComas =
                primeraLinea.Count(
                    caracter =>
                        caracter == ','
                );

            int cantidadPuntoComa =
                primeraLinea.Count(
                    caracter =>
                        caracter == ';'
                );

            return cantidadPuntoComa >
                   cantidadComas
                ? ';'
                : ',';
        }

        private List<string> SepararLineaCsv(
            string linea,
            char separador)
        {
            var valores =
                new List<string>();

            var valorActual =
                new StringBuilder();

            bool dentroDeComillas = false;

            if (linea == null)
            {
                valores.Add(string.Empty);

                return valores;
            }

            for (int i = 0;
                 i < linea.Length;
                 i++)
            {
                char caracter = linea[i];

                if (caracter == '"')
                {
                    if (dentroDeComillas &&
                        i + 1 < linea.Length &&
                        linea[i + 1] == '"')
                    {
                        valorActual.Append('"');
                        i++;
                    }
                    else
                    {
                        dentroDeComillas =
                            !dentroDeComillas;
                    }
                }
                else if (
                    caracter == separador &&
                    !dentroDeComillas)
                {
                    valores.Add(
                        valorActual
                            .ToString()
                            .Trim()
                    );

                    valorActual.Clear();
                }
                else
                {
                    valorActual.Append(
                        caracter
                    );
                }
            }

            valores.Add(
                valorActual
                    .ToString()
                    .Trim()
            );

            return valores;
        }

        private List<ClienteImportacionDto> LeerExcel(
            Stream archivo)
        {
            var filas =
                new List<ClienteImportacionDto>();

            if (archivo == null)
            {
                return filas;
            }

            if (archivo.CanSeek)
            {
                archivo.Position = 0;
            }

            using (IExcelDataReader reader =
                ExcelReaderFactory.CreateReader(
                    archivo))
            {
                var configuracion =
                    new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable =
                            delegate
                            {
                                return new
                                    ExcelDataTableConfiguration
                                {
                                    UseHeaderRow =
                                            true
                                };
                            }
                    };

                DataSet dataSet =
                    reader.AsDataSet(
                        configuracion
                    );

                if (dataSet == null ||
                    dataSet.Tables.Count == 0)
                {
                    return filas;
                }

                DataTable tabla =
                    dataSet.Tables[0];

                if (tabla == null ||
                    tabla.Columns.Count == 0)
                {
                    return filas;
                }

                string[] encabezados =
                    tabla.Columns
                        .Cast<DataColumn>()
                        .Select(
                            columna =>
                                NormalizarEncabezado(
                                    columna.ColumnName
                                )
                        )
                        .ToArray();

                ValidarEncabezados(encabezados);

                int numeroFila = 1;

                foreach (DataRow fila
                    in tabla.Rows)
                {
                    numeroFila++;

                    string[] valores =
                        fila.ItemArray
                            .Select(
                                valor =>
                                    valor == null ||
                                    valor == DBNull.Value
                                        ? string.Empty
                                        : valor
                                            .ToString()
                                            .Trim()
                            )
                            .ToArray();

                    if (valores.All(
                        string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }

                    filas.Add(
                        CrearDto(
                            encabezados,
                            valores,
                            numeroFila
                        )
                    );
                }
            }

            return filas;
        }

        private void ValidarEncabezados(
            string[] encabezados)
        {
            if (encabezados == null ||
                encabezados.Length == 0)
            {
                throw new Exception(
                    "El archivo no contiene encabezados."
                );
            }

            string[] columnasObligatorias =
            {
                "nombre",
                "empresa",
                "correo"
            };

            List<string> faltantes =
                columnasObligatorias
                    .Where(
                        columna =>
                            !encabezados.Contains(
                                columna
                            )
                    )
                    .ToList();

            if (faltantes.Count > 0)
            {
                throw new Exception(
                    "Faltan las columnas obligatorias: " +
                    string.Join(
                        ", ",
                        faltantes
                    ) +
                    "."
                );
            }
        }

        private ClienteImportacionDto CrearDto(
            string[] encabezados,
            string[] valores,
            int numeroFila)
        {
            var datos =
                new Dictionary<string, string>(
                    StringComparer
                        .OrdinalIgnoreCase
                );

            for (int i = 0;
                 i < encabezados.Length;
                 i++)
            {
                string encabezado =
                    encabezados[i];

                if (string.IsNullOrWhiteSpace(
                    encabezado))
                {
                    continue;
                }

                string valor =
                    i < valores.Length &&
                    valores[i] != null
                        ? valores[i].Trim()
                        : string.Empty;

                datos[encabezado] = valor;
            }

            return new ClienteImportacionDto
            {
                NumeroFila = numeroFila,

                Nombre = ObtenerValor(
                    datos,
                    "nombre"
                ),

                Empresa = ObtenerValor(
                    datos,
                    "empresa"
                ),

                Correo = ObtenerValor(
                    datos,
                    "correo"
                ),

                Telefono = ObtenerValor(
                    datos,
                    "telefono"
                ),

                Direccion = ObtenerValor(
                    datos,
                    "direccion"
                ),

                Estado = ObtenerValor(
                    datos,
                    "estado"
                )
            };
        }

        private void ProcesarClientes(
            List<ClienteImportacionDto> filas,
            ImportacionClientesViewModel resultado)
        {
            if (resultado.Errores == null)
            {
                resultado.Errores = new List<string>();
            }

            int usuarioId = ObtenerUsuarioSesion();

            using (var conn = DbConnectionFactory.GetConnection())
            {
                foreach (ClienteImportacionDto fila in filas)
                {
                    resultado.TotalProcesados++;

                    try
                    {
                        List<string> erroresFila = ValidarFila(fila);

                        if (erroresFila.Count > 0)
                        {
                            resultado.TotalErrores++;
                            resultado.Errores.Add(
                                "Fila " + fila.NumeroFila + ": " + string.Join(" ", erroresFila)
                            );
                            continue;
                        }

                        string correoNormalizado = fila.Correo.Trim().ToLowerInvariant();

                        // Buscar por correo usando Dapper en MySQL
                        var clienteExistente = conn.QueryFirstOrDefault<cliente>(
                            "SELECT * FROM clientes WHERE correo = @Correo",
                            new { Correo = correoNormalizado }
                        );

                        if (clienteExistente == null)
                        {
                            var nuevoEstado = string.IsNullOrWhiteSpace(fila.Estado) ? "Activo" : fila.Estado.Trim();
                            
                            // Insertar usando el sp_clientes_insertar en MySQL
                            conn.Execute(
                                "sp_clientes_insertar",
                                new {
                                    p_nombre = fila.Nombre.Trim(),
                                    p_empresa = fila.Empresa.Trim(),
                                    p_telefono = LimpiarValor(fila.Telefono),
                                    p_correo = fila.Correo.Trim(),
                                    p_direccion = LimpiarValor(fila.Direccion),
                                    p_estado = nuevoEstado,
                                    p_id_usuario = usuarioId
                                },
                                commandType: CommandType.StoredProcedure
                            );

                            resultado.TotalCreados++;
                        }
                        else
                        {
                            var nuevoEstado = string.IsNullOrWhiteSpace(fila.Estado) ? clienteExistente.estado : fila.Estado.Trim();

                            // Actualizar usando el sp_clientes_actualizar en MySQL
                            conn.Execute(
                                "sp_clientes_actualizar",
                                new {
                                    p_id_cliente = clienteExistente.id_cliente,
                                    p_nombre = fila.Nombre.Trim(),
                                    p_empresa = fila.Empresa.Trim(),
                                    p_telefono = LimpiarValor(fila.Telefono),
                                    p_correo = fila.Correo.Trim(),
                                    p_direccion = LimpiarValor(fila.Direccion),
                                    p_estado = nuevoEstado,
                                    p_id_usuario = clienteExistente.id_usuario
                                },
                                commandType: CommandType.StoredProcedure
                            );

                            resultado.TotalActualizados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.TotalErrores++;
                        resultado.Errores.Add(
                            "Fila " + fila.NumeroFila + ": " + ObtenerMensajeError(ex)
                        );
                    }
                }
            }
        }

        private List<string> ValidarFila(
            ClienteImportacionDto fila)
        {
            var errores =
                new List<string>();

            if (fila == null)
            {
                errores.Add(
                    "La fila no contiene información."
                );

                return errores;
            }

            if (string.IsNullOrWhiteSpace(
                fila.Nombre))
            {
                errores.Add(
                    "El nombre es obligatorio."
                );
            }

            if (string.IsNullOrWhiteSpace(
                fila.Empresa))
            {
                errores.Add(
                    "La empresa es obligatoria."
                );
            }

            if (string.IsNullOrWhiteSpace(
                fila.Correo))
            {
                errores.Add(
                    "El correo es obligatorio."
                );
            }
            else if (!EsCorreoValido(
                fila.Correo))
            {
                errores.Add(
                    "El correo no tiene un formato válido."
                );
            }

            return errores;
        }

        private bool EsCorreoValido(
            string correo)
        {
            if (string.IsNullOrWhiteSpace(
                correo))
            {
                return false;
            }

            try
            {
                string correoLimpio =
                    correo.Trim();

                var direccion =
                    new System.Net.Mail
                        .MailAddress(
                            correoLimpio
                        );

                return direccion.Address
                    .Equals(
                        correoLimpio,
                        StringComparison
                            .OrdinalIgnoreCase
                    );
            }
            catch
            {
                return false;
            }
        }

        private string NormalizarEncabezado(
            string encabezado)
        {
            if (string.IsNullOrWhiteSpace(
                encabezado))
            {
                return string.Empty;
            }

            string texto =
                encabezado
                    .Trim()
                    .ToLowerInvariant()
                    .Replace(" ", "_")
                    .Replace("-", "_");

            texto = QuitarAcentos(texto);

            switch (texto)
            {
                case "contacto":
                case "nombre_contacto":
                case "contacto_principal":
                    return "nombre";

                case "email":
                case "e_mail":
                case "correo_electronico":
                    return "correo";

                case "nombre_empresa":
                case "compania":
                    return "empresa";

                case "phone":
                case "numero_telefono":
                case "numero_de_telefono":
                    return "telefono";

                case "address":
                case "ubicacion":
                    return "direccion";

                case "estatus":
                    return "estado";

                default:
                    return texto;
            }
        }

        private string QuitarAcentos(
            string texto)
        {
            if (string.IsNullOrWhiteSpace(
                texto))
            {
                return string.Empty;
            }

            string normalizado =
                texto.Normalize(
                    NormalizationForm.FormD
                );

            var resultado =
                new StringBuilder();

            foreach (char caracter
                in normalizado)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo
                        .GetUnicodeCategory(
                            caracter
                        );

                if (categoria !=
                    UnicodeCategory
                        .NonSpacingMark)
                {
                    resultado.Append(
                        caracter
                    );
                }
            }

            return resultado
                .ToString()
                .Normalize(
                    NormalizationForm.FormC
                );
        }

        private string ObtenerValor(
            Dictionary<string, string> datos,
            string columna)
        {
            if (datos == null ||
                string.IsNullOrWhiteSpace(
                    columna))
            {
                return string.Empty;
            }

            string valor;

            if (datos.TryGetValue(
                columna,
                out valor))
            {
                return valor;
            }

            return string.Empty;
        }

        private string LimpiarValor(
            string valor)
        {
            if (string.IsNullOrWhiteSpace(
                valor))
            {
                return null;
            }

            return valor.Trim();
        }

        private int ObtenerUsuarioSesion()
        {
            if (Session["UsuarioId"] != null)
            {
                int usuarioId;

                if (int.TryParse(
                    Session["UsuarioId"].ToString(),
                    out usuarioId))
                {
                    return usuarioId;
                }
            }

            // Fallback al primer usuario registrado en MySQL si la sesión no está disponible
            using (var conn = DbConnectionFactory.GetConnection())
            {
                var primerUsuarioId = conn.QueryFirstOrDefault<int?>("SELECT id_usuario FROM usuarios ORDER BY id_usuario ASC LIMIT 1");
                return primerUsuarioId ?? 1;
            }
        }

        private string ObtenerMensajeError(
            Exception excepcion)
        {
            if (excepcion == null)
            {
                return
                    "Ocurrió un error desconocido.";
            }

            Exception errorActual =
                excepcion;

            while (
                errorActual.InnerException != null)
            {
                errorActual =
                    errorActual.InnerException;
            }

            return errorActual.Message;
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        private class ClienteImportacionDto
        {
            public int NumeroFila { get; set; }

            public string Nombre { get; set; }

            public string Empresa { get; set; }

            public string Correo { get; set; }

            public string Telefono { get; set; }

            public string Direccion { get; set; }

            public string Estado { get; set; }
        }
    }
}