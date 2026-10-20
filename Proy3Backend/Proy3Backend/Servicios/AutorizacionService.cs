using Proy3Backend.Models;
using Proy3Backend.Utilidades;
using System.Globalization;
using System.Text.Json;

namespace Proy3Backend.Servicios
{
    public class AutorizacionService
    {
        private const int MaxLongitudReferencia = 40;

        private readonly AlmacenDatos _almacen;

        public AutorizacionService(AlmacenDatos almacen)
        {
            _almacen = almacen;
        }

        public RespuestaAutorizaciones Procesar(JsonElement raiz)
        {
            var respuesta = new RespuestaAutorizaciones();
            ResultadoAutorizaciones resultado = respuesta.ResultadoAutorizaciones;
            DateTime ahora = ParserFecha.AhoraGuatemala();

            lock (_almacen.Lock)
            {
                foreach (JsonElement elemento in JsonLector.Elementos(raiz))
                {
                    Factura factura = Evaluar(elemento, ahora);

                    if (factura.Aprobada)
                    {
                        factura.CodigoAprobacion = SiguienteCodigo(ahora);
                        _almacen.ReferenciasAprobadas.Add(factura.Referencia);

                        resultado.CantidadAutorizacionesAprobadas++;
                        resultado.DetalleAprobaciones.Add(new DetalleAprobacion
                        {
                            Referencia = factura.Referencia,
                            CodigoAprobacion = factura.CodigoAprobacion.Value
                        });
                    }

                    if (factura.Errores.Contains(TipoError.NitEmisorInexistente))
                        resultado.CantidadAutorizacionesRechazadasPorNitEmisorInexistente++;

                    if (factura.Errores.Contains(TipoError.NitReceptorInexistente))
                        resultado.CantidadAutorizacionesRechazadasPorNitReceptorInexistente++;

                    _almacen.Facturas.Add(factura);
                }

                _almacen.GuardarFacturas();
                _almacen.GuardarCorrelativo();
            }

            return respuesta;
        }



        private Factura Evaluar(JsonElement elemento, DateTime ahora)
        {
            var factura = new Factura { FechaSolicitud = ahora };

            // Fecha: se extrae con expresion regular; el resto del texto se descarta.
            string? tiempo = JsonLector.Texto(elemento, "TIEMPO");
            if (ParserFecha.TryExtraerFechaHora(tiempo, out DateTime fecha))
            {
                factura.Fecha = fecha;
            }
            else
            {
                factura.Fecha = ahora;
                Agregar(factura, TipoError.DatosInvalidos);
            }

            // Referencia: texto unico de maximo 40 posiciones.
            // Se considera duplicada si ya existe en una factura APROBADA (o en una anterior del mismo mensaje).
            // Una factura rechazada puede reenviarse corregida con la misma referencia.
            string referencia = (JsonLector.Texto(elemento, "REFERENCIA") ?? "").Trim();
            factura.Referencia = referencia;
            if (referencia.Length == 0 || referencia.Length > MaxLongitudReferencia)
            {
                Agregar(factura, TipoError.DatosInvalidos);
            }
            else if (_almacen.ReferenciasAprobadas.Contains(referencia))
            {
                Agregar(factura, TipoError.ReferenciaDuplicada);
            }

            // NIT emisor: formato/verificador y existencia rtu.
            string? emisor = NitValidator.Normalizar(JsonLector.Texto(elemento, "NIT_EMISOR"));
            factura.NitEmisor = emisor ?? "";
            if (emisor == null || !NitValidator.EsValido(emisor))
                Agregar(factura, TipoError.NitEmisorInvalido);
            else if (!_almacen.Rtu.ContainsKey(emisor))
                Agregar(factura, TipoError.NitEmisorInexistente);

            // NIT receptor: igual que el emisor.
            string? receptor = NitValidator.Normalizar(JsonLector.Texto(elemento, "NIT_RECEPTOR"));
            factura.NitReceptor = receptor ?? "";
            if (receptor == null || !NitValidator.EsValido(receptor))
                Agregar(factura, TipoError.NitReceptorInvalido);
            else if (!_almacen.Rtu.ContainsKey(receptor))
                Agregar(factura, TipoError.NitReceptorInexistente);

            // Montos
            decimal? valor = JsonLector.Numero(elemento, "VALOR");
            decimal? iva = JsonLector.Numero(elemento, "IVA");
            decimal? total = JsonLector.Numero(elemento, "TOTAL");
            factura.Valor = valor ?? 0m;
            factura.Iva = iva;
            factura.Total = total;

            if (valor == null || valor.Value <= 0m || decimal.Round(valor.Value, 2) != valor.Value)
            {
                // VALOR debe ser real positivo con 2 posiciones decimales
                Agregar(factura, TipoError.DatosInvalidos);
            }
            else
            {
                // IVA = redondeado
                decimal ivaEsperado = Math.Round(valor.Value * 0.12m, 2, MidpointRounding.AwayFromZero);
                if (iva == null || iva.Value != ivaEsperado)
                    Agregar(factura, TipoError.IvaMalCalculado);

                // TOTAL 
                if (iva == null || total == null || total.Value != valor.Value + iva.Value)
                    Agregar(factura, TipoError.TotalMalCalculado);
            }

            return factura;
        }

        private static void Agregar(Factura factura, TipoError error)
        {
            if (!factura.Errores.Contains(error)) factura.Errores.Add(error);
        }

        private long SiguienteCodigo(DateTime ahora)
        {
            string dia = ahora.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            CorrelativoDiario correlativo = _almacen.Correlativo;

            if (correlativo.Fecha != dia)
            {
                correlativo.Fecha = dia;
                correlativo.Ultimo = 0;
            }

            correlativo.Ultimo++;

            string texto = dia + correlativo.Ultimo.ToString("D8", CultureInfo.InvariantCulture);
            return long.Parse(texto, CultureInfo.InvariantCulture);
        }
    }
}
