using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using static nutriclinica_backend.Infrastructure.Correo.IdentidadMarca;

namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>
/// Correos con la identidad del sitio publico (ver <see cref="IdentidadMarca"/>)
/// y la estructura de un correo de verdad: marca arriba, una tarjeta blanca con
/// franja de color, un solo boton principal (lo demas son enlaces de texto),
/// una linea de cierre y el pie con la direccion.
/// Todo va en tablas con estilos en linea: es lo unico que respetan todos
/// los clientes de correo. Donde no cargan Fraunces (Gmail), cae en Georgia.
/// </summary>
public static partial class PlantillaCorreo
{
    /// <param name="Detalle">Segunda linea, mas tenue (p. ej. la direccion bajo el lugar).</param>
    public record Dato(string Etiqueta, string Valor, string? Detalle = null);

    /// <param name="Primario">El boton grande; los demas se muestran como enlaces.</param>
    /// <param name="Peligro">Enlace en rojo (p. ej. "No podré asistir").</param>
    public record Boton(string Texto, string Url, bool Primario = false, bool Peligro = false);

    /// <summary>Bloque de fecha del "boleto": VIERNES / 20 / noviembre.</summary>
    public record Fecha(string DiaSemana, string Dia, string Mes);

    public record Contenido
    {
        /// <summary>Admite *enfasis*: "Te esperamos, *Ana*".</summary>
        public required string Titulo { get; init; }

        /// <summary>Opcional: los titulos ya suelen llevar el nombre.</summary>
        public string? Saludo { get; init; }
        public required string Introduccion { get; init; }

        public Fecha? FechaDestacada { get; init; }
        public IReadOnlyList<Dato> Datos { get; init; } = [];

        /// <summary>Codigo de verificacion, en grande.</summary>
        public string? Codigo { get; init; }

        public IReadOnlyList<Boton> Botones { get; init; } = [];

        /// <summary>Linea pequeña bajo el boton principal.</summary>
        public string? Ayuda { get; init; }
        public string? Nota { get; init; }

        /// <summary>Cierre centrado; *texto* va resaltado (p. ej. el telefono).</summary>
        public string? Cierre { get; init; }

        /// <summary>Pie bajo la tarjeta; cada linea con \n.</summary>
        public string? Pie { get; init; }

        /// <summary>"marca" o "peligro" (cancelaciones).</summary>
        public string Tono { get; init; } = "marca";

        public string ClinicaNombre { get; init; } = "NutriClínica";

        /// <summary>Logo publico (https); sin el, solo el nombre.</summary>
        public string? LogoUrl { get; init; }

        /// <summary>Nota escrita por quien envia (p. ej. el nutriologo al mandar el plan).</summary>
        public string? MensajePersonal { get; init; }
        public string? MensajeAutor { get; init; }
    }

    private const string FondoCorreo = "#EEF6F3";
    private const string BordeSuave = "#D7E9E3";
    private const string Gris = "#6B7472";
    private const string Cuerpo = "#3F4A48";

    public static string Html(Contenido c)
    {
        var peligro = c.Tono == "peligro";
        var acento = peligro ? Peligro : Marca;

        var logo = EsUrlSegura(c.LogoUrl)
            ? $"""<td style="padding-right:9px;vertical-align:middle;"><img src="{A(c.LogoUrl!)}" width="34" height="34" alt="" style="display:block;width:34px;height:34px;border-radius:50%;border:0;"></td>"""
            : "";

        var primario = c.Botones.FirstOrDefault(b => b.Primario);
        var enlaces = c.Botones.Where(b => b != primario).ToList();

        var cuerpo = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(c.Saludo))
            cuerpo.Append($"""<p style="margin:0 0 4px;color:{Texto};font-size:16px;font-weight:700;">{E(c.Saludo)}</p>""");
        cuerpo.Append($"""<p style="margin:0 0 24px;color:{Cuerpo};font-size:15.5px;line-height:1.6;">{E(c.Introduccion)}</p>""");

        if (!string.IsNullOrWhiteSpace(c.MensajePersonal))
        {
            cuerpo.Append($"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 22px;">
                  <tr><td style="border-left:3px solid {acento};padding:4px 0 4px 16px;">
                    <div style="font-family:{FuenteTitulo};font-style:italic;font-size:17px;line-height:1.5;color:{Texto};">“{E(c.MensajePersonal!.Trim()).Replace("\n", "<br>")}”</div>
                    {(c.MensajeAutor is null ? "" : $"<div style=\"margin-top:6px;color:{Gris};font-size:13px;font-weight:700;\">— {E(c.MensajeAutor)}</div>")}
                  </td></tr>
                </table>
                """);
        }

        if (!string.IsNullOrWhiteSpace(c.Codigo))
        {
            cuerpo.Append($"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 22px;">
                  <tr><td align="center" style="background:{Crema};border:1px dashed {Borde};border-radius:14px;padding:20px 10px;">
                    <div style="font-family:{FuenteTexto};font-size:38px;letter-spacing:8px;color:{Marca};font-weight:800;font-variant-numeric:tabular-nums;">{E(c.Codigo)}</div>
                  </td></tr>
                </table>
                """);
        }

        if (c.Datos.Count > 0) cuerpo.Append(Boleto(c, acento));

        if (primario is not null)
        {
            cuerpo.Append($"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:28px 0 0;">
                  <tr><td align="center">
                    <a href="{A(primario.Url)}" target="_blank" style="display:inline-block;background:{acento};color:#FFFFFF;font-family:{FuenteTexto};font-size:15.5px;font-weight:800;text-decoration:none;padding:15px 34px;border-radius:999px;">{E(primario.Texto)}</a>
                  </td></tr>
                </table>
                """);
            if (!string.IsNullOrWhiteSpace(c.Ayuda))
                cuerpo.Append($"""<p style="margin:10px 0 0;text-align:center;color:{Gris};font-size:12.5px;">{E(c.Ayuda)}</p>""");
        }

        if (enlaces.Count > 0)
        {
            var separador = $"<span style=\"color:{BordeSuave};padding:0 10px;\">|</span>";
            var html = string.Join(separador, enlaces.Select(b =>
                $"<a href=\"{A(b.Url)}\" target=\"_blank\" style=\"color:{(b.Peligro ? Peligro : Marca)};font-weight:700;text-decoration:none;white-space:nowrap;\">{E(b.Texto)}</a>"));
            cuerpo.Append($"""<p style="margin:{(primario is null ? 24 : 20)}px 0 0;text-align:center;font-size:13.5px;line-height:2;">{html}</p>""");
        }

        if (!string.IsNullOrWhiteSpace(c.Nota))
            cuerpo.Append($"""<p style="margin:18px 0 0;color:{Gris};font-size:13px;line-height:1.55;text-align:center;">{E(c.Nota)}</p>""");

        if (!string.IsNullOrWhiteSpace(c.Cierre))
        {
            cuerpo.Append($"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:26px 0 0;">
                  <tr><td style="border-top:1px solid #EEF1F0;padding-top:18px;text-align:center;color:{Cuerpo};font-size:13.5px;line-height:1.55;">{Resaltar(c.Cierre)}</td></tr>
                </table>
                """);
        }

        var pie = string.Join("<br>", (c.Pie ?? $"Aviso automático de {c.ClinicaNombre}.")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(E));

        return $"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="color-scheme" content="light"><title>{E(SinMarcas(c.Titulo))}</title>{EnlaceFuentes}</head>
            <body style="margin:0;padding:0;background:{FondoCorreo};font-family:{FuenteTexto};">
              <div style="display:none;max-height:0;overflow:hidden;">{E(c.Introduccion)}</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{FondoCorreo};">
                <tr><td align="center" style="padding:30px 12px 28px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;">
                    <tr><td align="center" style="padding:0 0 18px;">
                      <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                        {logo}
                        <td style="vertical-align:middle;font-family:{FuenteTitulo};font-size:22px;font-weight:600;color:{Texto};">{E(c.ClinicaNombre)}</td>
                      </tr></table>
                    </td></tr>
                    <tr><td style="background:{Tarjeta};border:1px solid {BordeSuave};border-top:6px solid {acento};border-radius:18px;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                        <tr><td style="padding:30px 32px 30px;">
                          <h1 style="margin:0 0 10px;font-family:{FuenteTitulo};font-weight:600;font-size:31px;line-height:1.15;color:{Texto};letter-spacing:-.4px;">{Titulo(c.Titulo, acento)}</h1>
                          {cuerpo}
                        </td></tr>
                      </table>
                    </td></tr>
                    <tr><td style="padding:18px 16px 0;text-align:center;color:{Gris};font-size:12px;line-height:1.6;">{pie}</td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
    }

    /// <summary>Los datos como boleto: la fecha en un bloque de color y al lado las filas.</summary>
    private static string Boleto(Contenido c, string acento)
    {
        var filas = new StringBuilder();
        var ancho = c.FechaDestacada is null ? 120 : 70;
        for (var i = 0; i < c.Datos.Count; i++)
        {
            var d = c.Datos[i];
            var borde = i == c.Datos.Count - 1 ? "" : $"border-bottom:1px dashed {BordeSuave};";
            var detalle = string.IsNullOrWhiteSpace(d.Detalle)
                ? ""
                : $"<div style=\"font-weight:500;color:{Gris};font-size:13px;margin-top:1px;\">{E(d.Detalle)}</div>";
            filas.Append($"""
                <tr>
                  <td width="{ancho}" valign="top" style="padding:11px 10px 11px 0;{borde}color:{Gris};font-size:13px;">{E(d.Etiqueta)}</td>
                  <td valign="top" style="padding:11px 0;{borde}color:{Texto};font-size:14.5px;font-weight:700;">{E(d.Valor)}{detalle}</td>
                </tr>
                """);
        }

        var fecha = c.FechaDestacada is not { } f
            ? ""
            : $"""
                <td width="104" align="center" valign="middle" style="width:104px;background:{acento};border-radius:13px 0 0 13px;padding:16px 4px;color:#FFFFFF;">
                  <div style="font-size:11px;font-weight:800;letter-spacing:1.3px;opacity:.85;">{E(f.DiaSemana.ToUpperInvariant())}</div>
                  <div style="font-family:{FuenteTitulo};font-size:42px;line-height:1.05;font-weight:600;">{E(f.Dia)}</div>
                  <div style="font-size:13px;font-weight:600;opacity:.9;">{E(f.Mes)}</div>
                </td>
                """;

        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border:1px solid {Borde};border-radius:14px;border-collapse:separate;">
              <tr>
                {fecha}
                <td valign="middle" style="padding:4px 18px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0">{filas}</table>
                </td>
              </tr>
            </table>
            """;
    }

    public static string TextoPlano(Contenido c)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SinMarcas(c.Titulo)).AppendLine();
        if (!string.IsNullOrWhiteSpace(c.Saludo)) sb.AppendLine(c.Saludo);
        sb.AppendLine(c.Introduccion).AppendLine();
        if (!string.IsNullOrWhiteSpace(c.MensajePersonal))
        {
            sb.AppendLine($"“{c.MensajePersonal.Trim()}”");
            if (c.MensajeAutor is not null) sb.AppendLine($"— {c.MensajeAutor}");
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(c.Codigo)) sb.AppendLine(c.Codigo).AppendLine();
        if (c.FechaDestacada is { } f) sb.AppendLine($"{f.DiaSemana} {f.Dia} de {f.Mes}");
        foreach (var d in c.Datos)
            sb.AppendLine(d.Detalle is null ? $"{d.Etiqueta}: {d.Valor}" : $"{d.Etiqueta}: {d.Valor} ({d.Detalle})");
        if (c.Botones.Count > 0)
        {
            sb.AppendLine();
            foreach (var b in c.Botones) sb.AppendLine($"{b.Texto}: {b.Url}");
        }
        if (c.Nota is not null) sb.AppendLine().AppendLine(c.Nota);
        if (c.Cierre is not null) sb.AppendLine().AppendLine(SinMarcas(c.Cierre));
        sb.AppendLine().AppendLine($"— {c.ClinicaNombre}");
        return sb.ToString();
    }

    /// <summary>*texto* en negritas y color de marca.</summary>
    private static string Resaltar(string marcado) =>
        Marcas().Replace(E(marcado), m => $"<b style=\"color:{Marca};\">{m.Groups[1].Value}</b>");

    [GeneratedRegex(@"\*([^*]+)\*")]
    private static partial Regex Marcas();

    private static bool EsUrlSegura(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp);

    private static string E(string texto) => WebUtility.HtmlEncode(texto);

    private static string A(string texto) => WebUtility.HtmlEncode(texto);
}
