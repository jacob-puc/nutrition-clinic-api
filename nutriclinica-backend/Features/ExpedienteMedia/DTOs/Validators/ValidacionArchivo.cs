namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;

public static class ValidacionArchivo
{
    public const long MaximoTamanioFotoBytes = 5 * 1024 * 1024;
    public const long MaximoTamanioDocumentoBytes = 10 * 1024 * 1024;

    public const string TextoTamanioFoto = "5 MB";
    public const string TextoTamanioDocumento = "10 MB";

    public static readonly string[] TiposFoto =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public static readonly string[] TiposDocumento =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];
}
