using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Citas.DTOs;
using nutriclinica_backend.Features.Citas.Interfaces;
using nutriclinica_backend.Features.Notificaciones;
using nutriclinica_backend.Infrastructure.Persistence;
using static nutriclinica_backend.Infrastructure.Correo.IdentidadMarca;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Respuesta del paciente desde el correo: confirmar o avisar que no
/// asistira. GET solo muestra la pregunta; el cambio ocurre con el POST del
/// boton, porque algunos servicios de correo abren los enlaces solos para
/// revisarlos y no deben poder confirmar ni cancelar nada.
/// La pagina usa la identidad del sitio publico (ver IdentidadMarca).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/citas/respuesta")]
[EnableRateLimiting(LimitesReserva.Consulta)]
public class RespuestaCitaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICitaService _citas;
    private readonly EnlacesCita _enlaces;

    public RespuestaCitaController(ApplicationDbContext context, ICitaService citas, EnlacesCita enlaces)
    {
        _context = context;
        _citas = citas;
        _enlaces = enlaces;
    }

    private enum Icono { Pregunta, Listo, Cancelada, Aviso }

    [HttpGet]
    public async Task<ContentResult> Preguntar([FromQuery] Guid c, [FromQuery] int s, [FromQuery] string a, [FromQuery] string t)
    {
        var (cita, error) = await ValidarAsync(c, s, a, t);
        if (error is not null) return error;

        var confirmar = a == EnlacesCita.Confirmar;
        if (confirmar && cita!.Estado == EstadoCita.Confirmada)
            return await PaginaAsync("Tu cita ya está *confirmada*", cita, "¡Te esperamos!", Icono.Listo);

        var formulario = $"""
            <form method="post" action="respuesta" class="acciones">
              <input type="hidden" name="c" value="{c}">
              <input type="hidden" name="s" value="{s}">
              <input type="hidden" name="a" value="{H(a)}">
              <input type="hidden" name="t" value="{H(t)}">
              <button type="submit" class="boton {(confirmar ? "" : "boton-peligro")}">
                {(confirmar ? "Sí, confirmo mi asistencia" : "Sí, no podré asistir")}
              </button>
            </form>
            """;
        return await PaginaAsync(
            confirmar ? "¿*Confirmas* tu cita?" : "¿No podrás *asistir*?",
            cita!,
            confirmar
                ? "Al confirmar, la clínica sabrá que te espera ese día."
                : "Cancelaremos la cita y liberaremos el horario. Puedes agendar otra cuando quieras.",
            confirmar ? Icono.Pregunta : Icono.Aviso,
            formulario,
            peligro: !confirmar);
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<ContentResult> Responder([FromForm] Guid c, [FromForm] int s, [FromForm] string a, [FromForm] string t)
    {
        var (cita, error) = await ValidarAsync(c, s, a, t);
        if (error is not null) return error;

        if (a == EnlacesCita.Confirmar)
        {
            if (cita!.Estado == EstadoCita.Programada)
                await _citas.CambiarEstadoAsync(c, new CambiarEstadoCitaDto { Estado = EstadoCita.Confirmada });
            return await PaginaAsync("¡Cita *confirmada*!", cita, "Gracias por confirmar. Te esperamos.", Icono.Listo);
        }

        // CambiarEstado avisa por correo al paciente y al nutriologo y quita la cita del calendario.
        await _citas.CambiarEstadoAsync(c, new CambiarEstadoCitaDto
        {
            Estado = EstadoCita.Cancelada,
            Motivo = "El paciente avisó desde el correo que no podrá asistir.",
        });
        return await PaginaAsync("Cita *cancelada*", cita!,
            "Gracias por avisarnos. Te enviamos la cancelación por correo; cuando quieras, agenda una nueva cita.",
            Icono.Cancelada, peligro: true);
    }

    private async Task<(Cita? cita, ContentResult? error)> ValidarAsync(Guid id, int secuencia, string accion, string firma)
    {
        if (!_enlaces.EsValido(id, secuencia, accion, firma))
            return (null, await PaginaAsync("Enlace *no válido*", null,
                "Este enlace no es válido. Comunícate con la clínica si necesitas ayuda.", Icono.Aviso, estado: 400));

        var cita = await _context.Citas
            .AsNoTracking()
            .Include(x => x.Nutricionista)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cita is null)
            return (null, await PaginaAsync("Cita *no encontrada*", null,
                "Esta cita ya no existe. Comunícate con la clínica si necesitas ayuda.", Icono.Aviso, estado: 404));
        if (cita.SecuenciaCalendario != secuencia)
            return (null, await PaginaAsync("Tu cita *cambió*", cita,
                "Este enlace es de un aviso anterior. Usa el del correo más reciente.", Icono.Aviso));
        if (cita.FechaInicio <= DateTime.UtcNow)
            return (null, await PaginaAsync("La cita *ya pasó*", cita, "Este enlace ya no se puede usar.", Icono.Aviso));
        if (cita.Estado is not (EstadoCita.Programada or EstadoCita.Confirmada))
            return (null, await PaginaAsync("Ya no se puede *cambiar*", cita,
                cita.Estado == EstadoCita.Cancelada ? "Esta cita ya estaba cancelada." : "Esta cita ya no admite cambios desde el correo.",
                Icono.Aviso));

        return (cita, null);
    }

    // ── Pagina ───────────────────────────────────────────────────────

    private async Task<ContentResult> PaginaAsync(
        string titulo, Cita? cita, string mensaje, Icono icono,
        string? extra = null, bool peligro = false, int estado = 200)
    {
        var datos = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();
        var clinica = string.IsNullOrWhiteSpace(datos?.RazonSocial) ? "NutriClínica" : datos.RazonSocial;
        var acento = peligro ? Peligro : Marca;

        var detalle = cita is null ? "" : $"""
            <div class="detalle">
              <p class="fecha">{H(Primera(ZonaClinica.Formatear(cita.FechaInicio, "dddd d 'de' MMMM")))}</p>
              <p class="linea">{Svg(RelojPath)} {ZonaClinica.Formatear(cita.FechaInicio, "HH:mm")} – {ZonaClinica.Formatear(cita.FechaFin, "HH:mm")} h</p>
              <p class="linea">{Svg(PersonaPath)} {H(cita.Nutricionista?.NombreCompleto ?? "")}</p>
              {(string.IsNullOrWhiteSpace(datos?.Direccion) ? "" : $"<p class=\"linea\">{Svg(PinPath)} {H(datos.Direccion)}</p>")}
            </div>
            """;

        var contacto = string.IsNullOrWhiteSpace(datos?.Telefono)
            ? ""
            : $"""<p class="pie">¿Dudas? Llama a la clínica: <a href="tel:{H(datos.Telefono)}">{H(datos.Telefono)}</a></p>""";

        var html = $$"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="robots" content="noindex"><title>{{H(SinMarcas(titulo))}} · {{H(clinica)}}</title>
            {{EnlaceFuentes}}
            <style>
              *{box-sizing:border-box}
              body{margin:0;min-height:100vh;background:{{Fondo}};color:{{Texto}};font-family:{{FuenteTexto}};
                background-image:radial-gradient(40rem 30rem at 0% 0%,rgba(255,252,240,.9),transparent 60%),
                  radial-gradient(36rem 28rem at 100% 10%,rgba(160,215,205,.55),transparent 60%),
                  radial-gradient(34rem 30rem at 50% 110%,rgba(200,235,213,.7),transparent 60%);}
              header{max-width:560px;margin:0 auto;padding:22px 20px 0;display:flex;align-items:center;gap:10px}
              .logo{width:44px;height:44px;border-radius:50%;background:{{Degradado}};display:grid;place-items:center;box-shadow:0 1px 2px rgba(18,52,50,.08)}
              .marca{font-family:{{FuenteTitulo}};font-size:26px;letter-spacing:-.3px}
              main{max-width:560px;margin:0 auto;padding:22px 20px 40px}
              .tarjeta{background:{{(peligro ? "#FBE6E3" : Menta)}};border-radius:40px;padding:34px 26px 28px;text-align:center}
              .icono{width:76px;height:76px;border-radius:50%;margin:0 auto 18px;display:grid;place-items:center;background:{{acento}};color:#fff}
              h1{font-family:{{FuenteTitulo}};font-weight:400;font-size:clamp(30px,8vw,40px);line-height:1.1;letter-spacing:-.5px;margin:0}
              .mensaje{color:rgba(33,31,26,.72);font-size:16.5px;line-height:1.55;max-width:400px;margin:14px auto 0}
              .detalle{background:{{Tarjeta}};border-radius:28px;padding:20px 22px;margin-top:24px;text-align:left}
              .fecha{font-family:{{FuenteTitulo}};font-size:24px;margin:0 0 8px}
              .linea{display:flex;align-items:center;gap:8px;margin:6px 0 0;font-size:14.5px;color:{{Texto}}}
              .linea svg{flex:none;color:{{Apagado}}}
              .acciones{margin-top:22px}
              .boton{width:100%;padding:16px 20px;border:0;border-radius:999px;font:700 16px {{FuenteTexto}};color:#fff;background:{{Marca}};cursor:pointer;transition:filter .15s}
              .boton:hover{filter:brightness(1.08)}
              .boton-peligro{background:{{Peligro}}}
              .pie{text-align:center;color:{{Apagado}};font-size:13.5px;margin-top:20px}
              .pie a{color:{{Marca}};font-weight:700;text-decoration:none}
            </style></head>
            <body>
              <header><span class="logo">{{HojaSvg}}</span><span class="marca">{{H(clinica)}}</span></header>
              <main>
                <section class="tarjeta">
                  <div class="icono">{{SvgIcono(icono)}}</div>
                  <h1>{{Titulo(titulo, acento)}}</h1>
                  <p class="mensaje">{{H(mensaje)}}</p>
                  {{detalle}}
                  {{extra}}
                </section>
                {{contacto}}
              </main>
            </body></html>
            """;
        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = estado };
    }

    // Iconos de lucide, los mismos de la web.
    private const string RelojPath = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 6v6l4 2\"/>";
    private const string PersonaPath = "<path d=\"M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2\"/><circle cx=\"12\" cy=\"7\" r=\"4\"/>";
    private const string PinPath = "<path d=\"M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/>";

    private static string Svg(string contenido, int tam = 16, double trazo = 2) =>
        $"<svg width=\"{tam}\" height=\"{tam}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" "
        + $"stroke-width=\"{trazo}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{contenido}</svg>";

    private static string SvgIcono(Icono icono) => icono switch
    {
        Icono.Listo => Svg("<path d=\"M20 6 9 17l-5-5\"/>", 38, 2.5),
        Icono.Cancelada => Svg("<path d=\"M18 6 6 18\"/><path d=\"m6 6 12 12\"/>", 36, 2.5),
        Icono.Pregunta => Svg("<path d=\"M8 2v4\"/><path d=\"M16 2v4\"/><rect width=\"18\" height=\"18\" x=\"3\" y=\"4\" rx=\"2\"/><path d=\"M3 10h18\"/><path d=\"m9 16 2 2 4-4\"/>", 34, 2.25),
        _ => Svg("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 8v4\"/><path d=\"M12 16h.01\"/>", 36, 2.25),
    };

    private static string H(string texto) => WebUtility.HtmlEncode(texto);

    private static string Primera(string texto) => texto.Length == 0 ? texto : char.ToUpper(texto[0]) + texto[1..];
}
