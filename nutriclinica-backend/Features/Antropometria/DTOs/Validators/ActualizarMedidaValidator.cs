using FluentValidation;

namespace nutriclinica_backend.Features.Antropometria.DTOs.Validators;

public class ActualizarMedidaValidator : AbstractValidator<ActualizarMedidaDto>
{
    public ActualizarMedidaValidator()
    {
        RuleFor(x => x.Peso)
            .GreaterThan(0).WithMessage("El peso debe ser mayor a 0 kg.")
            .LessThan(400).WithMessage("Ingresa un peso válido.");

        RuleFor(x => x.Estatura)
            .GreaterThan(0).WithMessage("La estatura debe ser mayor a 0 cm.")
            .LessThan(300).WithMessage("Ingresa una estatura válida en centímetros.");

        RuleFor(x => x.PorcentajeGrasa)
            .InclusiveBetween(0, 100).When(x => x.PorcentajeGrasa.HasValue)
            .WithMessage("El porcentaje de grasa debe estar entre 0% y 100%.");

        RuleFor(x => x.PorcentajeMasaMuscular)
            .InclusiveBetween(0, 100).When(x => x.PorcentajeMasaMuscular.HasValue)
            .WithMessage("El porcentaje de masa muscular debe estar entre 0% y 100%.");

        RuleFor(x => x.MedidaCintura)
            .InclusiveBetween(1, 300).When(x => x.MedidaCintura.HasValue)
            .WithMessage("La medida de cintura debe estar entre 1 cm y 300 cm.");

        RuleFor(x => x.MedidaCadera)
            .InclusiveBetween(1, 300).When(x => x.MedidaCadera.HasValue)
            .WithMessage("La medida de cadera debe estar entre 1 cm y 300 cm.");
    }
}