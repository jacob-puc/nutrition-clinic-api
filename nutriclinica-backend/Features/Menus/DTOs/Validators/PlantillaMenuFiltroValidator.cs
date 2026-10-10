using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class PlantillaMenuFiltroValidator : AbstractValidator<PlantillaMenuFiltroDto>
{
    private static readonly string[] CamposDeOrdenValidos =
    [
        "nombre",
        "objetivocalorico",
        "fechacreacion"
    ];

    public PlantillaMenuFiltroValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(150).WithMessage("El texto de búsqueda no puede superar 150 caracteres.");

        RuleFor(x => x)
            .Must(x => !x.ObjetivoCaloricoMinimo.HasValue || !x.ObjetivoCaloricoMaximo.HasValue
                       || x.ObjetivoCaloricoMinimo.Value <= x.ObjetivoCaloricoMaximo.Value)
            .WithMessage("El objetivo calórico mínimo no puede superar el máximo.");

        RuleFor(x => x.OrdenarPor)
            .Must(valor => string.IsNullOrWhiteSpace(valor)
                           || CamposDeOrdenValidos.Contains(valor.ToLowerInvariant()))
            .WithMessage("El campo de orden no es válido. Usa: nombre, objetivocalorico o fechacreacion.");
    }
}
