using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Citas.DTOs.Validators;

public class CrearCitaValidator : AbstractValidator<CrearCitaDto>
{
    public CrearCitaValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("El paciente es requerido.");

        RuleFor(x => x.NutricionistaId)
            .NotEmpty().WithMessage("El nutricionista es requerido.");

        RuleFor(x => x.FechaFin)
            .GreaterThan(x => x.FechaInicio).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.FechaInicio)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("La fecha de inicio no puede estar en el pasado.");

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo no puede tener más de 500 caracteres");
    }
}
