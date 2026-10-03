using FluentValidation;
using nutriclinica_backend.Features.Auth.DTOs;

namespace nutriclinica_backend.Features.Auth.DTOs.Validators;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(d => d.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es obligatorio.");
    }
}
