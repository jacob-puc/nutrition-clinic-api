using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class CrearDetallePlantillaMenuValidator : AbstractValidator<CrearDetallePlantillaMenuDto>
{
    public const decimal CantidadMaxima = 20;

    public CrearDetallePlantillaMenuValidator()
    {
        RuleFor(x => x.PlatoId)
            .NotEmpty().WithMessage("El plato es obligatorio.");

        RuleFor(x => x.DiaSemana)
            .IsInEnum().WithMessage("El día de la semana no es válido.");

        RuleFor(x => x.TiempoComida)
            .IsInEnum().WithMessage("El tiempo de comida no es válido.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.")
            .LessThanOrEqualTo(CantidadMaxima)
            .WithMessage($"La cantidad no puede superar {CantidadMaxima} porciones.");

        RuleFor(x => x.Indicaciones)
            .MaximumLength(500).WithMessage("Las indicaciones no pueden superar 500 caracteres.");
    }
}
