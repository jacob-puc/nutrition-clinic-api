using FluentValidation;

namespace nutriclinica_backend.Features.Consultas.DTOs.Validators;

public class CerrarConsultaValidator : AbstractValidator<CerrarConsultaDto>
{
    public CerrarConsultaValidator()
    {
        RuleFor(x => x.NotasClinicas)
            .MaximumLength(4000).WithMessage("Las notas no pueden tener más de 4000 caracteres");
    }
}
