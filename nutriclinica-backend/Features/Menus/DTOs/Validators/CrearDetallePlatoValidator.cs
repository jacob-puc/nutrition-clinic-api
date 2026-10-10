using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class CrearDetallePlatoValidator : AbstractValidator<CrearDetallePlatoDto>
{
    public const decimal RacionMaximaGramos = 2000;

    public CrearDetallePlatoValidator()
    {
        RuleFor(x => x.AlimentoId)
            .NotEmpty().WithMessage("El alimento es obligatorio.");

        RuleFor(x => x.RacionGramos)
            .GreaterThan(0).WithMessage("La ración debe ser mayor a 0 gramos.")
            .LessThanOrEqualTo(RacionMaximaGramos)
            .WithMessage($"La ración no puede superar {RacionMaximaGramos} gramos.");
    }
}
