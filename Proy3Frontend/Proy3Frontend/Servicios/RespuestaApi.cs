namespace Proy3Frontend.Servicios
{
    public class RespuestaApi<T>
    {
        public bool Ok { get; private set; }
        public T? Valor { get; private set; }
        public string? Error { get; set; }

        public static RespuestaApi<T> Correcto(T valor)
        {
            return new RespuestaApi<T> { Ok = true, Valor = valor};
        }

        public static RespuestaApi<T> Falla(string error)
        {
            return new RespuestaApi<T> { Ok = false, Error = error };
        }
    }
}
