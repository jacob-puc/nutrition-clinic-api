using System.Globalization;

namespace nutriclinica_backend.Core.Utils;

/// <summary>Medidas caseras para leer: "2 piezas", "½ taza", "1½ tazas".</summary>
public static class TextoMedida
{
    public static string? Formatear(decimal? cantidad, string? nombre)
    {
        if (cantidad is null || cantidad <= 0 || string.IsNullOrWhiteSpace(nombre)) return null;

        var unidad = cantidad > 1 ? Plural(nombre.Trim()) : nombre.Trim();
        return $"{Numero(cantidad.Value)} {unidad}";
    }

    /// <summary>
    /// Multiplicada por las porciones del plan. Se redondea al cuarto mas
    /// cercano: "0.38 taza" no se puede servir y los gramos van al lado.
    /// </summary>
    public static string? Escalada(decimal? cantidad, string? nombre, decimal porciones)
    {
        if (cantidad is null) return null;
        var cuartos = decimal.Round(cantidad.Value * porciones * 4m, MidpointRounding.AwayFromZero) / 4m;
        return Formatear(Math.Max(cuartos, 0.25m), nombre);
    }

    private static string Numero(decimal cantidad)
    {
        var entero = decimal.Truncate(cantidad);
        var fraccion = (cantidad - entero) switch
        {
            0.25m => "¼",
            0.5m => "½",
            0.75m => "¾",
            _ => null
        };

        if (fraccion is null) return cantidad.ToString("0.##", CultureInfo.InvariantCulture);
        return entero == 0 ? fraccion : $"{entero:0}{fraccion}";
    }

    /// <summary>Hasta "de": "cucharada de sopa" pasa a "cucharadas de sopa".</summary>
    private static string Plural(string nombre)
    {
        var palabras = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < palabras.Length; i++)
        {
            if (palabras[i].Equals("de", StringComparison.OrdinalIgnoreCase)) break;
            palabras[i] = PluralPalabra(palabras[i]);
        }
        return string.Join(' ', palabras);
    }

    private static string PluralPalabra(string palabra)
    {
        if (palabra.EndsWith('s')) return palabra;
        return "aeiouáéíóú".Contains(char.ToLowerInvariant(palabra[^1])) ? $"{palabra}s" : $"{palabra}es";
    }
}
