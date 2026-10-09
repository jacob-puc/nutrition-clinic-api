namespace nutriclinica_backend.Infrastructure.Storage;

public interface IStorageService
{
    Task<string> SubirArchivoAsync(string ruta, byte[] contenido, string contentType);
    Task EliminarArchivoAsync(string ruta);
}
