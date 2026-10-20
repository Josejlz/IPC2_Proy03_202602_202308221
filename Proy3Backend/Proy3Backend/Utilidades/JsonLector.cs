using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Globalization;
using System.Text.Json;

namespace Proy3Backend.Utilidades

{
    public static class JsonLector
    {
        

        public static decimal? Numero(JsonElement elemento, string propiedad)
        {
            if (!TryObtener(elemento, propiedad, out JsonElement valor))
            {
                return null;
            }

            if (valor.ValueKind == JsonValueKind.Number)
            {
                if (valor.TryGetDecimal(out decimal d))
                {
                    return d;
                }
                return null;
            }

            if ((valor.ValueKind == JsonValueKind.String) && (decimal.TryParse(valor.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal s)))
            {
                return s;
            }

            return null;
        }


        public static bool Tiene(JsonElement elemento, string propiedad) =>
        TryObtener(elemento, propiedad, out _);

        public static string? Texto(JsonElement elemento, string propiedad)
        {
            if (!TryObtener(elemento, propiedad, out JsonElement valor)) return null;

            return valor.ValueKind switch
            {
                JsonValueKind.String => valor.GetString(),
                JsonValueKind.Number => valor.GetRawText(),
                _ => null
            };
        }

        private static bool TryObtener(JsonElement elemento, string propiedad, out JsonElement valor)
        {
            valor = default;
            if (elemento.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (JsonProperty p in elemento.EnumerateObject())
            {
                if (string.Equals(p.Name, propiedad, StringComparison.OrdinalIgnoreCase))
                {
                    valor = p.Value;
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<JsonElement> Elementos(JsonElement raiz)
        {
            if(raiz.ValueKind == JsonValueKind.Array){
                return raiz.EnumerateArray();
            }

            if (raiz.ValueKind == JsonValueKind.Object)
            {
                return new[] { raiz };
            }

            return Array.Empty<JsonElement>();
        }
    }
}
