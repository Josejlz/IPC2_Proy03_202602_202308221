using System.Text.RegularExpressions;

namespace Proy3Backend.Utilidades
{
    public class NitValidator
    {
        private static readonly Regex Formato = new(@"^[0-9]{1,20}[0-9K]\z", RegexOptions.Compiled);

        public static string? Normalizar(string? nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
            {
                return null;
            }

            return nit.Trim().ToUpperInvariant();
        }

        public static bool EsValido(string? nit)
        {
            nit = Normalizar(nit);
            if (nit == null || !Formato.IsMatch(nit)) return false;

            string cuerpo = nit.Substring(0, nit.Length - 1);
            char verificador = nit[nit.Length - 1];

            return CalcularVerificador(cuerpo) == verificador;
        }

        public static char CalcularVerificador(string cuerpo)
        {
            int suma = 0;
            int factor = 2;
            for (int i = cuerpo.Length - 1; i >= 0; i--, factor++)
            {
                suma += (cuerpo[i] - '0') * factor;
            }

            int resultado = (11 - (suma % 11)) % 11;
            return resultado == 10 ? 'K' : (char)('0' + resultado);
        }

    }
}
