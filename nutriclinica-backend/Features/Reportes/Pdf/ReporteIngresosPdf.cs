using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Reportes.DTOs;

namespace nutriclinica_backend.Features.Reportes.Pdf;

/// <summary>
/// Reporte de ingresos imprimible: lo mismo que la pantalla de Reportes
/// (resumen, grafica y desgloses) mas el detalle que pide un corte de caja:
/// cada cobro del periodo y las consultas que siguen sin cobrarse.
/// Misma linea visual que los demas PDF (encabezado con la clinica, acento
/// teal, pie con fecha y paginas).
/// </summary>
public static class ReporteIngresosPdf
{
    private static readonly string Acento = Colors.Teal.Darken2;
    private static readonly string Suave = Colors.Teal.Lighten5;
    private static readonly string Tenue = Colors.Grey.Darken1;
    private static readonly string Linea = Colors.Grey.Lighten2;

    public record CobroFila(
        string Recibo, DateTime Fecha, string Paciente, string Nutricionista,
        TipoConsulta Tipo, MetodoPago Metodo, decimal Monto, bool Anulado);

    public record PendienteFila(DateTime Fecha, string Paciente, string Nutricionista, TipoConsulta Tipo);

    public static byte[] Generar(
        ReporteIngresosDto r,
        IReadOnlyList<CobroFila> cobros,
        IReadOnlyList<PendienteFila> pendientes,
        string alcance,
        DatosFiscalesClinica? clinica,
        int maximoFilas) =>
        Crear(r, cobros, pendientes, alcance, clinica, maximoFilas).GeneratePdf();

    public static IDocument Crear(
        ReporteIngresosDto r,
        IReadOnlyList<CobroFila> cobros,
        IReadOnlyList<PendienteFila> pendientes,
        string alcance,
        DatosFiscalesClinica? clinica,
        int maximoFilas)
    {
        var periodo = $"{ZonaClinica.Formatear(r.Desde, "d MMM yyyy")} – {ZonaClinica.Formatear(r.Hasta, "d MMM yyyy")}";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Colors.Grey.Darken4));

                page.Header().Element(c => Encabezado(c, clinica, periodo, alcance));
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(16);
                    col.Item().Element(c => Resumen(c, r));
                    if (r.Cobros > 0) col.Item().Element(c => Grafica(c, r));
                    col.Item().Element(c => Desgloses(c, r));
                    col.Item().Element(c => DetalleCobros(c, cobros, maximoFilas));
                    col.Item().Element(c => Pendientes(c, pendientes, maximoFilas));
                });
                page.Footer().Element(Pie);
            });
        });
    }

    // ── Secciones ────────────────────────────────────────────────────

    private static void Resumen(IContainer contenedor, ReporteIngresosDto r)
    {
        contenedor.Row(row =>
        {
            row.Spacing(8);
            Tarjeta(row.RelativeItem(), "Ingresos", Dinero(r.Total), Comparacion(r.Total, r.TotalAnterior), destacada: true);
            Tarjeta(row.RelativeItem(), "Cobros", r.Cobros.ToString(ZonaClinica.Cultura),
                r.Anulados == 0 ? "Sin anulaciones" : $"{r.Anulados} anulado(s) · {Dinero(r.MontoAnulado)}");
            Tarjeta(row.RelativeItem(), "Ticket promedio", Dinero(r.TicketPromedio), "Por cobro");
            Tarjeta(row.RelativeItem(), "Consultas atendidas", r.ConsultasAtendidas.ToString(ZonaClinica.Cultura),
                r.ConsultasPorCobrar == 0 ? "Todas cobradas" : $"{r.ConsultasPorCobrar} por cobrar");
        });
    }

    private static void Tarjeta(IContainer c, string etiqueta, string valor, string detalle, bool destacada = false)
    {
        c.Background(destacada ? Acento : Suave).Padding(10).Column(col =>
        {
            col.Item().Text(etiqueta).FontSize(8).FontColor(destacada ? Colors.Teal.Lighten4 : Tenue);
            col.Item().Text(valor).FontSize(15).Bold().FontColor(destacada ? Colors.White : Colors.Grey.Darken4);
            col.Item().Text(detalle).FontSize(7.5f).FontColor(destacada ? Colors.Teal.Lighten4 : Tenue);
        });
    }

    /// <summary>Barras de ingresos por tramo (dia, semana o mes).</summary>
    private static void Grafica(IContainer contenedor, ReporteIngresosDto r)
    {
        var titulo = r.Agrupacion switch
        {
            AgrupacionReporte.Dia => "Ingresos por día",
            AgrupacionReporte.Semana => "Ingresos por semana",
            _ => "Ingresos por mes",
        };
        var maximo = r.Serie.Count == 0 ? 0 : r.Serie.Max(p => p.Total);
        const float alto = 110;
        // Con muchos tramos solo se rotulan algunos para que no se encimen.
        var cada = Math.Max(1, (int)Math.Ceiling(r.Serie.Count / 16.0));

        Seccion(contenedor, titulo, col =>
        {
            col.Item().Height(alto + 26).Row(row =>
            {
                row.Spacing(r.Serie.Count > 40 ? 1 : 3);
                for (var i = 0; i < r.Serie.Count; i++)
                {
                    var p = r.Serie[i];
                    var barra = maximo == 0 ? 0 : (float)(p.Total / maximo) * alto;
                    var rotulo = i % cada == 0 ? EtiquetaTramo(p.Clave, r.Agrupacion, primero: i == 0) : "";
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Height(alto - barra);
                        c.Item().Height(Math.Max(barra, p.Total > 0 ? 2 : 0.5f))
                            .Background(p.Total > 0 ? Acento : Linea);
                        c.Item().PaddingTop(3).AlignCenter().Text(rotulo).FontSize(6).FontColor(Tenue);
                    });
                }
            });
            col.Item().Text($"Máximo: {Dinero(maximo)}").FontSize(7.5f).FontColor(Tenue);
        });
    }

    private static void Desgloses(IContainer contenedor, ReporteIngresosDto r)
    {
        contenedor.Row(row =>
        {
            row.Spacing(12);
            Desglose(row.RelativeItem(), "Por método de pago", r.Total,
                r.PorMetodo.Select(m => (Metodo(m.Metodo), m.Total, m.Cobros)).ToList());
            if (r.PorNutricionista.Count > 1)
            {
                Desglose(row.RelativeItem(), "Por nutricionista", r.Total,
                    r.PorNutricionista.Select(n => (n.Nombre, n.Total, n.Cobros)).ToList());
            }
            Desglose(row.RelativeItem(), "Por tipo de consulta", r.Total,
                r.PorTipoConsulta.Select(t => (t.Tipo.ToString(), t.Total, t.Cobros)).ToList());
        });
    }

    private static void Desglose(IContainer contenedor, string titulo, decimal total,
        List<(string Nombre, decimal Total, int Cobros)> filas)
    {
        Seccion(contenedor, titulo, col =>
        {
            if (filas.Count == 0)
            {
                col.Item().Text("Sin cobros").FontColor(Tenue);
                return;
            }
            foreach (var f in filas)
            {
                var porcentaje = total == 0 ? 0 : (float)(f.Total / total);
                col.Item().Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text(f.Nombre).SemiBold();
                        r.AutoItem().Text(Dinero(f.Total)).SemiBold();
                    });
                    c.Item().PaddingTop(2).Height(4).Row(r =>
                    {
                        if (porcentaje > 0) r.RelativeItem(porcentaje).Background(Acento);
                        if (porcentaje < 1) r.RelativeItem(1 - porcentaje).Background(Suave);
                    });
                    c.Item().PaddingTop(1).Text($"{f.Cobros} cobro(s) · {porcentaje:P0}").FontSize(7.5f).FontColor(Tenue);
                });
            }
        });
    }

    private static void DetalleCobros(IContainer contenedor, IReadOnlyList<CobroFila> cobros, int maximo)
    {
        Seccion(contenedor, $"Detalle de cobros ({cobros.Count})", col =>
        {
            if (cobros.Count == 0)
            {
                col.Item().Text("No hubo cobros en el periodo.").FontColor(Tenue);
                return;
            }

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(58);
                    c.ConstantColumn(54);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.4f);
                    c.ConstantColumn(64);
                });
                t.Header(h =>
                {
                    foreach (var titulo in new[] { "Recibo", "Fecha", "Paciente", "Nutricionista", "Tipo", "Método" })
                        h.Cell().Element(Cabecera).Text(titulo).FontSize(8).Bold().FontColor(Colors.White);
                    h.Cell().Element(Cabecera).AlignRight().Text("Monto").FontSize(8).Bold().FontColor(Colors.White);
                });

                var par = false;
                foreach (var c in cobros.Take(maximo))
                {
                    string fondo = par ? Colors.Grey.Lighten4 : Colors.White;
                    par = !par;
                    IContainer Celda(IContainer x) => x.Background(fondo).PaddingVertical(3).PaddingHorizontal(4);
                    string color = c.Anulado ? Tenue : (string)Colors.Grey.Darken4;

                    t.Cell().Element(Celda).Text(c.Recibo).FontSize(8).FontColor(color);
                    t.Cell().Element(Celda).Text(ZonaClinica.Formatear(c.Fecha, "dd/MM/yy")).FontSize(8).FontColor(color);
                    t.Cell().Element(Celda).Text(c.Paciente).FontSize(8).FontColor(color);
                    t.Cell().Element(Celda).Text(c.Nutricionista).FontSize(8).FontColor(color);
                    t.Cell().Element(Celda).Text(c.Tipo.ToString()).FontSize(8).FontColor(color);
                    t.Cell().Element(Celda).Text(Metodo(c.Metodo)).FontSize(8).FontColor(color);
                    t.Cell().Element(Celda).AlignRight().Text(txt =>
                    {
                        var monto = txt.Span(Dinero(c.Monto)).FontSize(8).SemiBold().FontColor(color);
                        if (c.Anulado)
                        {
                            monto.Strikethrough();
                            txt.Line("").FontSize(1);
                            txt.Span("Anulado").FontSize(6.5f).FontColor(Colors.Red.Darken2);
                        }
                    });
                }
            });

            if (cobros.Count > maximo)
                col.Item().Text($"Se muestran los primeros {maximo} de {cobros.Count} cobros. Acota el periodo para ver el resto.")
                    .FontSize(7.5f).FontColor(Tenue);
            col.Item().AlignRight().Text(t =>
            {
                t.Span("Total cobrado (sin anulados): ").FontColor(Tenue);
                t.Span(Dinero(cobros.Where(c => !c.Anulado).Sum(c => c.Monto))).Bold().FontColor(Acento);
            });
        });
    }

    private static void Pendientes(IContainer contenedor, IReadOnlyList<PendienteFila> pendientes, int maximo)
    {
        Seccion(contenedor, $"Consultas por cobrar ({pendientes.Count})", col =>
        {
            if (pendientes.Count == 0)
            {
                col.Item().Text("Todas las consultas del periodo están cobradas.").FontColor(Tenue);
                return;
            }
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(64);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(1.4f);
                });
                t.Header(h =>
                {
                    foreach (var titulo in new[] { "Fecha", "Paciente", "Nutricionista", "Tipo" })
                        h.Cell().Element(Cabecera).Text(titulo).FontSize(8).Bold().FontColor(Colors.White);
                });
                var par = false;
                foreach (var p in pendientes.Take(maximo))
                {
                    string fondo = par ? Colors.Grey.Lighten4 : Colors.White;
                    par = !par;
                    IContainer Celda(IContainer x) => x.Background(fondo).PaddingVertical(3).PaddingHorizontal(4);
                    t.Cell().Element(Celda).Text(ZonaClinica.Formatear(p.Fecha, "dd/MM/yy")).FontSize(8);
                    t.Cell().Element(Celda).Text(p.Paciente).FontSize(8);
                    t.Cell().Element(Celda).Text(p.Nutricionista).FontSize(8);
                    t.Cell().Element(Celda).Text(p.Tipo.ToString()).FontSize(8);
                }
            });
            if (pendientes.Count > maximo)
                col.Item().Text($"Se muestran las primeras {maximo} de {pendientes.Count}.").FontSize(7.5f).FontColor(Tenue);
        });
    }

    // ── Piezas comunes ───────────────────────────────────────────────

    private static void Encabezado(IContainer contenedor, DatosFiscalesClinica? clinica, string periodo, string alcance)
    {
        contenedor.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(clinica?.RazonSocial ?? "NutriClínica").FontSize(16).Bold().FontColor(Acento);
                    if (!string.IsNullOrWhiteSpace(clinica?.Direccion)) c.Item().Text(clinica.Direccion).FontColor(Tenue);
                    if (!string.IsNullOrWhiteSpace(clinica?.RFC)) c.Item().Text($"RFC {clinica.RFC}").FontColor(Tenue);
                });
                row.ConstantItem(230).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text("REPORTE DE INGRESOS").FontSize(13).Bold();
                    c.Item().AlignRight().Text(periodo).SemiBold();
                    c.Item().AlignRight().Text(alcance).FontColor(Tenue);
                });
            });
            col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Acento);
        });
    }

    private static void Seccion(IContainer contenedor, string titulo, Action<ColumnDescriptor> contenido)
    {
        contenedor.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text(titulo).FontSize(11).Bold().FontColor(Acento);
            contenido(col);
        });
    }

    private static IContainer Cabecera(IContainer c) => c.Background(Acento).PaddingVertical(4).PaddingHorizontal(4);

    private static void Pie(IContainer contenedor)
    {
        contenedor.Row(row =>
        {
            row.RelativeItem().Text("Montos en pesos mexicanos. Los cobros anulados no suman al total.")
                .FontSize(7.5f).FontColor(Tenue);
            row.AutoItem().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Tenue));
                t.Span($"Generado el {ZonaClinica.FechaHora(DateTime.UtcNow)}  ·  Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }

    private static string Dinero(decimal monto) => monto.ToString("C", ZonaClinica.Cultura);

    private static string Comparacion(decimal total, decimal anterior)
    {
        if (anterior == 0) return total > 0 ? "Sin ingresos el periodo anterior" : "Igual que el periodo anterior";
        var cambio = Math.Round((total - anterior) / anterior * 100);
        return cambio == 0 ? "Igual que el periodo anterior" : $"{(cambio > 0 ? "+" : "")}{cambio}% vs. periodo anterior";
    }

    /// <summary>Por dia basta el numero (el mes va en la primera barra o al cambiar).</summary>
    private static string EtiquetaTramo(string clave, AgrupacionReporte agrupacion, bool primero)
    {
        var fecha = DateTime.ParseExact(clave, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return agrupacion switch
        {
            AgrupacionReporte.Mes => fecha.ToString("MMM yy", ZonaClinica.Cultura),
            AgrupacionReporte.Dia when !primero && fecha.Day != 1 => fecha.Day.ToString(ZonaClinica.Cultura),
            _ => fecha.ToString("d MMM", ZonaClinica.Cultura),
        };
    }

    private static string Metodo(MetodoPago m) => m switch
    {
        MetodoPago.TarjetaDebito => "Débito",
        MetodoPago.TarjetaCredito => "Crédito",
        _ => m.ToString(),
    };
}
