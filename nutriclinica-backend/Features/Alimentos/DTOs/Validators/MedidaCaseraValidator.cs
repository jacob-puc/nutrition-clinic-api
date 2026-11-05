using FluentValidation;

namespace nutriclinica_backend.Features.Alimentos.DTOs.Validators;

public class MedidaCaseraValidator : AbstractValidator<MedidaCaseraDto>
{
    public const int MaximoPorAlimento = 8;
    public const decimal GramosMaximos = 2000;

    public MedidaCaseraValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Ponle nombre a la medida.")
            .MaximumLength(40).WithMessage("El nombre de la medida no puede superar 40 caracteres.");

        RuleFor(x => x.Gramos)
            .GreaterThan(0).WithMessage("La medida debe pesar más de 0 g.")
            .LessThanOrEqualTo(GramosMaximos).WithMessage($"Una medida no puede superar {GramosMaximos} g.");
    }

    /// <summary>Reglas de la lista: tope y sin nombres repetidos.</summary>
    public static bool NombresUnicos(IEnumerable<MedidaCaseraDto> medidas)
    {
        var nombres = medidas.Select(m => m.Nombre.Trim().ToLowerInvariant()).ToList();
        return nombres.Distinct().Count() == nombres.Count;
    }
}
