using FluentValidation;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;

public class ActualizarDocumentoValidator : AbstractValidator<ActualizarDocumentoDto>
{
    public ActualizarDocumentoValidator()
    {
        RuleFor(x => x.NombreDocumento)
            .NotEmpty().WithMessage("El nombre del documento es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre del documento no puede exceder 200 caracteres.");

        RuleFor(x => x.UrlDocumento)
            .NotEmpty().WithMessage("La URL del documento es obligatoria.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("La URL del documento no tiene un formato válido.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de documento seleccionado no es válido.");
    }
}