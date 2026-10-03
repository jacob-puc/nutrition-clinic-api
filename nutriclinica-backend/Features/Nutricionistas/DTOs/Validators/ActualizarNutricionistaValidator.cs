using FluentValidation;

namespace nutriclinica_backend.Features.Nutricionistas.DTOs.Validators;

public class ActualizarNutricionistaValidator : AbstractValidator<ActualizarNutricionistaDto>
{
    public ActualizarNutricionistaValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres");

        RuleFor(x => x.CorreoElectronico)
            .NotEmpty().WithMessage("El correo es requerido")
            .EmailAddress().WithMessage("El correo no es válido")
            .MaximumLength(200).WithMessage("El correo no puede tener más de 200 caracteres");

        RuleFor(x => x.Telefono)
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage("El número de teléfono no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Telefono));

        RuleFor(x => x.NumeroColegiatura)
            .MaximumLength(50).WithMessage("El número de colegiatura no puede tener más de 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.NumeroColegiatura));

        RuleFor(x => x.Especialidad)
            .MaximumLength(100).WithMessage("La especialidad no puede tener más de 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Especialidad));
    }
}
