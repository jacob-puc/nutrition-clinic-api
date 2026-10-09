using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace nutriclinica_backend.Infrastructure.Storage;

public class SupabaseStorageService : IStorageService
{
    private const int ValidezUrlFirmadaSegundos = 3600;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly StorageConfig _config;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        IHttpClientFactory httpClientFactory,
        StorageConfig config,
        ILogger<SupabaseStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<string> SubirArchivoAsync(string bucket, string ruta, byte[] contenido, string contentType)
    {
        _config.ValidarParaUso();

        var url = ConstruirUrlObjeto(bucket, ruta, "object");

        using var contenidoHttp = new ByteArrayContent(contenido);
        contenidoHttp.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = contenidoHttp
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.StorageKey);
        request.Headers.Add("x-upsert", "true");

        var cliente = _httpClientFactory.CreateClient("supabase-storage");
        var respuesta = await cliente.SendAsync(request);

        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            _logger.LogError(
                "Fallo la subida a Supabase Storage. Bucket {Bucket}. Status {Status}. {Cuerpo}",
                bucket,
                (int)respuesta.StatusCode,
                cuerpo);

            throw new InvalidOperationException(
                $"No se pudo subir el archivo a Supabase Storage (HTTP {(int)respuesta.StatusCode}).");
        }

        return ruta;
    }

    public string GenerarUrlPublica(string bucket, string ruta)
    {
        _config.ValidarParaUso();
        return ConstruirUrlObjeto(bucket, ruta, "object/public");
    }

    public async Task<string> GenerarUrlFirmadaAsync(string bucket, string ruta)
    {
        _config.ValidarParaUso();

        var url = ConstruirUrlObjeto(bucket, ruta, "object/sign");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { expiresIn = ValidezUrlFirmadaSegundos }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.StorageKey);

        var cliente = _httpClientFactory.CreateClient("supabase-storage");
        var respuesta = await cliente.SendAsync(request);

        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            _logger.LogError(
                "Fallo la firma de URL en Supabase Storage. Bucket {Bucket}. Status {Status}. {Cuerpo}",
                bucket,
                (int)respuesta.StatusCode,
                cuerpo);

            throw new InvalidOperationException(
                $"No se pudo generar la URL firmada del archivo (HTTP {(int)respuesta.StatusCode}).");
        }

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var rutaFirmada = json.RootElement.GetProperty("signedURL").GetString();

        if (string.IsNullOrWhiteSpace(rutaFirmada))
        {
            throw new InvalidOperationException("Supabase Storage no devolvió ninguna URL firmada.");
        }

        return $"{_config.Url.TrimEnd('/')}/storage/v1{rutaFirmada}";
    }

    public async Task EliminarArchivoAsync(string bucket, string ruta)
    {
        _config.ValidarParaUso();

        var request = new HttpRequestMessage(HttpMethod.Delete, ConstruirUrlObjeto(bucket, ruta, "object"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.StorageKey);

        var cliente = _httpClientFactory.CreateClient("supabase-storage");
        var respuesta = await cliente.SendAsync(request);

        if (!respuesta.IsSuccessStatusCode && respuesta.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "No se pudo eliminar {Ruta} del bucket {Bucket}. Status {Status}. {Cuerpo}",
                ruta,
                bucket,
                (int)respuesta.StatusCode,
                cuerpo);
        }
    }

    private string ConstruirUrlObjeto(string bucket, string ruta, string prefijo)
    {
        return $"{_config.Url.TrimEnd('/')}/storage/v1/{prefijo}/{bucket}/{ruta}";
    }
}
