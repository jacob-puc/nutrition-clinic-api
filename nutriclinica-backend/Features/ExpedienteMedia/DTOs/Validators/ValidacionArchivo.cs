namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;

public static class ValidacionArchivo
{
    public const long MaximoTamanioBytes = 10 * 1024 * 1024;

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

    public static string TamanioEnMegabytes => $"{MaximoTamanioBytes / 1024 / 1024} MB";
}
