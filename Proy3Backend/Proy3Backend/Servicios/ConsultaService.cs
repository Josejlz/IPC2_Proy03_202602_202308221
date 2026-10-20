using Proy3Backend.Models;
using Proy3Backend.Utilidades;

namespace Proy3Backend.Servicios
{
    public class ConsultaService
    {
        private readonly AlmacenDatos _almacen;

        public ConsultaService(AlmacenDatos almacen)
        {
            _almacen = almacen;
        }

        // -------------------------------------------------------------------
        // Utilidades internas
        // -------------------------------------------------------------------

        /// <summary>Facturas autorizadas cuya fecha de documento cae en [desde, hasta] (copia, segura fuera del lock).</summary>
        private List<Factura> Aprobadas(DateTime desde, DateTime hasta)
        {
            lock (_almacen.Lock)
            {
                return _almacen.Facturas
                    .Where(f => f.Aprobada && f.Fecha.Date >= desde.Date && f.Fecha.Date <= hasta.Date)
                    .ToList();
            }
        }

        private string? NombreDe(string nit)
        {
            lock (_almacen.Lock)
            {
                return _almacen.Rtu.TryGetValue(nit, out Contribuyente? c) ? c.Nombre : null;
            }
        }

        // -------------------------------------------------------------------
        // /ConsultaDatos: facturas emitidas o recibidas por un NIT
        // -------------------------------------------------------------------

        public ConsultaDatosResultado ConsultaDatos(string nit, bool emitidas, DateTime desde, DateTime hasta)
        {
            List<Factura> facturas = Aprobadas(desde, hasta)
                .Where(f => emitidas ? f.NitEmisor == nit : f.NitReceptor == nit)
                .OrderBy(f => f.Fecha)
                .ThenBy(f => f.CodigoAprobacion)
                .ToList();

            var resultado = new ConsultaDatosResultado
            {
                Nit = nit,
                Nombre = NombreDe(nit),
                Tipo = emitidas ? "emitidas" : "recibidas",
                Desde = ParserFecha.Formatear(desde),
                Hasta = ParserFecha.Formatear(hasta),
                TotalFacturas = facturas.Count,
                MontoTotal = facturas.Sum(f => f.Total ?? 0m)
            };

            resultado.PorFecha = facturas
                .GroupBy(f => f.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new ResumenFecha
                {
                    Fecha = ParserFecha.Formatear(g.Key),
                    CantidadFacturas = g.Count(),
                    MontoTotal = g.Sum(f => f.Total ?? 0m)
                })
                .ToList();

            resultado.Facturas = facturas
                .Select(f => new FacturaDetalle
                {
                    Referencia = f.Referencia,
                    Fecha = ParserFecha.FormatearConHora(f.Fecha),
                    NitEmisor = f.NitEmisor,
                    NitReceptor = f.NitReceptor,
                    Valor = f.Valor,
                    Iva = f.Iva ?? 0m,
                    Total = f.Total ?? 0m,
                    CodigoAprobacion = f.CodigoAprobacion ?? 0
                })
                .ToList();

            return resultado;
        }

        // -------------------------------------------------------------------
        // /ResumenIva: IVA emitido y recibido por NIT en una fecha
        // -------------------------------------------------------------------

        public ResumenIvaResultado ResumenIva(DateTime fecha)
        {
            List<Factura> facturas = Aprobadas(fecha, fecha);

            List<string> nits = facturas.Select(f => f.NitEmisor)
                .Concat(facturas.Select(f => f.NitReceptor))
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            var resultado = new ResumenIvaResultado
            {
                Fecha = ParserFecha.Formatear(fecha),
                TotalIva = facturas.Sum(f => f.Iva ?? 0m)
            };

            foreach (string nit in nits)
            {
                resultado.Nits.Add(new IvaPorNit
                {
                    Nit = nit,
                    Nombre = NombreDe(nit),
                    IvaEmitido = facturas.Where(f => f.NitEmisor == nit).Sum(f => f.Iva ?? 0m),
                    IvaRecibido = facturas.Where(f => f.NitReceptor == nit).Sum(f => f.Iva ?? 0m)
                });
            }

            return resultado;
        }

        // -------------------------------------------------------------------
        // /ResumenRango: valor autorizado por fecha (total con IVA o sin IVA)
        // -------------------------------------------------------------------

        public ResumenRangoResultado ResumenRango(DateTime desde, DateTime hasta, bool sinIva)
        {
            List<Factura> facturas = Aprobadas(desde, hasta);

            var resultado = new ResumenRangoResultado
            {
                Desde = ParserFecha.Formatear(desde),
                Hasta = ParserFecha.Formatear(hasta),
                Valores = sinIva ? "sinIva" : "total"
            };

            resultado.Fechas = facturas
                .GroupBy(f => f.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new ValorPorFecha
                {
                    Fecha = ParserFecha.Formatear(g.Key),
                    CantidadFacturas = g.Count(),
                    Valor = sinIva ? g.Sum(f => f.Valor) : g.Sum(f => f.Total ?? 0m)
                })
                .ToList();

            resultado.Total = resultado.Fechas.Sum(x => x.Valor);
            return resultado;
        }

        // -------------------------------------------------------------------
        // /Grafica: los mismos datos de los dos resumenes, listos para graficar
        // -------------------------------------------------------------------

        public DatosGrafica GraficaIva(DateTime fecha)
        {
            ResumenIvaResultado resumen = ResumenIva(fecha);

            return new DatosGrafica
            {
                Titulo = "IVA emitido y recibido por NIT - " + resumen.Fecha,
                Etiquetas = resumen.Nits.Select(n => n.Nit).ToList(),
                Series = new List<SerieGrafica>
            {
                new SerieGrafica { Nombre = "IVA emitido", Valores = resumen.Nits.Select(n => n.IvaEmitido).ToList() },
                new SerieGrafica { Nombre = "IVA recibido", Valores = resumen.Nits.Select(n => n.IvaRecibido).ToList() }
            }
            };
        }

        public DatosGrafica GraficaRango(DateTime desde, DateTime hasta, bool sinIva)
        {
            ResumenRangoResultado resumen = ResumenRango(desde, hasta, sinIva);
            string tipo = sinIva ? "valor sin IVA" : "valor total";

            return new DatosGrafica
            {
                Titulo = "Valor autorizado por fecha (" + tipo + ") " + resumen.Desde + " - " + resumen.Hasta,
                Etiquetas = resumen.Fechas.Select(x => x.Fecha).ToList(),
                Series = new List<SerieGrafica>
            {
                new SerieGrafica { Nombre = sinIva ? "Valor sin IVA" : "Valor total", Valores = resumen.Fechas.Select(x => x.Valor).ToList() }
            }
            };
        }

        
        // /Estadisticas: reporte de la SAT por fecha

        public EstadisticasResultado Estadisticas(DateTime desde, DateTime hasta)
        {
            List<Factura> facturas;
            lock (_almacen.Lock)
            {
                facturas = _almacen.Facturas
                    .Where(f => f.Fecha.Date >= desde.Date && f.Fecha.Date <= hasta.Date)
                    .ToList();
            }

            var resultado = new EstadisticasResultado
            {
                Desde = ParserFecha.Formatear(desde),
                Hasta = ParserFecha.Formatear(hasta)
            };

            resultado.Dias = facturas
                .GroupBy(f => f.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new EstadisticaDia
                {
                    Fecha = ParserFecha.Formatear(g.Key),
                    CantidadFacturasRecibidas = g.Count(),
                    Errores = new ErroresDia
                    {
                        NitEmisorInvalido = g.Count(f => f.Errores.Contains(TipoError.NitEmisorInvalido)),
                        NitReceptorInvalido = g.Count(f => f.Errores.Contains(TipoError.NitReceptorInvalido)),
                        NitEmisorInexistente = g.Count(f => f.Errores.Contains(TipoError.NitEmisorInexistente)),
                        NitReceptorInexistente = g.Count(f => f.Errores.Contains(TipoError.NitReceptorInexistente)),
                        IvaMalCalculado = g.Count(f => f.Errores.Contains(TipoError.IvaMalCalculado)),
                        TotalMalCalculado = g.Count(f => f.Errores.Contains(TipoError.TotalMalCalculado)),
                        ReferenciaDuplicada = g.Count(f => f.Errores.Contains(TipoError.ReferenciaDuplicada)),
                        DatosInvalidos = g.Count(f => f.Errores.Contains(TipoError.DatosInvalidos))
                    },
                    CantidadFacturasSinErrores = g.Count(f => f.Errores.Count == 0),
                    CantidadEmisores = g.Select(f => f.NitEmisor).Where(n => n.Length > 0).Distinct().Count(),
                    CantidadReceptores = g.Select(f => f.NitReceptor).Where(n => n.Length > 0).Distinct().Count()
                })
                .ToList();

            return resultado;
        }
    }

}
