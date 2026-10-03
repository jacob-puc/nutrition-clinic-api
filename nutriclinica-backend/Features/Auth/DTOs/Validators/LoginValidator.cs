using FluentValidation;
using nutriclinica_backend.Features.Auth.DTOs;

namespace nutriclinica_backend.Features.Auth.DTOs.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(d => d.CorreoElectronico)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

        RuleFor(d => d.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
