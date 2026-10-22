namespace Proy3Frontend.Models
{
    public class FacturaDetalle
    {
        public string Referencia { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string NitEmisor { get; set; } = "";
        public string NitReceptor { get; set; } = "";
        public decimal Valor { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public long CodigoAprobacion { get; set; }
    }

    public class ResumenFecha
    {
        public string Fecha { get; set; } = "";
        public int CantidadFacturas { get; set; }
        public decimal MontoTotal { get; set; }
    }

    public class ConsultaDatosResultado
    {
        public string Nit { get; set; } = "";
        public string? Nombre { get; set; }
        public string Tipo { get; set; } = "";
        public string Desde { get; set; } = "";
        public string Hasta { get; set; } = "";
        public List<ResumenFecha> PorFecha { get; set; } = new();
        public int TotalFacturas { get; set; }
        public decimal MontoTotal { get; set; }
        public List<FacturaDetalle> Facturas { get; set; } = new();
    }

    public class IvaPorNit
    {
        public string Nit { get; set; } = "";
        public string? Nombre { get; set; }
        public decimal IvaEmitido { get; set; }
        public decimal IvaRecibido { get; set; }
    }

    public class ResumenIvaResultado
    {
        public string Fecha { get; set; } = "";
        public List<IvaPorNit> Nits { get; set; } = new();
        public decimal TotalIva { get; set; }
    }

    public class ValorPorFecha
    {
        public string Fecha { get; set; } = "";
        public int CantidadFacturas { get; set; }
        public decimal Valor { get; set; }
    }

    public class ResumenRangoResultado
    {
        public string Desde { get; set; } = "";
        public string Hasta { get; set; } = "";
        public string Valores { get; set; } = "";
        public List<ValorPorFecha> Fechas { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class SerieGrafica
    {
        public string Nombre { get; set; } = "";
        public List<decimal> Valores { get; set; } = new();
    }

    public class DatosGrafica
    {
        public string Titulo { get; set; } = "";
        public string Tipo { get; set; } = "bar";
        public List<string> Etiquetas { get; set; } = new();
        public List<SerieGrafica> Series { get; set; } = new();
    }

    public class ErroresDia
    {
        public int NitEmisorInvalido { get; set; }
        public int NitReceptorInvalido { get; set; }
        public int NitEmisorInexistente { get; set; }
        public int NitReceptorInexistente { get; set; }
        public int IvaMalCalculado { get; set; }
        public int TotalMalCalculado { get; set; }
        public int ReferenciaDuplicada { get; set; }
        public int DatosInvalidos { get; set; }
    }

    public class EstadisticaDia
    {
        public string Fecha { get; set; } = "";
        public int CantidadFacturasRecibidas { get; set; }
        public ErroresDia Errores { get; set; } = new();
        public int CantidadFacturasSinErrores { get; set; }
        public int CantidadEmisores { get; set; }
        public int CantidadReceptores { get; set; }
    }

    public class EstadisticasResultado
    {
        public string Desde { get; set; } = "";
        public string Hasta { get; set; } = "";

        public List<EstadisticaDia> Dias { get; set; } = new();
    }

}
