using FluentValidation;

namespace nutriclinica_backend.Features.Transacciones.DTOs.Validators;

public class ActualizarDatosFiscalesClinicaValidator : AbstractValidator<ActualizarDatosFiscalesClinicaDto>
{
    public ActualizarDatosFiscalesClinicaValidator()
    {
        RuleFor(x => x.RazonSocial)
            .NotEmpty().WithMessage("La razón social es obligatoria.")
            .MaximumLength(200).WithMessage("La razón social no puede superar 200 caracteres.");

        RuleFor(x => x.RFC)
            .NotEmpty().WithMessage("El RFC es obligatorio.")
            .Must(rfc => System.Text.RegularExpressions.Regex.IsMatch(
                rfc?.Trim().ToUpperInvariant() ?? string.Empty,
                "^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$"))
            .WithMessage("El RFC no tiene un formato válido.");

        RuleFor(x => x.RegimenFiscal)
            .IsInEnum().WithMessage("El régimen fiscal no es válido.");

        RuleFor(x => x.CodigoPostal)
            .NotEmpty().WithMessage("El código postal es obligatorio.")
            .Matches("^[0-9]{5}$").WithMessage("El código postal debe tener 5 dígitos.");

        RuleFor(x => x.Direccion)
            .MaximumLength(300).WithMessage("La dirección no puede superar 300 caracteres.");

        RuleFor(x => x.Serie)
            .NotEmpty().WithMessage("La serie es obligatoria.")
            .MaximumLength(10).WithMessage("La serie no puede superar 10 caracteres.");

        RuleFor(x => x.TarifaIva)
            .InclusiveBetween(0, 100).WithMessage("La tarifa de IVA debe estar entre 0 y 100.");
    }
}
