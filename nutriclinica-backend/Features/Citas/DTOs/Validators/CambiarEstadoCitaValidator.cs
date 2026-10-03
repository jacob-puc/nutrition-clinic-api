using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Citas.DTOs.Validators;

public class CambiarEstadoCitaValidator : AbstractValidator<CambiarEstadoCitaDto>
{
    public CambiarEstadoCitaValidator()
    {
        RuleFor(x => x.Estado)
            .IsInEnum().WithMessage("El estado de la cita no es válido.");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Debes indicar el motivo de la cancelación.")
            .When(x => x.Estado == EstadoCita.Cancelada);

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo no puede tener más de 500 caracteres");
    }
}
