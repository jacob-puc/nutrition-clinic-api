using FluentValidation;

namespace nutriclinica_backend.Features.Alimentos.DTOs.Validators;

public class AlimentoFiltroValidator : AbstractValidator<AlimentoFiltroDto>
{
    private static readonly string[] CamposDeOrdenValidos =
    [
        "nombre",
        "calorias",
        "proteinas",
        "carbohidratos",
        "grasas",
        "fechacreacion"
    ];

    public AlimentoFiltroValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(150).WithMessage("El texto de búsqueda no puede superar 150 caracteres.");

        RuleFor(x => x)
            .Must(x => !x.CaloriasMinimas.HasValue || !x.CaloriasMaximas.HasValue
                       || x.CaloriasMinimas.Value <= x.CaloriasMaximas.Value)
            .WithMessage("El mínimo de calorías no puede superar el máximo.");

        RuleFor(x => x.OrdenarPor)
            .Must(valor => string.IsNullOrWhiteSpace(valor)
                           || CamposDeOrdenValidos.Contains(valor.ToLowerInvariant()))
            .WithMessage(
                "El campo de orden no es válido. Usa: nombre, calorias, proteinas, "
                + "carbohidratos, grasas o fechacreacion.");
    }
}
