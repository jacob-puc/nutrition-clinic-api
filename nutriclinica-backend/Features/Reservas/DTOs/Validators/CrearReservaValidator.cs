using FluentValidation;

namespace nutriclinica_backend.Features.Reservas.DTOs.Validators;

public class CrearReservaValidator : AbstractValidator<CrearReservaDto>
{
    public CrearReservaValidator()
    {
        RuleFor(x => x.Inicio)
            .NotEmpty().WithMessage("Elige un horario.");

        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("Escribe tu nombre.")
            .MinimumLength(3).WithMessage("Escribe tu nombre completo.")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres.");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("Escribe tu teléfono.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Escribe tu teléfono a 10 dígitos, sin espacios.");

        RuleFor(x => x.CorreoElectronico)
            .NotEmpty().WithMessage("Escribe tu correo.")
            .EmailAddress().WithMessage("El correo no es válido.")
            .MaximumLength(150);

        RuleFor(x => x.Sexo)
            .IsInEnum().WithMessage("Elige una opción.");

        RuleFor(x => x.Motivo)
            .MaximumLength(300).WithMessage("El motivo no puede tener más de 300 caracteres.");
    }
}
