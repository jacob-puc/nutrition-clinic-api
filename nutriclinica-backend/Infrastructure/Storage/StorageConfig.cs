using nutriclinica_backend.Core.Exceptions;

namespace nutriclinica_backend.Infrastructure.Storage;

public class StorageConfig
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "expediente";

    public void ValidarParaUso()
    {
        if (string.IsNullOrWhiteSpace(Url) || string.IsNullOrWhiteSpace(StorageKey))
        {
            throw new StorageNoConfiguradoException(
                "La subida de archivos no está configurada en el servidor. "
                + "Defina las variables de entorno Supabase__Url y Supabase__StorageKey.");
        }
    }
}
