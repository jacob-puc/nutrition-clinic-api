using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Notificaciones;

/// <summary>
/// Cada 10 minutos busca citas vigentes que empiecen en las proximas 24 h y
/// le recuerda al paciente. No recuerda una cita agendada con menos de un
/// dia de anticipacion (el aviso de "agendada" acaba de llegar) ni una que
/// empieza en menos de 2 h (ya no da tiempo de reorganizarse).
/// </summary>
public class RecordatorioCitas : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(10);
    private static readonly EstadoCita[] Vigentes = [EstadoCita.Programada, EstadoCita.Confirmada];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CorreoConfig _config;
    private readonly ILogger<RecordatorioCitas> _logger;

    public RecordatorioCitas(IServiceScopeFactory scopeFactory, CorreoConfig config, ILogger<RecordatorioCitas> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Deja arrancar la app (migraciones) antes de la primera vuelta.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_config.EstaConfigurado)
            {
                try
                {
                    await RecordarAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error al preparar los recordatorios de citas.");
                }
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RecordarAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificador = scope.ServiceProvider.GetRequiredService<INotificadorCitas>();

        var ahora = DateTime.UtcNow;
        var desde = ahora.AddHours(2);
        var hasta = ahora.AddHours(24);

        var citas = await db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Nutricionista)
            .Where(c => Vigentes.Contains(c.Estado)
                        && c.FechaInicio > desde
                        && c.FechaInicio <= hasta
                        && c.FechaCreacion < c.FechaInicio.AddHours(-24)
                        && (c.RecordatorioEnviadoPara == null || c.RecordatorioEnviadoPara != c.FechaInicio)
                        && c.Paciente.CorreoElectronico != "")
            .Take(50)
            .ToListAsync(ct);

        foreach (var cita in citas)
        {
            await notificador.RecordatorioAsync(cita, cita.Paciente, cita.Nutricionista);
            cita.RecordatorioEnviadoPara = cita.FechaInicio;
        }

        if (citas.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Recordatorios de cita encolados: {Total}.", citas.Count);
        }
    }
}
