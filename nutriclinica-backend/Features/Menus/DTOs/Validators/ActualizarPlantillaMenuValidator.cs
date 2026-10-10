using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class ActualizarPlantillaMenuValidator : AbstractValidator<ActualizarPlantillaMenuDto>
{
    public const decimal ObjetivoCaloricoMaximo = 10000;

    public ActualizarPlantillaMenuValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la plantilla es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede superar 1000 caracteres.");

        RuleFor(x => x.ObjetivoCalorico)
            .GreaterThan(0).WithMessage("El objetivo calórico debe ser mayor a 0.")
            .LessThanOrEqualTo(ObjetivoCaloricoMaximo)
            .WithMessage($"El objetivo calórico no puede superar {ObjetivoCaloricoMaximo} kcal.")
            .When(x => x.ObjetivoCalorico.HasValue);

        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("La plantilla debe tener al menos un plato asignado.");

        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearDetallePlantillaMenuValidator());
    }
}
