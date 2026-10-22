using Color = QuestPDF.Infrastructure.Color;
using IContainer = QuestPDF.Infrastructure.IContainer;
using Document = QuestPDF.Fluent.Document;
using Proy3Frontend.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection.Metadata;

namespace Proy3Frontend.Servicios;

/// 
public static class ReportePdfBuilder
{
    private static readonly Color[] Colores = { Colors.Blue.Medium, Colors.Orange.Medium, Colors.Green.Medium };

    static ReportePdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static async Task<byte[]> ConstruirAsync(
        ApiClient api, string? nit, string tipo, string? desde, string? hasta, string? fecha, string valores)
    {
        bool hayRango = !string.IsNullOrWhiteSpace(desde) && !string.IsNullOrWhiteSpace(hasta);
        bool hayNit = !string.IsNullOrWhiteSpace(nit);
        bool hayFecha = !string.IsNullOrWhiteSpace(fecha);

        // Se piden todos los datos a la API (async)...
        RespuestaApi<EstadisticasResultado>? estadisticas = null;
        RespuestaApi<ConsultaDatosResultado>? contribuyente = null;
        RespuestaApi<ResumenIvaResultado>? iva = null;
        RespuestaApi<DatosGrafica>? graficaIva = null;
        RespuestaApi<ResumenRangoResultado>? rango = null;
        RespuestaApi<DatosGrafica>? graficaRango = null;

        if (hayRango)
        {
            estadisticas = await api.GetAsync<EstadisticasResultado>(
                "/Estadisticas", ("desde", desde), ("hasta", hasta));
        }

        if (hayRango && hayNit)
        {
            contribuyente = await api.GetAsync<ConsultaDatosResultado>(
                "/ConsultaDatos", ("nit", nit), ("tipo", tipo), ("desde", desde), ("hasta", hasta));
        }

        if (hayFecha)
        {
            iva = await api.GetAsync<ResumenIvaResultado>("/ResumenIva", ("fecha", fecha));
            graficaIva = await api.GetAsync<DatosGrafica>("/Grafica", ("resumen", "iva"), ("fecha", fecha));
        }

        if (hayRango)
        {
            rango = await api.GetAsync<ResumenRangoResultado>(
                "/ResumenRango", ("desde", desde), ("hasta", hasta), ("valores", valores));
            graficaRango = await api.GetAsync<DatosGrafica>(
                "/Grafica", ("resumen", "rango"), ("desde", desde), ("hasta", hasta), ("valores", valores));
        }

        // 2) se arma el documento.
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(40);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Text("Reporte de autorizaciones de DTE").Bold().FontSize(16);

                pagina.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text("Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))
                        .FontSize(9).FontColor(Colors.Grey.Darken1);

                    if (!hayRango && !hayFecha)
                    {
                        col.Item().Text("No se indicaron fechas, por lo que no hay información que mostrar.");
                    }

                    if (estadisticas != null) SeccionEstadisticas(col, estadisticas);
                    if (contribuyente != null) SeccionContribuyente(col, contribuyente);
                    if (iva != null) SeccionIva(col, iva, graficaIva);
                    if (rango != null) SeccionRango(col, rango, graficaRango);
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    // Secciones

    private static void SeccionEstadisticas(ColumnDescriptor col, RespuestaApi<EstadisticasResultado> r)
    {
        Titulo(col, "1. Estadísticas por fecha");
        if (!r.Ok) { Aviso(col, r.Error); return; }

        EstadisticasResultado datos = r.Valor!;
        col.Item().Text("Rango: " + datos.Desde + " al " + datos.Hasta);

        if (datos.Dias.Count == 0)
        {
            col.Item().Text("No hay facturas recibidas en el rango.");
            return;
        }

        Tabla(col,
            new[] { "Fecha", "Facturas recibidas", "Facturas sin errores", "Emisores distintos", "Receptores distintos" },
            datos.Dias.Select(d => new[]
            {
                d.Fecha,
                Num(d.CantidadFacturasRecibidas),
                Num(d.CantidadFacturasSinErrores),
                Num(d.CantidadEmisores),
                Num(d.CantidadReceptores)
            }).ToList());

        col.Item().Text("Errores en facturas recibidas, por fecha:").Bold();

        Tabla(col,
            new[]
            {
                "Fecha", "NIT emisor inválido", "NIT receptor inválido", "NIT emisor inexistente",
                "NIT receptor inexistente", "IVA mal calculado", "TOTAL mal calculado",
                "REFERENCIA duplicada", "Datos inválidos"
            },
            datos.Dias.Select(d => new[]
            {
                d.Fecha,
                Num(d.Errores.NitEmisorInvalido),
                Num(d.Errores.NitReceptorInvalido),
                Num(d.Errores.NitEmisorInexistente),
                Num(d.Errores.NitReceptorInexistente),
                Num(d.Errores.IvaMalCalculado),
                Num(d.Errores.TotalMalCalculado),
                Num(d.Errores.ReferenciaDuplicada),
                Num(d.Errores.DatosInvalidos)
            }).ToList(),
            tamano: 8);
    }

    private static void SeccionContribuyente(ColumnDescriptor col, RespuestaApi<ConsultaDatosResultado> r)
    {
        Titulo(col, "2. Facturas por contribuyente");
        if (!r.Ok) { Aviso(col, r.Error); return; }

        ConsultaDatosResultado datos = r.Valor!;
        string nombre = string.IsNullOrEmpty(datos.Nombre) ? "" : " (" + datos.Nombre + ")";
        col.Item().Text("NIT " + datos.Nit + nombre + " - facturas " + datos.Tipo + " del " + datos.Desde + " al " + datos.Hasta);

        if (datos.PorFecha.Count == 0)
        {
            col.Item().Text("No hay facturas autorizadas para este NIT en el rango.");
            return;
        }

        var filas = datos.PorFecha
            .Select(f => new[] { f.Fecha, Num(f.CantidadFacturas), Formato.Dinero(f.MontoTotal) })
            .ToList();
        filas.Add(new[] { "TOTAL", Num(datos.TotalFacturas), Formato.Dinero(datos.MontoTotal) });

        Tabla(col, new[] { "Fecha", "Facturas autorizadas", "Monto total facturado" }, filas);
    }

    private static void SeccionIva(ColumnDescriptor col, RespuestaApi<ResumenIvaResultado> r, RespuestaApi<DatosGrafica>? grafica)
    {
        Titulo(col, "3. Resumen de IVA por NIT");
        if (!r.Ok) { Aviso(col, r.Error); return; }

        ResumenIvaResultado datos = r.Valor!;
        col.Item().Text("Fecha: " + datos.Fecha);

        if (datos.Nits.Count == 0)
        {
            col.Item().Text("No hay facturas autorizadas en esa fecha.");
            return;
        }

        var filas = datos.Nits
            .Select(n => new[] { n.Nit, n.Nombre ?? "", Formato.Dinero(n.IvaEmitido), Formato.Dinero(n.IvaRecibido) })
            .ToList();
        filas.Add(new[]
        {
            "TOTAL", "",
            Formato.Dinero(datos.Nits.Sum(n => n.IvaEmitido)),
            Formato.Dinero(datos.Nits.Sum(n => n.IvaRecibido))
        });

        Tabla(col, new[] { "NIT", "Nombre", "IVA emitido", "IVA recibido" }, filas, numericasDesde: 2);

        if (grafica != null && grafica.Ok) Barras(col, grafica.Valor!);
    }

    private static void SeccionRango(ColumnDescriptor col, RespuestaApi<ResumenRangoResultado> r, RespuestaApi<DatosGrafica>? grafica)
    {
        Titulo(col, "4. Resumen por rango de fechas");
        if (!r.Ok) { Aviso(col, r.Error); return; }

        ResumenRangoResultado datos = r.Valor!;
        string tipoValor = datos.Valores == "sinIva" ? "valores sin IVA" : "valores totales (con IVA)";
        col.Item().Text("Del " + datos.Desde + " al " + datos.Hasta + " - " + tipoValor);

        if (datos.Fechas.Count == 0)
        {
            col.Item().Text("No hay facturas autorizadas en el rango.");
            return;
        }

        var filas = datos.Fechas
            .Select(f => new[] { f.Fecha, Num(f.CantidadFacturas), Formato.Dinero(f.Valor) })
            .ToList();
        filas.Add(new[]
        {
            "TOTAL",
            Num(datos.Fechas.Sum(f => f.CantidadFacturas)),
            Formato.Dinero(datos.Total)
        });

        Tabla(col, new[] { "Fecha", "Facturas autorizadas", "Valor autorizado" }, filas);

        if (grafica != null && grafica.Ok) Barras(col, grafica.Valor!);
    }

    // Piezas reutilizables

    private static string Num(int valor) => valor.ToString(CultureInfo.InvariantCulture);

    private static void Titulo(ColumnDescriptor col, string texto)
    {
        col.Item().PaddingTop(10).Text(texto).Bold().FontSize(13);
    }

    private static void Aviso(ColumnDescriptor col, string? error)
    {
        col.Item().Text("No se pudo obtener la información: " + error);
    }

    private static IContainer EstiloCabecera(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);

    private static IContainer EstiloCelda(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

    /// Para tablas
    private static void Tabla(ColumnDescriptor col, string[] encabezados, List<string[]> filas, int numericasDesde = 1, float tamano = 9)
    {
        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                for (int i = 0; i < encabezados.Length; i++) columnas.RelativeColumn();
            });

            tabla.Header(cabecera =>
            {
                for (int i = 0; i < encabezados.Length; i++)
                {
                    IContainer celda = cabecera.Cell().Element(EstiloCabecera);
                    if (i >= numericasDesde) celda = celda.AlignRight();
                    celda.Text(encabezados[i]).Bold().FontSize(tamano);
                }
            });

            foreach (string[] fila in filas)
            {
                bool esTotal = fila.Length > 0 && fila[0] == "TOTAL";

                for (int i = 0; i < fila.Length; i++)
                {
                    IContainer celda = tabla.Cell().Element(EstiloCelda);
                    if (i >= numericasDesde) celda = celda.AlignRight();

                    if (esTotal) celda.Text(fila[i]).FontSize(tamano).Bold();
                    else celda.Text(fila[i]).FontSize(tamano);
                }
            }
        });
    }

    /// Graficas
    private static void Barras(ColumnDescriptor col, DatosGrafica g)
    {
        col.Item().PaddingTop(6).Text(g.Titulo).Bold();

        if (g.Etiquetas.Count == 0 || g.Series.Count == 0)
        {
            col.Item().Text("Sin datos para graficar.");
            return;
        }

        double max = g.Series.SelectMany(s => s.Valores).Select(v => (double)v).DefaultIfEmpty(0).Max();
        if (max <= 0) max = 1;

        // Leyendas
        if (g.Series.Count > 1)
        {
            col.Item().Row(fila =>
            {
                for (int s = 0; s < g.Series.Count; s++)
                {
                    int serie = s;
                    fila.AutoItem().Width(8).Height(8).Background(Colores[serie % Colores.Length]);
                    fila.AutoItem().PaddingLeft(4).PaddingRight(14).Text(g.Series[serie].Nombre).FontSize(8);
                }
            });
        }

        for (int i = 0; i < g.Etiquetas.Count; i++)
        {
            int indice = i;

            col.Item().PaddingVertical(2).Row(fila =>
            {
                fila.ConstantItem(90).Text(g.Etiquetas[indice]).FontSize(8);

                fila.RelativeItem().Column(barras =>
                {
                    for (int s = 0; s < g.Series.Count; s++)
                    {
                        int serie = s;
                        double valor = indice < g.Series[serie].Valores.Count ? (double)g.Series[serie].Valores[indice] : 0;
                        float ancho = (float)(valor / max * 300);

                        barras.Item().Row(barra =>
                        {
                            if (ancho > 0.5f)
                            {
                                barra.AutoItem().Width(ancho).Height(8).Background(Colores[serie % Colores.Length]);
                            }
                            barra.AutoItem().PaddingLeft(4).Text(valor.ToString("N2", CultureInfo.InvariantCulture)).FontSize(8);
                        });
                    }
                });
            });
        }
    }
}
