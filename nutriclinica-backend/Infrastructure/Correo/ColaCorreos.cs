using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Infrastructure.Correo;

public interface IColaCorreos
{
    /// <summary>
    /// Agrega el correo al contexto; se guarda con el proximo SaveChanges de
    /// quien lo llama (misma transaccion que el cambio que lo origina).
    /// </summary>
    CorreoSaliente? Encolar(MensajeCorreo mensaje, string tipo, Guid? referenciaId = null, Guid? enviadoPorId = null);
}

public class ColaCorreos : IColaCorreos
{
    private readonly ApplicationDbContext _context;

    public ColaCorreos(ApplicationDbContext context)
    {
        _context = context;
    }

    public CorreoSaliente? Encolar(MensajeCorreo mensaje, string tipo, Guid? referenciaId = null, Guid? enviadoPorId = null)
    {
        if (string.IsNullOrWhiteSpace(mensaje.Para)) return null;

        var correo = new CorreoSaliente
        {
            EnviadoPorId = enviadoPorId,
            Adjuntos = mensaje.Adjuntos
                .Select(a => new CorreoAdjunto { Nombre = a.Nombre, TipoContenido = a.TipoContenido, Contenido = a.Contenido })
                .ToList(),
            Para = mensaje.Para.Trim(),
            ParaNombre = mensaje.ParaNombre,
            Asunto = mensaje.Asunto,
            CuerpoHtml = mensaje.Html,
            CuerpoTexto = mensaje.Texto,
            Ics = mensaje.Ics,
            IcsMetodo = mensaje.IcsMetodo,
            Tipo = tipo,
            ReferenciaId = referenciaId,
        };
        _context.CorreosSalientes.Add(correo);
        return correo;
    }
}

/// <summary>Elige el proveedor segun la configuracion.</summary>
public class SelectorEnvioCorreo
{
    private readonly CorreoConfig _config;
    private readonly IServiceProvider _servicios;

    public SelectorEnvioCorreo(CorreoConfig config, IServiceProvider servicios)
    {
        _config = config;
        _servicios = servicios;
    }

    public IEnvioCorreo? Actual => _config.ProveedorEfectivo switch
    {
        "Brevo" => _servicios.GetRequiredService<BrevoEnvioCorreo>(),
        "Smtp" => _servicios.GetRequiredService<SmtpEnvioCorreo>(),
        _ => null
    };
}

/// <summary>
/// Envia los correos pendientes cada pocos segundos. Reintenta con espera
/// creciente (1, 5, 15, 60 min) y se rinde al quinto intento.
/// </summary>
public class ProcesadorCorreos : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);
    private static readonly int[] EsperaMinutos = [1, 5, 15, 60];
    private const int MaxIntentos = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CorreoConfig _config;
    private readonly ILogger<ProcesadorCorreos> _logger;
    private bool _avisoSinConfigurar;

    public ProcesadorCorreos(IServiceScopeFactory scopeFactory, CorreoConfig config, ILogger<ProcesadorCorreos> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarLoteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al procesar la cola de correos.");
            }

            try
            {
                await Task.Delay(Intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ProcesarLoteAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ahora = DateTime.UtcNow;

        // Lo que se quedo demasiado tiempo sin enviar ya no sirve (una cita pasada).
        var limite = ahora.AddHours(-_config.HorasMaximasEnCola);
        await db.CorreosSalientes
            .Where(c => c.Estado == EstadoCorreo.Pendiente && c.FechaCreacion < limite)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Estado, EstadoCorreo.Descartado)
                .SetProperty(c => c.UltimoError, "Demasiado tiempo en cola sin poder enviarse."), ct);

        var envio = scope.ServiceProvider.GetRequiredService<SelectorEnvioCorreo>().Actual;
        if (envio is null || !_config.EstaConfigurado)
        {
            if (!_avisoSinConfigurar)
            {
                _logger.LogWarning(
                    "Correo sin configurar: los avisos se quedan en cola. Define Correo__BrevoApiKey y Correo__RemitenteCorreo.");
                _avisoSinConfigurar = true;
            }
            return;
        }
        _avisoSinConfigurar = false;

        var pendientes = await db.CorreosSalientes
            .Include(c => c.Adjuntos)
            .Where(c => c.Estado == EstadoCorreo.Pendiente && c.ProximoIntento <= ahora)
            .OrderBy(c => c.FechaCreacion)
            .Take(10)
            .ToListAsync(ct);

        foreach (var correo in pendientes)
        {
            correo.Intentos++;
            correo.Proveedor = envio.Nombre;
            try
            {
                await envio.EnviarAsync(new MensajeCorreo
                {
                    Para = correo.Para,
                    ParaNombre = correo.ParaNombre,
                    Asunto = correo.Asunto,
                    Html = correo.CuerpoHtml,
                    Texto = correo.CuerpoTexto,
                    Ics = correo.Ics,
                    IcsMetodo = correo.IcsMetodo,
                    Adjuntos = correo.Adjuntos
                        .Select(a => new AdjuntoCorreo(a.Nombre, a.TipoContenido, a.Contenido))
                        .ToList(),
                }, ct);

                correo.Estado = EstadoCorreo.Enviado;
                correo.FechaEnvio = DateTime.UtcNow;
                correo.UltimoError = null;
                _logger.LogInformation("Correo {Tipo} enviado ({Id}).", correo.Tipo, correo.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                correo.UltimoError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                if (correo.Intentos >= MaxIntentos)
                {
                    correo.Estado = EstadoCorreo.Fallido;
                }
                else
                {
                    var espera = EsperaMinutos[Math.Min(correo.Intentos - 1, EsperaMinutos.Length - 1)];
                    correo.ProximoIntento = DateTime.UtcNow.AddMinutes(espera);
                }
                _logger.LogWarning("No se pudo enviar el correo {Id} (intento {Intento}): {Error}",
                    correo.Id, correo.Intentos, correo.UltimoError);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
