using FluentValidation;

namespace nutriclinica_backend.Features.Alimentos.DTOs.Validators;

public class ActualizarAlimentoValidator : AbstractValidator<ActualizarAlimentoDto>
{
    public const decimal CaloriasMaximas = 900;
    public const decimal GramosMaximos = 100;

    public ActualizarAlimentoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del alimento es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Calorias)
            .GreaterThanOrEqualTo(0).WithMessage("Las calorías no pueden ser negativas.")
            .LessThanOrEqualTo(CaloriasMaximas)
            .WithMessage($"Las calorías no pueden superar {CaloriasMaximas} por porción de 100 g.");

        RuleFor(x => x.Proteinas)
            .GreaterThanOrEqualTo(0).WithMessage("Las proteínas no pueden ser negativas.")
            .LessThanOrEqualTo(GramosMaximos)
            .WithMessage($"Las proteínas no pueden superar {GramosMaximos} g por porción de 100 g.");

        RuleFor(x => x.Carbohidratos)
            .GreaterThanOrEqualTo(0).WithMessage("Los carbohidratos no pueden ser negativos.")
            .LessThanOrEqualTo(GramosMaximos)
            .WithMessage($"Los carbohidratos no pueden superar {GramosMaximos} g por porción de 100 g.");

        RuleFor(x => x.Grasas)
            .GreaterThanOrEqualTo(0).WithMessage("Las grasas no pueden ser negativas.")
            .LessThanOrEqualTo(GramosMaximos)
            .WithMessage($"Las grasas no pueden superar {GramosMaximos} g por porción de 100 g.");

        RuleFor(x => x.ImagenUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("La URL de la imagen no tiene un formato válido.");
    }
}
