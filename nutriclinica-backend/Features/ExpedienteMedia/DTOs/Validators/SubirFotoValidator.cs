using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;

public class SubirFotoValidator : AbstractValidator<SubirFotoDto>
{
    public SubirFotoValidator()
    {
        RuleFor(x => x.Archivo)
            .NotNull().WithMessage("Debes adjuntar un archivo de imagen.")
            .Must(archivo => archivo!.Length > 0).WithMessage("El archivo está vacío.")
            .Must(archivo => archivo!.Length <= ValidacionArchivo.MaximoTamanioBytes)
            .WithMessage($"La imagen no puede superar {ValidacionArchivo.TamanioEnMegabytes}.")
            .Must(archivo => ValidacionArchivo.TiposFoto.Contains(archivo!.ContentType.ToLower()))
            .WithMessage("Solo se permiten imágenes JPEG, PNG o WEBP.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de foto seleccionado no es válido.");

        RuleFor(x => x.Notas)
            .MaximumLength(1000).WithMessage("Las notas no pueden superar 1000 caracteres.");
    }
}
