using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Consultas.DTOs.Validators;

public class CrearConsultaValidator : AbstractValidator<CrearConsultaDto>
{
    public CrearConsultaValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("El paciente es requerido.");

        RuleFor(x => x.TipoConsulta)
            .IsInEnum().WithMessage("El tipo de consulta seleccionado no es válido.");

        // Sin cita (atención sin agendar) el nutricionista es obligatorio.
        RuleFor(x => x.NutricionistaId)
            .NotEmpty().WithMessage("El nutricionista es requerido cuando la consulta no proviene de una cita.")
            .When(x => !x.CitaId.HasValue);

        RuleFor(x => x.NotasClinicas)
            .MaximumLength(4000).WithMessage("Las notas no pueden tener más de 4000 caracteres");
    }
}
