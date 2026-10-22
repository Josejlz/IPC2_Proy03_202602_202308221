using System.Text.Json;
using System.Text;

namespace Proy3Frontend.Servicios
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public string UrlBase => _http.BaseAddress?.ToString() ?? "";


        //deserializa respuesta json
        public async Task<RespuestaApi<T>> GetAsync<T>(string ruta, params (string Clave, string? Valor)[] parametros)
        {
            IEnumerable<string> partes = parametros
                .Where(p => !string.IsNullOrWhiteSpace(p.Valor))
                .Select(p=>p.Clave + "=" + Uri.EscapeDataString(p.Valor!.Trim()));

            string url = ruta;
            if (partes.Any())
            {
                url += "?" + string.Join("&", partes);
            }

            RespuestaApi<string> cruda = await EjecutarAsync(() => _http.GetAsync(url));

            if (!cruda.Ok)
            {
                return RespuestaApi<T>.Falla(cruda.Error??"Error desconocido.");
            }

            try
            {
                T? valor = JsonSerializer.Deserialize<T>(cruda.Valor!, _opciones);
                if (valor == null)
                {
                    return RespuestaApi<T>.Falla(cruda.Error ?? "Error Desconocido");
                }
                return RespuestaApi<T>.Correcto(valor);
            }
            catch (JsonException)
            {
                return RespuestaApi<T>.Falla("La respuesta de la API no tiene el formato esperado.");
            }

        }

        //post con json sin modificar

        public Task<RespuestaApi<string>> PostJsonAsync(string ruta, string json)
        {
            return EjecutarAsync(()=>_http.PostAsync(ruta, new StringContent(json, Encoding.UTF8, "application/json")));
        }

        // para post sin cuerpo

        public Task<RespuestaApi<string>> PostAsync(string ruta)
        {
            return EjecutarAsync(()=>_http.PostAsync(ruta, null));
        }

        private async Task<RespuestaApi<string>> EjecutarAsync(Func<Task<HttpResponseMessage>> peticion)
        {
            try
            {
                using HttpResponseMessage respuesta = await peticion();
                string cuerpo = await respuesta.Content.ReadAsStringAsync();
                if (respuesta.IsSuccessStatusCode)
                {
                    return RespuestaApi<string>.Correcto(cuerpo);
                }
                return RespuestaApi<string>.Falla(ExtraerError(cuerpo, (int)respuesta.StatusCode));
            }
            catch (HttpRequestException)
            {
                return RespuestaApi<string>.Falla("No se pudo conecctar con la API");
            }
            catch (TaskCanceledException)
            {
                return RespuestaApi<string>.Falla("Se tardó en responder la API.");
            }
        }


        public static string ExtraerError(string cuerpo, int codigo)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(cuerpo);
                if ((doc.RootElement.ValueKind == JsonValueKind.Object) && (doc.RootElement.TryGetProperty("error", out JsonElement error)) &&
                    (error.ValueKind == JsonValueKind.String))
                {
                    return error.GetString() ?? ("Error " + codigo);
                }
            }
            catch (JsonException)
            {
                // continua
            }
            return "Respuesta codigo: " + codigo + ".";
        }
    }
}
