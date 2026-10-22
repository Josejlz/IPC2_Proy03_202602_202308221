using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Proy3Frontend.Servicios
{
    public static class Formato
    {
        public static string Numero(decimal valor)
        {
            return valor.ToString("N2", CultureInfo.InvariantCulture);
        }

        public static string Dinero(decimal valor)
        {
            return "Q." + Numero(valor) ;
        }

        private static readonly JsonSerializerOptions Chulo = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string JsonChulo(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, Chulo);
            }
            catch (JsonException)
            {
                return json;
            }
        }
    }
}
