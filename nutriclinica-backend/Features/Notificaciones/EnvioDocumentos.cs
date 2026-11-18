using System.Globalization;
using System.Net.Mail;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Reportes.Services;
using nutriclinica_backend.Features.Transacciones.Interfaces;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Notificaciones;

public class EnviarDocumentoDto
{
    /// <summary>Nota opcional de quien envia; va dentro del correo.</summary>
    public string? Mensaje { get; set; }

    /// <summary>
    /// Destinatario distinto al del paciente. Solo para comprobantes y
    /// facturas (p. ej. el correo de la empresa que factura); el plan y el
    /// historial van siempre al correo del paciente.
    /// </summary>
    public string? Para { get; set; }
}

public record EnvioDocumentoDto(
    Guid Id,
    string Tipo,
    string Para,
    string Estado,
    DateTime FechaCreacion,
    DateTime? FechaEnvio,
    string? Error,
    string? EnviadoPor);

/// <summary>
/// Envio manual de documentos al paciente: plan, historial clinico,
/// comprobante de pago y factura. Siempre lo pide una persona (con
/// confirmacion en la web); el PDF se genera en ese momento y se guarda con
/// el correo, asi que editar el plan despues no cambia lo enviado.
/// </summary>
public interface IEnvioDocumentos
{
    Task<EnvioDocumentoDto> PlanAsync(Guid menuId, EnviarDocumentoDto dto, Guid usuarioId);
    Task<EnvioDocumentoDto> HistorialAsync(Guid pacienteId, EnviarDocumentoDto dto, Guid usuarioId);
    Task<EnvioDocumentoDto> ComprobanteAsync(Guid pagoId, EnviarDocumentoDto dto, Guid usuarioId);
    Task<EnvioDocumentoDto> FacturaAsync(Guid facturaId, EnviarDocumentoDto dto, Guid usuarioId);

    /// <summary>Envios de un documento (plan, paciente, pago o factura), del mas reciente al mas viejo.</summary>
    Task<List<EnvioDocumentoDto>> EnviosAsync(Guid referenciaId);
}

public class EnvioDocumentos : IEnvioDocumentos
{
    public const string TipoPlan = "DocumentoPlan";
    public const string TipoHistorial = "DocumentoHistorial";
    public const string TipoComprobante = "DocumentoComprobante";
    public const string TipoFactura = "DocumentoFactura";

    private const int MaxMensaje = 1000;

    private readonly ApplicationDbContext _context;
    private readonly IColaCorreos _cola;
    private readonly CorreoConfig _config;
    private readonly IReporteService _reportes;
    private readonly IPagoService _pagos;
    private readonly IFacturaService _facturas;

    public EnvioDocumentos(
        ApplicationDbContext context,
        IColaCorreos cola,
        CorreoConfig config,
        IReporteService reportes,
        IPagoService pagos,
        IFacturaService facturas)
    {
        _context = context;
        _cola = cola;
        _config = config;
        _reportes = reportes;
        _pagos = pagos;
        _facturas = facturas;
    }

    public async Task<EnvioDocumentoDto> PlanAsync(Guid menuId, EnviarDocumentoDto dto, Guid usuarioId)
    {
        var plan = await _context.MenusAsignados
            .AsNoTracking()
            .Include(m => m.Paciente)
            .FirstOrDefaultAsync(m => m.Id == menuId && m.IsActive)
            ?? throw new KeyNotFoundException("No encontramos ese plan. Puede que se haya eliminado.");

        var para = CorreoDelPaciente(plan.Paciente);
        var (pdf, nombre) = await _reportes.MenuPacienteAsync(menuId);

        var datos = new List<PlantillaCorreo.Dato>
        {
            new("Plan", plan.Nombre),
            new("Inicia", Primera(plan.FechaInicio.ToString("dddd d 'de' MMMM", ZonaClinica.Cultura))),
        };
        if (plan.ObjetivoCalorico is { } kcal)
            datos.Add(new("Objetivo diario", $"{kcal.ToString("N0", ZonaClinica.Cultura)} kcal"));

        return await EncolarAsync(new Envio(
            Tipo: TipoPlan,
            ReferenciaId: menuId,
            Para: para,
            ParaNombre: plan.Paciente.NombreCompleto,
            Asunto: $"Tu plan nutricional: {plan.Nombre}",
            Titulo: $"Tu plan está *listo*, {PrimerNombre(plan.Paciente.NombreCompleto)}",
            Saludo: null,
            Introduccion: "Te compartimos tu plan de alimentación. Lo encuentras adjunto en PDF, con el menú de cada día.",
            Datos: datos,
            Nota: "Guárdalo en tu teléfono para tenerlo a la mano. También lo ves en la app de NutriClínica.",
            Adjunto: new AdjuntoCorreo(nombre, "application/pdf", pdf)), dto, usuarioId);
    }

    public async Task<EnvioDocumentoDto> HistorialAsync(Guid pacienteId, EnviarDocumentoDto dto, Guid usuarioId)
    {
        var paciente = await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pacienteId && p.IsActive)
            ?? throw new KeyNotFoundException("No encontramos a ese paciente. Puede que se haya eliminado.");

        var para = CorreoDelPaciente(paciente);
        var (pdf, nombre) = await _reportes.HistorialClinicoAsync(pacienteId);

        return await EncolarAsync(new Envio(
            Tipo: TipoHistorial,
            ReferenciaId: pacienteId,
            Para: para,
            ParaNombre: paciente.NombreCompleto,
            Asunto: "Tu historial clínico",
            Titulo: "Tu historial *clínico*",
            Saludo: null,
            Introduccion: $"{PrimerNombre(paciente.NombreCompleto)}, te compartimos tu historial clínico en PDF, con tus datos de salud y tus consultas.",
            Datos: [new("Actualizado al", Primera(ZonaClinica.Formatear(DateTime.UtcNow, "dddd d 'de' MMMM 'de' yyyy")))],
            Nota: "Es información personal: guárdalo en un lugar seguro y no lo reenvíes a desconocidos.",
            Adjunto: new AdjuntoCorreo(nombre, "application/pdf", pdf)), dto, usuarioId);
    }

    public async Task<EnvioDocumentoDto> ComprobanteAsync(Guid pagoId, EnviarDocumentoDto dto, Guid usuarioId)
    {
        var pago = await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Paciente)
            .FirstOrDefaultAsync(p => p.Id == pagoId && p.IsActive)
            ?? throw new KeyNotFoundException("No encontramos ese pago. Puede que se haya eliminado.");

        var para = DestinatarioEditable(dto.Para, pago.Paciente);
        var pdf = await _pagos.GenerarComprobantePdfAsync(pagoId);

        return await EncolarAsync(new Envio(
            Tipo: TipoComprobante,
            ReferenciaId: pagoId,
            Para: para,
            ParaNombre: pago.Paciente.NombreCompleto,
            Asunto: $"Comprobante de pago {pago.NumeroRecibo}",
            Titulo: $"Gracias por tu *pago*, {PrimerNombre(pago.Paciente.NombreCompleto)}",
            Saludo: null,
            Introduccion: "Adjuntamos tu comprobante en PDF. Guárdalo por si lo necesitas más adelante.",
            Datos:
            [
                new("Recibo", pago.NumeroRecibo),
                new("Fecha", Primera(ZonaClinica.Formatear(pago.FechaPago, "d 'de' MMMM 'de' yyyy"))),
                new("Monto", Dinero(pago.Monto, pago.Moneda)),
                new("Método", MetodoTexto(pago.MetodoPago)),
            ],
            Nota: null,
            Adjunto: new AdjuntoCorreo($"comprobante-{pago.NumeroRecibo}.pdf", "application/pdf", pdf)), dto, usuarioId);
    }

    public async Task<EnvioDocumentoDto> FacturaAsync(Guid facturaId, EnviarDocumentoDto dto, Guid usuarioId)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .Include(f => f.Pago).ThenInclude(p => p.Paciente)
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.IsActive)
            ?? throw new KeyNotFoundException("No encontramos esa factura. Puede que se haya eliminado.");

        var para = DestinatarioEditable(dto.Para, factura.Pago.Paciente);
        var pdf = await _facturas.GenerarFacturaPdfAsync(facturaId);
        var folio = $"{factura.Serie}-{factura.Folio:D4}";

        return await EncolarAsync(new Envio(
            Tipo: TipoFactura,
            ReferenciaId: facturaId,
            Para: para,
            ParaNombre: factura.NombreRazonSocial,
            Asunto: $"Factura {folio}",
            Titulo: "Tu factura está *lista*",
            Saludo: null,
            Introduccion: $"{PrimerNombre(factura.Pago.Paciente.NombreCompleto)}, adjuntamos la factura de tu consulta en PDF.",
            Datos:
            [
                new("Folio", folio),
                new("A nombre de", factura.NombreRazonSocial),
                new("RFC", factura.RFC),
                new("Total", Dinero(factura.Total, factura.Pago.Moneda)),
            ],
            Nota: null,
            Adjunto: new AdjuntoCorreo($"factura-{folio}.pdf", "application/pdf", pdf)), dto, usuarioId);
    }

    public async Task<List<EnvioDocumentoDto>> EnviosAsync(Guid referenciaId)
    {
        var envios = await _context.CorreosSalientes
            .AsNoTracking()
            .Where(c => c.ReferenciaId == referenciaId && c.Tipo.StartsWith("Documento"))
            .OrderByDescending(c => c.FechaCreacion)
            .Take(10)
            .ToListAsync();

        var ids = envios.Where(e => e.EnviadoPorId.HasValue).Select(e => e.EnviadoPorId!.Value).Distinct().ToList();
        var nombres = await _context.Nutricionistas
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.NombreCompleto);

        return envios.Select(e => Mapear(e, e.EnviadoPorId is { } id ? nombres.GetValueOrDefault(id) : null)).ToList();
    }

    // ── Armado ───────────────────────────────────────────────────────

    private record Envio(
        string Tipo,
        Guid ReferenciaId,
        string Para,
        string ParaNombre,
        string Asunto,
        string Titulo,
        string? Saludo,
        string Introduccion,
        IReadOnlyList<PlantillaCorreo.Dato> Datos,
        string? Nota,
        AdjuntoCorreo Adjunto);

    private async Task<EnvioDocumentoDto> EncolarAsync(Envio e, EnviarDocumentoDto dto, Guid usuarioId)
    {
        if (!_config.EstaConfigurado)
            throw new ConflictException("El envío de correos no está configurado en el servidor. Avisa al administrador.");

        var mensaje = dto.Mensaje?.Trim();
        if (mensaje is { Length: > MaxMensaje })
            throw Falla("Mensaje", $"El mensaje no puede tener más de {MaxMensaje} caracteres.");

        var remitente = await _context.Nutricionistas.AsNoTracking().FirstOrDefaultAsync(n => n.Id == usuarioId);
        var clinica = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();
        var clinicaNombre = string.IsNullOrWhiteSpace(clinica?.RazonSocial) ? "NutriClínica" : clinica.RazonSocial;

        var contenido = new PlantillaCorreo.Contenido
        {
            Titulo = e.Titulo,
            Saludo = e.Saludo,
            Introduccion = e.Introduccion,
            Datos = e.Datos,
            Nota = e.Nota,
            MensajePersonal = string.IsNullOrWhiteSpace(mensaje) ? null : mensaje,
            MensajeAutor = remitente?.NombreCompleto,
            ClinicaNombre = clinicaNombre,
            LogoUrl = string.IsNullOrWhiteSpace(clinica?.LogoUrl) ? LogoSitio() : clinica.LogoUrl,
            Cierre = string.IsNullOrWhiteSpace(clinica?.Telefono)
                ? "¿Dudas? Responde a este correo."
                : $"¿Dudas? Llámanos al *{clinica.Telefono}* o responde a este correo.",
            Pie = string.IsNullOrWhiteSpace(clinica?.Direccion)
                ? $"Enviado por {clinicaNombre}."
                : $"{clinicaNombre} · {clinica.Direccion}\nEste documento es personal; guárdalo en un lugar seguro.",
        };

        var correo = _cola.Encolar(new MensajeCorreo
        {
            Para = e.Para,
            ParaNombre = e.ParaNombre,
            Asunto = e.Asunto,
            Html = PlantillaCorreo.Html(contenido),
            Texto = PlantillaCorreo.TextoPlano(contenido),
            Adjuntos = [e.Adjunto],
        }, e.Tipo, e.ReferenciaId, usuarioId)!;

        await _context.SaveChangesAsync();
        return Mapear(correo, remitente?.NombreCompleto);
    }

    private static string CorreoDelPaciente(Paciente paciente)
    {
        if (string.IsNullOrWhiteSpace(paciente.CorreoElectronico) || !EsCorreo(paciente.CorreoElectronico))
            throw Falla("Para", "El paciente no tiene un correo válido. Agrégalo en su expediente para poder enviarle documentos.");
        return paciente.CorreoElectronico.Trim();
    }

    private static string DestinatarioEditable(string? para, Paciente paciente)
    {
        if (string.IsNullOrWhiteSpace(para)) return CorreoDelPaciente(paciente);
        if (!EsCorreo(para)) throw Falla("Para", "Escribe un correo válido.");
        return para.Trim();
    }

    private static bool EsCorreo(string texto) =>
        MailAddress.TryCreate(texto.Trim(), out var m) && m.Address == texto.Trim() && texto.Contains('.');

    private string? LogoSitio() =>
        Uri.TryCreate(_config.UrlSitio, UriKind.Absolute, out var u) ? $"{u.ToString().TrimEnd('/')}/logo-correo.png" : null;

    private static EnvioDocumentoDto Mapear(CorreoSaliente c, string? enviadoPor) => new(
        c.Id, c.Tipo, c.Para, c.Estado.ToString(), c.FechaCreacion, c.FechaEnvio,
        c.Estado == EstadoCorreo.Fallido ? c.UltimoError : null, enviadoPor);

    private static ValidationException Falla(string campo, string mensaje) => new([new ValidationFailure(campo, mensaje)]);

    private static string Dinero(decimal monto, string moneda) =>
        $"{monto.ToString("C2", CultureInfo.GetCultureInfo("es-MX"))} {moneda}";

    private static string MetodoTexto(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.TarjetaDebito => "Tarjeta de débito",
        MetodoPago.TarjetaCredito => "Tarjeta de crédito",
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.QR => "Pago con QR",
        _ => "Otro",
    };

    private static string PrimerNombre(string nombre) =>
        nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;

    private static string Primera(string texto) => texto.Length == 0 ? texto : char.ToUpper(texto[0]) + texto[1..];
}
