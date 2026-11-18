namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>Un correo listo para enviar.</summary>
public record MensajeCorreo
{
    public required string Para { get; init; }
    public string? ParaNombre { get; init; }
    public required string Asunto { get; init; }
    public required string Html { get; init; }
    public required string Texto { get; init; }

    /// <summary>Invitacion de calendario (iCalendar) para iMIP; null si no lleva.</summary>
    public string? Ics { get; init; }

    /// <summary>REQUEST (crear/actualizar) o CANCEL.</summary>
    public string? IcsMetodo { get; init; }

    public IReadOnlyList<AdjuntoCorreo> Adjuntos { get; init; } = [];
}

public record AdjuntoCorreo(string Nombre, string TipoContenido, byte[] Contenido);

/// <summary>Proveedor que entrega el correo (Brevo o SMTP).</summary>
public interface IEnvioCorreo
{
    string Nombre { get; }

    /// <summary>Envia o lanza una excepcion con el motivo.</summary>
    Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default);
}

public class CorreoNoConfiguradoException(string mensaje) : Exception(mensaje);
