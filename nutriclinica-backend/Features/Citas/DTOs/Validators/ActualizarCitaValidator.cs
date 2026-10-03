using FluentValidation;

namespace nutriclinica_backend.Features.Citas.DTOs.Validators;

public class ActualizarCitaValidator : AbstractValidator<ActualizarCitaDto>
{
    public ActualizarCitaValidator()
    {
        RuleFor(x => x.NutricionistaId)
            .NotEmpty().WithMessage("El nutricionista es requerido.");

        RuleFor(x => x.FechaFin)
            .GreaterThan(x => x.FechaInicio).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo no puede tener más de 500 caracteres");
    }
}
