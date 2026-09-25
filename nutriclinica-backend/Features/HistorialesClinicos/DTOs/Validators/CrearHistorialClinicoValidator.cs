using FluentValidation;

namespace nutriclinica_backend.Features.HistorialesClinicos.DTOs.Validators;

public class CrearHistorialClinicoValidator : AbstractValidator<CrearHistorialClinicoDto>
{
    public CrearHistorialClinicoValidator()
    {
        RuleForEach(x => x.Alergias)
            .NotEmpty().WithMessage("Las alergias no pueden contener elementos vacíos.");

        RuleForEach(x => x.AlimentosFavoritos)
            .NotEmpty().WithMessage("Los alimentos favoritos no pueden contener elementos vacíos.");

        RuleForEach(x => x.AlimentosNoFavoritos)
            .NotEmpty().WithMessage("Los alimentos no favoritos no pueden contener elementos vacíos.");
    }
}