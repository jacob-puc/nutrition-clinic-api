using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;

namespace nutriclinica_backend.Infrastructure.Persistence;

/// <summary>
/// Medidas caseras tipicas del catalogo inicial, en gramos aproximados que el
/// nutriologo puede ajustar. Corre una sola vez, con la tabla vacia: despues
/// lo que manda es lo que el equipo capture.
/// </summary>
public static class MedidasCaserasSeeder
{
    private static readonly (string, decimal)[] Aceite = [("cucharadita", 5m), ("cucharada", 14m)];
    private static readonly (string, decimal)[] Leche = [("taza", 240m), ("vaso", 250m)];
    private static readonly (string, decimal)[] CarneMolida = [("taza", 140m), ("cucharada", 15m)];
    private static readonly (string, decimal)[] Frijol = [("taza", 170m), ("cucharada", 15m)];
    private static readonly (string, decimal)[] Yogurt = [("taza", 245m), ("cucharada", 15m)];
    private static readonly (string, decimal)[] Arroz = [("taza", 160m), ("cucharada", 12m)];

    private static readonly Dictionary<string, (string Nombre, decimal Gramos)[]> Medidas = new()
    {
        ["aceite de canola capullo"] = Aceite,
        ["aceite de oliva"] = Aceite,
        ["aceite vegetal nutrioli"] = Aceite,
        ["arroz blanco"] = Arroz,
        ["arroz blanco cocido"] = Arroz,
        ["arroz grano largo chedraui"] = Arroz,
        ["atun dolores en agua"] = [("lata", 100m)],
        ["azucar morena zulka"] = [("cucharadita", 4m), ("cucharada", 12m)],
        ["brocoli cocido"] = [("taza", 155m)],
        ["camote cocido"] = [("pieza", 130m), ("taza", 200m)],
        ["carne molida de cerdo cocida"] = CarneMolida,
        ["carne molida de res cocida"] = CarneMolida,
        ["cebolla cocida"] = [("taza", 210m), ("cucharada", 15m)],
        ["doritos nacho"] = [("bolsa", 58m)],
        ["espagueti aurrera"] = [("taza", 100m)],
        ["espinaca cocida"] = [("taza", 180m)],
        ["frijoles charros la costena"] = [("taza", 240m), ("cucharada", 15m)],
        ["frijoles negros cocidos"] = Frijol,
        ["frijoles negros refritos isadora"] = [("taza", 240m), ("cucharada", 15m)],
        ["frijoles pintos cocidos"] = Frijol,
        ["galletas marias gamesa"] = [("pieza", 6m)],
        ["huevo"] = [("pieza", 50m)],
        ["huevo cocido"] = [("pieza", 50m)],
        ["jitomate cocido"] = [("pieza", 120m), ("taza", 240m)],
        ["jugo de manzana jumex"] = [("vaso", 250m)],
        ["leche descremada"] = Leche,
        ["leche deslactosada great value"] = Leche,
        ["leche entera"] = Leche,
        ["leche santa clara light"] = Leche,
        ["lechuga orejona cruda"] = [("taza", 50m), ("hoja", 15m)],
        ["lentejas cocidas"] = [("taza", 200m), ("cucharada", 12m)],
        ["medias noches bimbo"] = [("pieza", 55m)],
        ["nopales cocidos"] = [("taza", 150m), ("pieza", 90m)],
        ["pan blanco"] = [("rebanada", 28m)],
        ["pan blanco bimbo"] = [("rebanada", 27m)],
        ["pan integral"] = [("rebanada", 30m)],
        ["pan integral bimbo"] = [("rebanada", 30m)],
        ["papa cocida"] = [("pieza", 150m), ("taza", 155m)],
        ["papas sabritas original"] = [("bolsa", 42m)],
        ["pasta cocida"] = [("taza", 140m)],
        ["pechuga de pollo"] = [("filete", 120m)],
        ["pechuga de pollo cocida"] = [("filete", 120m), ("taza", 140m)],
        ["pure de tomate del fuerte"] = [("taza", 240m), ("cucharada", 15m)],
        ["queso panela fud"] = [("rebanada", 30m)],
        ["tilapia cocida"] = [("filete", 90m)],
        ["tortillas de harina del barrio"] = [("pieza", 40m)],
        ["yogurt griego natural lala"] = Yogurt,
        ["yogurt natural"] = Yogurt,
        ["zanahoria cocida"] = [("pieza", 60m), ("taza", 155m)]
    };

    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.MedidasCaseras.AnyAsync())
        {
            return;
        }

        var alimentos = await context.Alimentos
            .AsNoTracking()
            .Select(a => new { a.Id, a.Nombre })
            .ToListAsync();

        var nuevas = new List<MedidaCasera>();

        foreach (var alimento in alimentos)
        {
            if (!Medidas.TryGetValue(Clave(alimento.Nombre), out var medidas))
            {
                continue;
            }

            nuevas.AddRange(medidas.Select((m, orden) => new MedidaCasera
            {
                Id = Guid.NewGuid(),
                AlimentoId = alimento.Id,
                Nombre = m.Nombre,
                Gramos = m.Gramos,
                Orden = orden
            }));
        }

        if (nuevas.Count == 0)
        {
            return;
        }

        context.MedidasCaseras.AddRange(nuevas);
        await context.SaveChangesAsync();

        logger.LogInformation("Seed: {Cantidad} medidas caseras agregadas al catalogo.", nuevas.Count);
    }

    /// <summary>Minusculas y sin acentos ("Brócoli" y "brocoli" son el mismo).</summary>
    private static string Clave(string nombre)
    {
        var limpio = new StringBuilder(nombre.Length);

        foreach (var caracter in nombre.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                limpio.Append(caracter);
            }
        }

        return limpio.ToString();
    }
}
