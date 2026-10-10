using FluentValidation;

namespace nutriclinica_backend.Features.Menus.DTOs.Validators;

public class CrearPlatoValidator : AbstractValidator<CrearPlatoDto>
{
    public CrearPlatoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del plato es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede superar 1000 caracteres.");

        RuleFor(x => x.ImagenUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("La URL de la imagen no tiene un formato válido.");

        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("El plato debe tener al menos un alimento.");

        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearDetallePlatoValidator());

        RuleFor(x => x.Detalles)
            .Must(detalles => detalles.Select(d => d.AlimentoId).Distinct().Count() == detalles.Count)
            .WithMessage("El mismo alimento no puede repetirse dentro del plato. Suma sus gramajes en una sola fila.");
    }
}
