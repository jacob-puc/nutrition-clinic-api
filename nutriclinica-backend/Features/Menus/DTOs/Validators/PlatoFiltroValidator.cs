using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class PlatoFiltroValidator : AbstractValidator<PlatoFiltroDto>
{
    private static readonly string[] CamposDeOrdenValidos =
    [
        "nombre",
        "calorias",
        "fechacreacion"
    ];

    public PlatoFiltroValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(150).WithMessage("El texto de búsqueda no puede superar 150 caracteres.");

        RuleFor(x => x.OrdenarPor)
            .Must(valor => string.IsNullOrWhiteSpace(valor)
                           || CamposDeOrdenValidos.Contains(valor.ToLowerInvariant()))
            .WithMessage("El campo de orden no es válido. Usa: nombre, calorias o fechacreacion.");
    }
}
