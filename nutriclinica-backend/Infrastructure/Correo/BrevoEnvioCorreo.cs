using System.Net.Http.Json;
using System.Text;

namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>
/// Envio por la API HTTP de Brevo (no usa puertos SMTP, que algunos hostings
/// bloquean). La invitacion de calendario va como adjunto .ics: Gmail y
/// Outlook la reconocen y ofrecen agregarla.
/// </summary>
public class BrevoEnvioCorreo : IEnvioCorreo
{
    public const string ClienteHttp = "brevo";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CorreoConfig _config;

    public BrevoEnvioCorreo(IHttpClientFactory httpClientFactory, CorreoConfig config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public string Nombre => "Brevo";

    public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_config.BrevoApiKey) || string.IsNullOrWhiteSpace(_config.Remitente))
            throw new CorreoNoConfiguradoException("Faltan Correo__BrevoApiKey o Correo__RemitenteCorreo.");

        var adjuntos = mensaje.Adjuntos
            .Select(a => new { name = a.Nombre, content = Convert.ToBase64String(a.Contenido) })
            .ToList();
        if (mensaje.Ics is not null)
        {
            adjuntos.Add(new
            {
                name = mensaje.IcsMetodo == "CANCEL" ? "cancelacion.ics" : "invitacion.ics",
                content = Convert.ToBase64String(Encoding.UTF8.GetBytes(mensaje.Ics))
            });
        }

        var cuerpo = new Dictionary<string, object?>
        {
            ["sender"] = new { name = _config.RemitenteNombre, email = _config.Remitente },
            ["to"] = new[] { new { email = mensaje.Para, name = mensaje.ParaNombre ?? mensaje.Para } },
            ["subject"] = mensaje.Asunto,
            ["htmlContent"] = mensaje.Html,
            ["textContent"] = mensaje.Texto,
        };
        if (adjuntos.Count > 0) cuerpo["attachment"] = adjuntos;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(cuerpo)
        };
        request.Headers.Add("api-key", _config.BrevoApiKey);
        request.Headers.Add("accept", "application/json");

        var respuesta = await _httpClientFactory.CreateClient(ClienteHttp).SendAsync(request, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Brevo respondio HTTP {(int)respuesta.StatusCode}: {Recortar(detalle, 500)}");
        }
    }

    private static string Recortar(string texto, int max) => texto.Length <= max ? texto : texto[..max];
}
