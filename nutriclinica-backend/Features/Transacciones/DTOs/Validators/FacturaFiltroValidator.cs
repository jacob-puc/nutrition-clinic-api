using FluentValidation;

namespace nutriclinica_backend.Features.Transacciones.DTOs.Validators;

public class FacturaFiltroValidator : AbstractValidator<FacturaFiltroDto>
{
    public FacturaFiltroValidator()
    {
        RuleFor(x => x.RFC)
            .MaximumLength(13).WithMessage("El RFC no puede superar 13 caracteres.");

        RuleFor(x => x)
            .Must(x => !x.FechaInicio.HasValue || !x.FechaFin.HasValue
                       || x.FechaInicio.Value <= x.FechaFin.Value)
            .WithMessage("La fecha inicial no puede superar la fecha final.");
    }
}
