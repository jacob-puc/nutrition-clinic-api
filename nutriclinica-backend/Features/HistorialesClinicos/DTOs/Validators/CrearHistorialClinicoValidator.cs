using FluentValidation;

namespace nutriclinica_backend.Features.HistorialesClinicos.DTOs.Validators;

public class CrearHistorialClinicoValidator : AbstractValidator<CrearHistorialClinicoDto>
{
    private const int MaximoElementos = 30;
    private const int LongitudMaxima = 100;

    public CrearHistorialClinicoValidator()
    {
        Lista(x => x.Enfermedades, "enfermedades");
        Lista(x => x.Alergias, "alergias");
        Lista(x => x.AlimentosFavoritos, "alimentos favoritos");
        Lista(x => x.AlimentosNoFavoritos, "alimentos no favoritos");
    }

    private void Lista(System.Linq.Expressions.Expression<Func<CrearHistorialClinicoDto, IEnumerable<string>>> lista, string nombre)
    {
        RuleFor(lista)
            .NotNull().WithMessage($"La lista de {nombre} es obligatoria.")
            .Must(l => l is null || l.Count() <= MaximoElementos)
            .WithMessage($"Registra como máximo {MaximoElementos} {nombre}.");

        RuleForEach(lista)
            .NotEmpty().WithMessage($"Las {nombre} no pueden contener elementos vacíos.")
            .MaximumLength(LongitudMaxima).WithMessage($"Cada elemento de {nombre} admite hasta {LongitudMaxima} caracteres.");
    }
}
