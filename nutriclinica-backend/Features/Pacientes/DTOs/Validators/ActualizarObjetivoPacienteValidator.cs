using FluentValidation;
using nutriclinica_backend.Features.Pacientes.DTOs;

namespace nutriclinica_backend.Features.Pacientes.DTOs.Validators;

public class ActualizarObjetivoPacienteValidator
    : AbstractValidator<ActualizarObjetivoPacienteDto>
{
    public ActualizarObjetivoPacienteValidator()
    {
        RuleFor(x => x.TituloObjetivo)
            .MaximumLength(80)
            .WithMessage("El titulo del objetivo no puede tener mas de 80 caracteres.");

        RuleFor(x => x.PesoObjetivo)
            .GreaterThan(0)
            .WithMessage("El peso objetivo debe ser mayor a 0 kg.")
            .When(x => x.PesoObjetivo.HasValue);

        RuleFor(x => x.PesoObjetivo)
            .LessThan(400)
            .WithMessage("Ingresa un peso objetivo valido.")
            .When(x => x.PesoObjetivo.HasValue);
    }
}
