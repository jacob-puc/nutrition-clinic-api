using System.Net.Http.Headers;

namespace nutriclinica_backend.Infrastructure.Storage;

public class SupabaseStorageService : IStorageService
{
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

    public async Task<string> SubirArchivoAsync(string ruta, byte[] contenido, string contentType)
    {
        _config.ValidarParaUso();

        var url = ConstruirUrlObjeto(ruta, publico: false);

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
                "Fallo la subida a Supabase Storage. Status {Status}. Respuesta: {Cuerpo}",
                (int)respuesta.StatusCode,
                cuerpo);

            throw new InvalidOperationException(
                $"No se pudo subir el archivo a Supabase Storage (HTTP {(int)respuesta.StatusCode}).");
        }

        return ConstruirUrlPublica(ruta);
    }

    public async Task EliminarArchivoAsync(string ruta)
    {
        _config.ValidarParaUso();

        var request = new HttpRequestMessage(HttpMethod.Delete, ConstruirUrlObjeto(ruta, publico: false));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.StorageKey);

        var cliente = _httpClientFactory.CreateClient("supabase-storage");
        var respuesta = await cliente.SendAsync(request);

        if (!respuesta.IsSuccessStatusCode && respuesta.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "No se pudo eliminar {Ruta} de Supabase Storage. Status {Status}. {Cuerpo}",
                ruta,
                (int)respuesta.StatusCode,
                cuerpo);
        }
    }

    private string ConstruirUrlObjeto(string ruta, bool publico)
    {
        var baseUri = _config.Url.TrimEnd('/');
        var segmento = publico ? "/public" : string.Empty;
        return $"{baseUri}/storage/v1/object{segmento}/{_config.Bucket}/{ruta}";
    }

    private string ConstruirUrlPublica(string ruta) => ConstruirUrlObjeto(ruta, publico: true);
}
