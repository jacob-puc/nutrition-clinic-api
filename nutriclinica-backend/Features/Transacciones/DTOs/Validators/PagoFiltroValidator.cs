using FluentValidation;

namespace nutriclinica_backend.Features.Transacciones.DTOs.Validators;

public class PagoFiltroValidator : AbstractValidator<PagoFiltroDto>
{
    public PagoFiltroValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FechaInicio.HasValue || !x.FechaFin.HasValue
                       || x.FechaInicio.Value <= x.FechaFin.Value)
            .WithMessage("La fecha inicial no puede superar la fecha final.");
    }
}
