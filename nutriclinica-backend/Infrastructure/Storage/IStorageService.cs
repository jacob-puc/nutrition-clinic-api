namespace nutriclinica_backend.Infrastructure.Storage;

public interface IStorageService
{
    Task<string> SubirArchivoAsync(string bucket, string ruta, byte[] contenido, string contentType);
    string GenerarUrlPublica(string bucket, string ruta);
    Task<string> GenerarUrlFirmadaAsync(string bucket, string ruta);
    Task EliminarArchivoAsync(string bucket, string ruta);
}
