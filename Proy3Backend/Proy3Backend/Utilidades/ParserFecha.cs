using System.Globalization;
using System.Text.RegularExpressions;

namespace Proy3Backend.Utilidades
{
    public class ParserFecha
    {

        private static readonly Regex FechaHora = new(
        @"(?<![0-9])([0-9]{1,2})/([0-9]{1,2})/([0-9]{4})\s+([0-9]{1,2}):([0-9]{2})",
        RegexOptions.Compiled);

        public static bool TryExtraerFechaHora(string? texto, out DateTime fecha)
        {
            fecha = default;
            if (string.IsNullOrWhiteSpace(texto)) return false;

            Match m = FechaHora.Match(texto);
            if (!m.Success) return false;

            try
            {
                fecha = new DateTime(
                    int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture),
                    0);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Dia, mes u hora fuera de rango 
                return false;
            }
        }

        private static readonly string[] FormatosConsulta = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };

        public static bool TryParseConsulta(string? texto, out DateTime fecha)
        {
            return DateTime.TryParseExact(texto?.Trim(), FormatosConsulta,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
        }

        public static string Formatear(DateTime fecha) =>
            fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        public static string FormatearConHora(DateTime fecha) =>
            fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        public static DateTime AhoraGuatemala() => DateTime.UtcNow.AddHours(-6);

    }
}
