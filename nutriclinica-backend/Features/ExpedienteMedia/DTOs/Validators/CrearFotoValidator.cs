using FluentValidation;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;

public class CrearFotoValidator : AbstractValidator<CrearFotoDto>
{
    public CrearFotoValidator()
    {
        RuleFor(x => x.UrlFoto)
            .NotEmpty().WithMessage("La URL de la foto es obligatoria.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("La URL de la foto no tiene un formato válido.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de foto seleccionado no es válido.");
    }
}