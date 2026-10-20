using System.Text.Json.Serialization;


namespace Proy3Backend.Models
{

    public class Contribuyente
    {
        public string Nit { get; set; } = "";
        public string Nombre { get; set; } = "";
    }

    public enum TipoError
    {
        NitEmisorInvalido,
        NitReceptorInvalido,
        NitEmisorInexistente,
        NitReceptorInexistente,
        IvaMalCalculado,
        TotalMalCalculado,
        ReferenciaDuplicada,
        DatosInvalidos
    }


    public class Factura
    {
        public string Referencia { get; set; } = "";
        public DateTime Fecha { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string NitEmisor { get; set; } = "";
        public string NitReceptor { get; set; } = "";
        public decimal Valor { get; set; }
        public decimal? Iva { get; set; }
        public decimal? Total { get; set; }

        public long? CodigoAprobacion { get; set; }

        public List<TipoError> Errores { get; set; } = new();

        [JsonIgnore]
        public bool Aprobada => Errores.Count == 0;
    }

    public class CorrelativoDiario
    {
        public string Fecha { get; set; } = "";
        public long Ultimo { get; set;  }
    }

}
