using FluentValidation;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;

public class SubirDocumentoValidator : AbstractValidator<SubirDocumentoDto>
{
    public SubirDocumentoValidator()
    {
        RuleFor(x => x.Archivo)
            .NotNull().WithMessage("Debes adjuntar un documento.")
            .Must(archivo => archivo!.Length > 0).WithMessage("El archivo está vacío.")
            .Must(archivo => archivo!.Length <= ValidacionArchivo.MaximoTamanioBytes)
            .WithMessage($"El documento no puede superar {ValidacionArchivo.TamanioEnMegabytes}.")
            .Must(archivo => ValidacionArchivo.TiposDocumento.Contains(archivo!.ContentType.ToLower()))
            .WithMessage("Solo se admiten PDF, imágenes JPEG/PNG y documentos Word (DOC o DOCX).");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de documento seleccionado no es válido.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000).WithMessage("Las observaciones no pueden superar 1000 caracteres.");
    }
}
