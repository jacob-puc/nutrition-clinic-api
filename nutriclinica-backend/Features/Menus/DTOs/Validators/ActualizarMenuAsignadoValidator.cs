using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class ActualizarMenuAsignadoValidator : AbstractValidator<ActualizarMenuAsignadoDto>
{
    public const decimal ObjetivoCaloricoMaximo = 10000;

    public ActualizarMenuAsignadoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del menu asignado es requerido")
            .MaximumLength(150).WithMessage("El nombre del menu no puede superar los 150 caracteres.");
        RuleFor(x=> x.Descripcion)
            .MaximumLength(1000).WithMessage("La descripcion no puede superar los 1000 caracteres.");
        RuleFor(x => x.ObjetivoCalorico)
            .GreaterThan(0).WithMessage("El objetivo calórico del menu asignado debe ser mayor a 0")
            .LessThanOrEqualTo(ObjetivoCaloricoMaximo)
            .WithMessage($"El objetivo calórico no puede superar {ObjetivoCaloricoMaximo} kcal.")
            .When(x => x.ObjetivoCalorico.HasValue);

        RuleFor(x => x.Estado)
            .IsInEnum().WithMessage("El estado del menú asignado no es válido.");

        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("El menú asignado debe tener al menos un plato.");

        RuleForEach(x => x.Detalles)
            .SetValidator(new ActualizarDetallesMenuAsignadoValidator());
    }
}