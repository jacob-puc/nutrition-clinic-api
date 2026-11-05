using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class CrearDetallePlatoValidator : AbstractValidator<CrearDetallePlatoDto>
{
    public const decimal RacionMaximaGramos = 2000;
    public const decimal CantidadMaximaMedida = 1000;

    public CrearDetallePlatoValidator()
    {
        RuleFor(x => x.AlimentoId)
            .NotEmpty().WithMessage("El alimento es obligatorio.");

        RuleFor(x => x.RacionGramos)
            .GreaterThan(0).WithMessage("La ración debe ser mayor a 0 gramos.")
            .LessThanOrEqualTo(RacionMaximaGramos)
            .WithMessage($"La ración no puede superar {RacionMaximaGramos} gramos.");

        RuleFor(x => x.MedidaCantidad)
            .NotNull().WithMessage("Indica la cantidad de la medida.")
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.")
            .LessThanOrEqualTo(CantidadMaximaMedida)
            .WithMessage($"La cantidad no puede superar {CantidadMaximaMedida}.")
            .When(x => !string.IsNullOrWhiteSpace(x.MedidaNombre));

        RuleFor(x => x.MedidaNombre)
            .MaximumLength(40).WithMessage("El nombre de la medida no puede superar 40 caracteres.");
    }
}
