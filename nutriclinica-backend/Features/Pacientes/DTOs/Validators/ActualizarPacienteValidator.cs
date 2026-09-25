using FluentValidation;

namespace nutriclinica_backend.Features.Pacientes.DTOs.Validators;

public class ActualizarPacienteValidator : AbstractValidator<ActualizarPacienteDto>
{
    public ActualizarPacienteValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres");

        RuleFor(x => x.CorreoElectronico)
            .NotEmpty().WithMessage("El correo es requerido")
            .EmailAddress().WithMessage("El correo no es válido");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage("El número de teléfono no tiene un formato válido.");

        RuleFor(x => x.FechaNacimiento)
            .Must(f => f <= DateTime.UtcNow.Date)
            .WithMessage("La fecha de nacimiento no puede ser futura.")
            .When(x => x.FechaNacimiento.HasValue);

        RuleFor(x => x.Sexo)
            .Must(s => string.IsNullOrEmpty(s) || new[] { "M", "F", "Otro" }.Contains(s))
            .WithMessage("El sexo debe ser 'M', 'F' u 'Otro'.");
    }
}