using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.PortalPaciente;

/// <param name="TieneApp">Ya tiene una cuenta de la app vinculada a su expediente.</param>
/// <param name="InvitadoEn">Ultima invitacion enviada (null si nunca).</param>
public record EstadoAppPacienteDto(Guid PacienteId, bool TieneApp, DateTime? InvitadoEn);

/// <summary>
/// Quien de los pacientes ya usa la app y la invitacion por correo para
/// quien no. La invitacion solo explica como crear la cuenta con el mismo
/// correo del expediente: al verificarlo, la cuenta se vincula sola.
/// </summary>
public class InvitacionesApp
{
    public const string TipoCorreo = "InvitacionApp";

    private readonly ApplicationDbContext _context;
    private readonly IColaCorreos _cola;
    private readonly CorreoConfig _config;

    public InvitacionesApp(ApplicationDbContext context, IColaCorreos cola, CorreoConfig config)
    {
        _context = context;
        _cola = cola;
        _config = config;
    }

    /// <summary>
    /// Solo los pacientes con algo que contar (cuenta o invitacion); los que
    /// no aparecen no tienen app ni se les ha invitado.
    /// </summary>
    public async Task<List<EstadoAppPacienteDto>> EstadosAsync()
    {
        var conApp = await _context.CuentasPaciente.AsNoTracking()
            .Where(c => c.IsActive && c.PacienteId != null)
            .Select(c => c.PacienteId!.Value)
            .Distinct()
            .ToListAsync();

        var invitaciones = await _context.CorreosSalientes.AsNoTracking()
            .Where(c => c.Tipo == TipoCorreo && c.ReferenciaId != null)
            .GroupBy(c => c.ReferenciaId!.Value)
            .Select(g => new { PacienteId = g.Key, Ultima = g.Max(c => c.FechaCreacion) })
            .ToDictionaryAsync(x => x.PacienteId, x => x.Ultima);

        var ids = conApp.ToHashSet();
        ids.UnionWith(invitaciones.Keys);
        return ids
            .Select(id => new EstadoAppPacienteDto(id, conApp.Contains(id), invitaciones.TryGetValue(id, out var f) ? f : null))
            .ToList();
    }

    public async Task<EstadoAppPacienteDto> InvitarAsync(Guid pacienteId, Guid enviadoPorId)
    {
        if (!_config.EstaConfigurado)
            throw new ConflictException("El envío de correos no está configurado en el servidor. Avisa al administrador.");

        var paciente = await _context.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pacienteId && p.IsActive)
            ?? throw new KeyNotFoundException("No encontramos a ese paciente. Puede que se haya dado de baja.");

        if (string.IsNullOrWhiteSpace(paciente.CorreoElectronico))
            throw new ConflictException("El paciente no tiene correo. Agrégalo en «Editar datos» para invitarlo.");

        var tieneApp = await _context.CuentasPaciente.AnyAsync(c => c.IsActive && c.PacienteId == pacienteId);
        if (tieneApp) throw new ConflictException("Este paciente ya tiene cuenta en la app.");

        var clinica = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();
        var clinicaNombre = string.IsNullOrWhiteSpace(clinica?.RazonSocial) ? "NutriClínica" : clinica.RazonSocial;
        var nombre = paciente.NombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? paciente.NombreCompleto;

        var botones = new List<PlantillaCorreo.Boton>();
        if (EsUrl(_config.UrlApp)) botones.Add(new("Descargar la app", _config.UrlApp!, Primario: true));

        var contenido = new PlantillaCorreo.Contenido
        {
            Titulo = $"Tu plan, *contigo*, {nombre}",
            Introduccion = $"En {clinicaNombre} ya tenemos app: ahí ves tu menú de cada día, tu progreso y tus citas, y puedes confirmarlas o agendar una nueva.",
            Datos =
            [
                new("Paso 1", "Descarga la app NutriClínica"),
                new("Paso 2", "Toca «Crear mi cuenta»"),
                new("Paso 3", "Usa este correo", paciente.CorreoElectronico),
            ],
            Botones = botones,
            Nota = "Con este mismo correo tu cuenta se vincula sola a tu expediente: verás tu plan desde el primer día.",
            Cierre = string.IsNullOrWhiteSpace(clinica?.Telefono)
                ? "¿Dudas? Responde a este correo."
                : $"¿Dudas? Llámanos al *{clinica.Telefono}* o responde a este correo.",
            Pie = $"{clinicaNombre}\nRecibes este correo porque eres paciente de {clinicaNombre}.",
            ClinicaNombre = clinicaNombre,
            LogoUrl = string.IsNullOrWhiteSpace(clinica?.LogoUrl) ? LogoSitio() : clinica.LogoUrl,
        };

        var correo = _cola.Encolar(new MensajeCorreo
        {
            Para = paciente.CorreoElectronico,
            ParaNombre = paciente.NombreCompleto,
            Asunto = $"{nombre}, tu plan ahora en tu celular",
            Html = PlantillaCorreo.Html(contenido),
            Texto = PlantillaCorreo.TextoPlano(contenido),
        }, TipoCorreo, pacienteId, enviadoPorId);
        await _context.SaveChangesAsync();

        return new EstadoAppPacienteDto(pacienteId, false, correo?.FechaCreacion ?? DateTime.UtcNow);
    }

    private string? LogoSitio() =>
        Uri.TryCreate(_config.UrlSitio, UriKind.Absolute, out var u) ? $"{u.ToString().TrimEnd('/')}/logo-correo.png" : null;

    private static bool EsUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp);
}
