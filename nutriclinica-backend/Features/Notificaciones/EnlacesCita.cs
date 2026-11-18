using System.Security.Cryptography;
using System.Text;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Features.Notificaciones;

/// <summary>
/// Enlaces firmados para que el paciente confirme o cancele su cita desde
/// el correo, sin iniciar sesion. La firma (HMAC con el secreto del JWT)
/// incluye la secuencia de la cita: si la cita se reprograma, los enlaces
/// de correos anteriores dejan de valer.
/// </summary>
public class EnlacesCita
{
    public const string Confirmar = "confirmar";
    public const string Cancelar = "cancelar";

    private readonly byte[] _clave;
    private readonly CorreoConfig _config;

    public EnlacesCita(JwtConfig jwt, CorreoConfig config)
    {
        // Clave derivada: no se usa el secreto del JWT tal cual para otra cosa.
        _clave = SHA256.HashData(Encoding.UTF8.GetBytes($"respuesta-cita:{jwt.Secret}"));
        _config = config;
    }

    public bool Disponible => Uri.TryCreate(_config.UrlApi, UriKind.Absolute, out _);

    public string? Url(Guid citaId, int secuencia, string accion)
    {
        if (!Uri.TryCreate(_config.UrlApi, UriKind.Absolute, out var baseUrl)) return null;
        var firma = Firmar(citaId, secuencia, accion);
        return $"{baseUrl.ToString().TrimEnd('/')}/api/citas/respuesta"
               + $"?c={citaId}&s={secuencia}&a={accion}&t={firma}";
    }

    public bool EsValido(Guid citaId, int secuencia, string accion, string? firma)
    {
        if (string.IsNullOrEmpty(firma) || accion is not (Confirmar or Cancelar)) return false;
        var esperada = Encoding.ASCII.GetBytes(Firmar(citaId, secuencia, accion));
        var recibida = Encoding.ASCII.GetBytes(firma);
        return CryptographicOperations.FixedTimeEquals(esperada, recibida);
    }

    private string Firmar(Guid citaId, int secuencia, string accion)
    {
        var datos = Encoding.UTF8.GetBytes($"{citaId:N}:{secuencia}:{accion}");
        return Convert.ToBase64String(HMACSHA256.HashData(_clave, datos))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
