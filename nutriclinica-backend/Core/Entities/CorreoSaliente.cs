namespace nutriclinica_backend.Core.Entities;

/// <summary>
/// Cola de correos (patron outbox). Se guarda en la misma transaccion que el
/// cambio que lo origina (p. ej. agendar una cita) y un proceso en segundo
/// plano lo envia y reintenta: si el proveedor de correo falla o tarda, la
/// operacion del usuario no se ve afectada, y queda registro de lo enviado.
/// </summary>
public class CorreoSaliente
{
    public Guid Id { get; set; }

    public string Para { get; set; } = string.Empty;
    public string? ParaNombre { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string CuerpoHtml { get; set; } = string.Empty;
    public string CuerpoTexto { get; set; } = string.Empty;

    public string? Ics { get; set; }
    public string? IcsMetodo { get; set; }

    /// <summary>Que lo origino: "CitaAgendada", "CitaCancelada", "Prueba"...</summary>
    public string Tipo { get; set; } = string.Empty;
    public Guid? ReferenciaId { get; set; }

    public EstadoCorreo Estado { get; set; } = EstadoCorreo.Pendiente;
    public int Intentos { get; set; }
    public DateTime ProximoIntento { get; set; } = DateTime.UtcNow;
    public string? UltimoError { get; set; }
    public string? Proveedor { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEnvio { get; set; }

    /// <summary>Quien lo envio a mano (documentos); null en avisos automaticos.</summary>
    public Guid? EnviadoPorId { get; set; }

    public virtual List<CorreoAdjunto> Adjuntos { get; set; } = [];
}

/// <summary>
/// Archivo adjunto tal como estaba al pedir el envio (p. ej. el PDF del plan
/// en ese momento): si despues se edita el plan, lo enviado no cambia.
/// </summary>
public class CorreoAdjunto
{
    public Guid Id { get; set; }
    public Guid CorreoSalienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = "application/pdf";
    public byte[] Contenido { get; set; } = [];
}

public enum EstadoCorreo
{
    Pendiente,
    Enviado,
    Fallido,
    Descartado
}
