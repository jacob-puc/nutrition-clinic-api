using FluentValidation;

namespace nutriclinica_backend.Features.Transacciones.DTOs.Validators;

public class CrearPagoValidator : AbstractValidator<CrearPagoDto>
{
    public CrearPagoValidator()
    {
        RuleFor(x => x.ConsultaId)
            .NotEmpty().WithMessage("La consulta es obligatoria.");

        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.")
            .LessThanOrEqualTo(100000).WithMessage("El monto no puede superar 100,000 MXN.");

        RuleFor(x => x.MetodoPago)
            .IsInEnum().WithMessage("El método de pago no es válido.");

        RuleFor(x => x.Notas)
            .MaximumLength(500).WithMessage("Las notas no pueden superar 500 caracteres.");
    }
}
