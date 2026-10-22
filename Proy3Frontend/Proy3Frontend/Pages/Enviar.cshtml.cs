using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proy3Frontend.Servicios;
using System.Text;
using System.Text.Json;

namespace Proy3Frontend.Pages
{
    public class EnviarModel : PageModel
    {
        private const int MaxVistaPrevia = 1500;
        private readonly ApiClient _api;

        public EnviarModel(ApiClient api)
        {
            _api = api;
        }

        public class Aprobacion
        {
            public string Referencia { get; set; } = "";
            public string Codigo { get; set; } = "";
        }

        public class ResultadoArchivo
        {
            public string Nombre { get; set; } = "";
            public long Bytes { get; set; }
            public int Elementos { get; set; }
            public bool Ok { get; set; }
            public string? Error { get; set; }
            public string Tipo { get; set; } = "";
            public string Enviado { get; set; } = "";
            public string Respuesta { get; set; } = "";
            public List<KeyValuePair<string, string>> Resumen { get; set; } = new();
            public List<Aprobacion> Aprobaciones { get; set; } = new();
        }

        [BindProperty]
        public List<IFormFile> Archivos { get; set; } = new();
        public List<ResultadoArchivo> Resultados { get; } = new();

        public string? Error { get; private set; }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            if (Archivos == null || Archivos.Count == 0)
            {
                Error = "Selecciona al menos un archivo .json.";
                return;
            }

            foreach (IFormFile archivo in Archivos)
            {
                Resultados.Add(await ProcesarArchivoAsync(archivo));
            }
        }

        private async Task<ResultadoArchivo> ProcesarArchivoAsync(IFormFile archivo)
        {
            var resultado = new ResultadoArchivo { Nombre = archivo.FileName, Bytes = archivo.Length };

            string texto;
            using (var lector = new StreamReader(archivo.OpenReadStream(), Encoding.UTF8, true))
            {
                texto = await lector.ReadToEndAsync();
            }

            resultado.Enviado = texto.Length > MaxVistaPrevia
                ? texto.Substring(0, MaxVistaPrevia) + "\n... (vista previa recortada)"
                : texto;

            // Solo se verifica que sea JSON y se cuentan los elementos
            try
            {
                using JsonDocument doc = JsonDocument.Parse(texto);
                resultado.Elementos = doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.GetArrayLength()
                    : 1;
            }
            catch (JsonException)
            {
                resultado.Ok = false;
                resultado.Error = "El archivo no contiene un JSON valido; no se envio a la API.";
                return resultado;
            }

            RespuestaApi<string> respuesta = await _api.PostJsonAsync("/Procesar", texto);
            if (!respuesta.Ok)
            {
                resultado.Ok = false;
                resultado.Error = respuesta.Error;
                return resultado;
            }

            resultado.Ok = true;
            resultado.Respuesta = Formato.JsonChulo(respuesta.Valor!);
            ResumirRespuesta(resultado, respuesta.Valor!);
            return resultado;
        }

        private static void ResumirRespuesta(ResultadoArchivo resultado, string cuerpo)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(cuerpo);
                JsonElement raiz = doc.RootElement;

                if (raiz.TryGetProperty("resultadoAutorizaciones", out JsonElement ra))
                {
                    resultado.Tipo = "Solicitudes de autorización";
                    Agregar(resultado, "Autorizaciones aprobadas", ra, "cantidadAutorizacionesAprobadas");
                    Agregar(resultado, "Rechazadas por NIT emisor inexistente", ra, "cantidadAutorizacionesRechazadasPorNitEmisorInexistente");
                    Agregar(resultado, "Rechazadas por NIT receptor inexistente", ra, "cantidadAutorizacionesRechazadasPorNitReceptorInexistente");

                    if (ra.TryGetProperty("detalleAprobaciones", out JsonElement detalle) &&
                        detalle.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in detalle.EnumerateArray())
                        {
                            resultado.Aprobaciones.Add(new Aprobacion
                            {
                                Referencia = item.TryGetProperty("referencia", out JsonElement r) ? r.ToString() : "",
                                Codigo = item.TryGetProperty("codigoAprobacion", out JsonElement c) ? c.ToString() : ""
                            });
                        }
                    }
                }
                else if (raiz.TryGetProperty("CONTRIBUYENTES_NUEVOS", out _))
                {
                    resultado.Tipo = "Actualización del RTU";
                    Agregar(resultado, "Contribuyentes nuevos", raiz, "CONTRIBUYENTES_NUEVOS");
                    Agregar(resultado, "Contribuyentes actualizados", raiz, "CONTRIBUYENTES_ACTUALIZADOS");
                    Agregar(resultado, "NITs inválidos", raiz, "NITS_INVALIDOS");
                }
            }
            catch (JsonException)
            {
                //solo para no dejarlo vacio :v
            }
        }

        private static void Agregar(ResultadoArchivo resultado, string etiqueta, JsonElement objeto, string propiedad)
        {
            if (objeto.TryGetProperty(propiedad, out JsonElement valor))
            {
                resultado.Resumen.Add(new KeyValuePair<string, string>(etiqueta, valor.ToString()));
            }
        }

    }
}
