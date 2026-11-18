using System.Net;
using System.Text.RegularExpressions;

namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>
/// Identidad del sitio publico de la web (paginas de inicio y /reservar):
/// fondo menta, titulos en Fraunces con una palabra en cursiva de color,
/// texto en Plus Jakarta Sans, logo de hoja en circulo con degradado y
/// tarjetas muy redondeadas. La usan los correos y las paginas que sirve el
/// backend (confirmar cita) para que todo se vea como un mismo producto.
/// </summary>
public static partial class IdentidadMarca
{
    public const string Fondo = "#DDF0EC";
    public const string Tarjeta = "#FFFEFC";
    public const string Menta = "#C8EDD5";
    public const string Crema = "#F1F8F2";
    public const string Marca = "#00625A";
    public const string Peligro = "#B4232A";
    public const string Texto = "#211F1A";
    public const string Apagado = "#3F5A5C";
    public const string Borde = "#C9E3DC";

    public const string FuenteTexto = "'Plus Jakarta Sans',Segoe UI,Helvetica,Arial,sans-serif";
    public const string FuenteTitulo = "Fraunces,Georgia,'Times New Roman',serif";

    public const string EnlaceFuentes =
        "<link href=\"https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght@0,9..144,400;1,9..144,400"
        + "&family=Plus+Jakarta+Sans:wght@400;600;700;800&display=swap\" rel=\"stylesheet\">";

    /// <summary>Hoja de lucide (la misma del logo de la web), en blanco.</summary>
    public const string HojaSvg =
        "<svg width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#fff\" stroke-width=\"2.25\" "
        + "stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">"
        + "<path d=\"M11 20a10 10 0 0010-10 25.9 25.9 0 00-1.04-7.281 1 1 0 00-1.755-.325C15.833 5.5 13 5.5 9.8 6.1A7 7 0 0011 20\"/>"
        + "<path d=\"M2 21a5 5 0 012.911-4.544C7.613 15.212 8.351 15.24 11 13\"/></svg>";

    public const string Degradado = "linear-gradient(135deg,#00625A 0%,#19938A 55%,#6B9786 100%)";

    /// <summary>
    /// Titulo con enfasis: "Tu cita quedó *agendada*" pone "agendada" en
    /// cursiva y color, como los titulos del sitio publico.
    /// </summary>
    public static string Titulo(string marcado, string colorEnfasis) =>
        Enfasis().Replace(WebUtility.HtmlEncode(marcado),
            m => $"<em style=\"font-style:italic;color:{colorEnfasis};\">{m.Groups[1].Value}</em>");

    /// <summary>El mismo titulo sin marcas, para texto plano y asuntos.</summary>
    public static string SinMarcas(string marcado) => marcado.Replace("*", "");

    [GeneratedRegex(@"\*([^*]+)\*")]
    private static partial Regex Enfasis();
}
