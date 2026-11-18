using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Persistence;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Controllers;

/// <summary>Diagnostico del envio de correos (solo administracion).</summary>
[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/correos")]
public class CorreosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CorreoConfig _config;
    private readonly SelectorEnvioCorreo _selector;

    public CorreosController(ApplicationDbContext context, CorreoConfig config, SelectorEnvioCorreo selector)
    {
        _context = context;
        _config = config;
        _selector = selector;
    }

    /// <summary>Configuracion y ultimos envios, para ver si algo falla.</summary>
    [HttpGet("estado")]
    public async Task<ActionResult<object>> Estado()
    {
        var hace24 = DateTime.UtcNow.AddHours(-24);
        var ultimos = await _context.CorreosSalientes
            .AsNoTracking()
            .OrderByDescending(c => c.FechaCreacion)
            .Take(20)
            .Select(c => new
            {
                c.Id, c.Tipo, c.Para, c.Asunto, Estado = c.Estado.ToString(),
                c.Intentos, c.UltimoError, c.Proveedor, c.FechaCreacion, c.FechaEnvio
            })
            .ToListAsync();

        return Ok(new
        {
            Configurado = _config.EstaConfigurado,
            Proveedor = _config.ProveedorEfectivo,
            Remitente = _config.Remitente,
            Pendientes = await _context.CorreosSalientes.CountAsync(c => c.Estado == EstadoCorreo.Pendiente),
            EnviadosUltimas24h = await _context.CorreosSalientes.CountAsync(c => c.Estado == EstadoCorreo.Enviado && c.FechaEnvio >= hace24),
            Fallidos = await _context.CorreosSalientes.CountAsync(c => c.Estado == EstadoCorreo.Fallido),
            Ultimos = ultimos
        });
    }

    public record PruebaCorreoDto(string? Para);

    /// <summary>
    /// Envia en el momento (sin cola) un correo de prueba con una invitacion
    /// de calendario para mañana. Sin destinatario, va al correo de la sesion.
    /// </summary>
    [HttpPost("prueba")]
    public async Task<ActionResult<object>> Prueba([FromBody] PruebaCorreoDto? dto)
    {
        var para = string.IsNullOrWhiteSpace(dto?.Para)
            ? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email")
            : dto!.Para!.Trim();
        if (string.IsNullOrWhiteSpace(para))
            return BadRequest(new { mensaje = "Indica a qué correo enviar la prueba." });

        var envio = _selector.Actual;
        if (envio is null || !_config.EstaConfigurado)
        {
            return Ok(new
            {
                Enviado = false,
                Proveedor = _config.ProveedorEfectivo,
                Error = "El correo no está configurado. Define Correo__BrevoApiKey y Correo__RemitenteCorreo (o Correo__Smtp__Usuario y Correo__Smtp__Contrasena)."
            });
        }

        var inicio = DateTime.UtcNow.Date.AddDays(1).AddHours(17); // mañana 11:00 en Mexico
        var clinica = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();
        var contenido = new PlantillaCorreo.Contenido
        {
            Titulo = "Correo de prueba",
            Saludo = "¡Hola!",
            Introduccion = "Si lees esto, NutriClínica ya puede enviar avisos. Revisa también si la cita de prueba aparece en tu calendario.",
            Datos =
            [
                new("Proveedor", envio.Nombre),
                new("Remitente", _config.Remitente),
                new("Invitación", "Cita de prueba mañana a las 11:00"),
            ],
            ClinicaNombre = string.IsNullOrWhiteSpace(clinica?.RazonSocial) ? "NutriClínica" : clinica.RazonSocial,
            LogoUrl = clinica?.LogoUrl,
        };
        var ics = CalendarioIcs.Generar(new CalendarioIcs.Evento(
            Uid: $"prueba-{Guid.NewGuid()}@nutriclinica",
            Secuencia: 0,
            InicioUtc: inicio,
            FinUtc: inicio.AddHours(1),
            Titulo: "Cita de prueba NutriClínica",
            Descripcion: "Invitación de prueba: puedes borrarla.",
            Lugar: null,
            OrganizadorCorreo: _config.Remitente,
            OrganizadorNombre: "NutriClínica",
            AsistenteCorreo: para,
            AsistenteNombre: para), "REQUEST");

        try
        {
            await envio.EnviarAsync(new MensajeCorreo
            {
                Para = para,
                Asunto = "Prueba de correo · NutriClínica",
                Html = PlantillaCorreo.Html(contenido),
                Texto = PlantillaCorreo.TextoPlano(contenido),
                Ics = ics,
                IcsMetodo = "REQUEST",
            }, HttpContext.RequestAborted);
            return Ok(new { Enviado = true, Proveedor = envio.Nombre, Para = para });
        }
        catch (Exception ex)
        {
            return Ok(new { Enviado = false, Proveedor = envio.Nombre, Error = ex.Message });
        }
    }
}
