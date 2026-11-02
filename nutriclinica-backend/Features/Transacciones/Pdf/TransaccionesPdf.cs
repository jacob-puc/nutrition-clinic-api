using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Utils;

namespace nutriclinica_backend.Features.Transacciones.Pdf;

public static class TransaccionesPdf
{
    private static readonly CultureInfo CulturaMx = CultureInfo.GetCultureInfo("es-MX");

    public static byte[] ComprobantePago(
        Pago pago,
        string pacienteNombre,
        string concepto,
        DatosFiscalesClinica? clinica)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(clinica?.RazonSocial ?? "NutriClínica")
                            .FontSize(18).Bold().FontColor(Colors.Teal.Darken2);

                        if (clinica is not null)
                        {
                            col.Item().Text($"RFC: {clinica.RFC}");
                            col.Item().Text(clinica.Direccion);

                            if (!string.IsNullOrWhiteSpace(clinica.Telefono))
                            {
                                col.Item().Text($"Tel: {clinica.Telefono}");
                            }

                            if (!string.IsNullOrWhiteSpace(clinica.Correo))
                            {
                                col.Item().Text(clinica.Correo);
                            }
                        }
                    });

                    row.ConstantItem(190).AlignRight().Column(col =>
                    {
                        col.Item().Text("COMPROBANTE DE PAGO").FontSize(13).Bold();
                        col.Item().Text($"Folio: {pago.NumeroRecibo}");
                        col.Item().Text($"Fecha: {ZonaClinica.FechaHora(pago.FechaPago)}");
                    });
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().Text("Datos del pago")
                        .Bold().FontSize(12).FontColor(Colors.Teal.Darken2);

                    Fila(col, "Paciente", pacienteNombre);
                    Fila(col, "Concepto", concepto);
                    Fila(col, "Método de pago", pago.MetodoPago.ToString());
                    Fila(col, "Estado", pago.Estado.ToString());
                    Fila(col, "Moneda", pago.Moneda);

                    if (!string.IsNullOrWhiteSpace(pago.Notas))
                    {
                        Fila(col, "Notas", pago.Notas);
                    }

                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1)
                            .Padding(12).Column(c =>
                            {
                                c.Item().Text("TOTAL PAGADO")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(pago.Monto.ToString("C2", CulturaMx))
                                    .FontSize(20).Bold().FontColor(Colors.Teal.Darken2);
                            });
                    });
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().Text("Este comprobante es un recibo interno y no constituye un CFDI.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Generado el {ZonaClinica.FechaHora(DateTime.UtcNow)}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    public static byte[] FacturaPdf(
        Factura factura,
        Pago pago,
        string pacienteNombre,
        string concepto,
        DatosFiscalesClinica? clinica)
    {
        var subtotal = factura.Subtotal.ToString("C2", CulturaMx);
        var iva = factura.Iva.ToString("C2", CulturaMx);
        var total = factura.Total.ToString("C2", CulturaMx);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(clinica?.RazonSocial ?? "NutriClínica")
                            .FontSize(18).Bold().FontColor(Colors.Teal.Darken2);

                        if (clinica is not null)
                        {
                            col.Item().Text($"RFC: {clinica.RFC}");
                            col.Item().Text($"Régimen fiscal: {CatalogosSat.Regimen(clinica.RegimenFiscal)}");
                            col.Item().Text($"CP: {clinica.CodigoPostal}");
                            col.Item().Text(clinica.Direccion);
                        }
                    });

                    row.ConstantItem(190).AlignRight().Column(col =>
                    {
                        col.Item().Text("FACTURA").FontSize(16).Bold();
                        col.Item().Text($"Serie-Folio: {factura.Serie}-{factura.Folio:D4}");
                        col.Item().Text($"Fecha: {ZonaClinica.FechaHora(factura.FechaEmision)}");
                    });
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().Text("Receptor").Bold().FontSize(12).FontColor(Colors.Teal.Darken2);
                    Fila(col, "Nombre / Razón social", factura.NombreRazonSocial);
                    Fila(col, "RFC", factura.RFC);
                    Fila(col, "Tipo de persona", CatalogosSat.Persona(factura.TipoPersona));
                    Fila(col, "Régimen fiscal", CatalogosSat.Regimen(factura.RegimenFiscal));
                    Fila(col, "Uso de CFDI", CatalogosSat.Uso(factura.UsoCfdi));
                    Fila(col, "Código postal", factura.CodigoPostal);

                    col.Item().PaddingTop(8).Text("Concepto").Bold().FontSize(12)
                        .FontColor(Colors.Teal.Darken2);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5)
                                .Text("Descripción").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).AlignRight()
                                .Text("Cant.").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).AlignRight()
                                .Text("P. unitario").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).AlignRight()
                                .Text("Importe").FontColor(Colors.White).Bold();
                        });

                        table.Cell().Padding(5).Text(concepto);
                        table.Cell().Padding(5).AlignRight().Text("1");
                        table.Cell().Padding(5).AlignRight().Text(subtotal);
                        table.Cell().Padding(5).AlignRight().Text(subtotal);
                    });

                    col.Item().AlignRight().Width(240).Column(totales =>
                    {
                        totales.Item().Row(fila =>
                        {
                            fila.RelativeItem().Text("Subtotal");
                            fila.ConstantItem(100).AlignRight().Text(subtotal);
                        });
                        totales.Item().Row(fila =>
                        {
                            fila.RelativeItem().Text($"IVA ({factura.TarifaIva:0.##}%)");
                            fila.ConstantItem(100).AlignRight().Text(iva);
                        });
                        totales.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        totales.Item().Row(fila =>
                        {
                            fila.RelativeItem().Text("Total").Bold();
                            fila.ConstantItem(100).AlignRight().Text(total).Bold();
                        });
                    });

                    col.Item().PaddingTop(8).Text(
                        $"Pago: {pago.NumeroRecibo} · {pago.MetodoPago} · " +
                        $"{ZonaClinica.FechaHora(pago.FechaPago)}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);

                    col.Item().Text($"Paciente: {pacienteNombre}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().Text(factura.UUID is null
                            ? "Comprobante SIN timbrar (UUID pendiente) — no válido para efectos fiscales."
                            : $"UUID: {factura.UUID}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Generado el {ZonaClinica.FechaHora(DateTime.UtcNow)}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static void Fila(ColumnDescriptor col, string etiqueta, string valor)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(150).Text(etiqueta).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().Text(valor).Bold();
        });
    }
}
