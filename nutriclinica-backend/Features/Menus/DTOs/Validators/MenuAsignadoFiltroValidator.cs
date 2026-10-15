using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class MenuAsignadoFiltroValidator : AbstractValidator<MenuAsignadoFiltroDto>
{
    private static readonly string[] CamposDeOrdenValidos =
    [
        "nombre",
        "fechainicio",
        "fechacreacion"
    ];

    public MenuAsignadoFiltroValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FechaDesde.HasValue || !x.FechaHasta.HasValue
                       || x.FechaDesde.Value <= x.FechaHasta.Value)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");

        RuleFor(x => x.OrdenarPor)
            .Must(valor => string.IsNullOrWhiteSpace(valor)
                           || CamposDeOrdenValidos.Contains(valor.ToLowerInvariant()))
            .WithMessage("El campo de orden no es válido. Usa: nombre, fechainicio o fechacreacion.");
    }
}
