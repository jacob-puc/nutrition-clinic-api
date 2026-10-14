using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class CrearMenuAsignadoValidator : AbstractValidator<CrearMenuAsignadoDto>
{
    public const decimal ObjetivoCaloricoMaximo = 10000;

    public CrearMenuAsignadoValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("El paciente es obligatorio.");

        RuleFor(x => x.PlantillaMenuId)
            .NotEmpty().WithMessage("La plantilla de origen es obligatoria.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del menú es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede superar 1000 caracteres.");

        RuleFor(x => x.ObjetivoCalorico)
            .GreaterThan(0).WithMessage("El objetivo calórico debe ser mayor a 0.")
            .LessThanOrEqualTo(ObjetivoCaloricoMaximo)
            .WithMessage($"El objetivo calórico no puede superar {ObjetivoCaloricoMaximo} kcal.")
            .When(x => x.ObjetivoCalorico.HasValue);

        RuleFor(x => x.FechaInicio)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.Estado)
            .IsInEnum().WithMessage("El estado del menú no es válido.");
    }
}
