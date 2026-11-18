using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Notificaciones;

/// <summary>
/// Avisos por correo de las citas, al paciente y al nutriologo, con la
/// invitacion de calendario (iMIP). Solo encola: el correo se guarda con el
/// SaveChanges de quien llama y lo envia <see cref="ProcesadorCorreos"/>.
/// </summary>
public interface INotificadorCitas
{
    Task CitaAgendadaAsync(Cita cita, Paciente paciente, Nutricionista nutricionista);

    /// <param name="nutricionistaAnterior">Si cambio quien atiende, recibe la cancelacion.</param>
    Task CitaReprogramadaAsync(Cita cita, Paciente paciente, Nutricionista nutricionista, Nutricionista? nutricionistaAnterior);

    Task CitaCanceladaAsync(Cita cita, Paciente paciente, Nutricionista nutricionista, string? motivo);

    /// <summary>Recordatorio al paciente el dia anterior (sin invitacion: ya la tiene).</summary>
    Task RecordatorioAsync(Cita cita, Paciente paciente, Nutricionista nutricionista);
}

public class NotificadorCitas : INotificadorCitas
{
    private readonly IColaCorreos _cola;
    private readonly CorreoConfig _config;
    private readonly ApplicationDbContext _context;
    private readonly EnlacesCita _enlaces;

    public NotificadorCitas(IColaCorreos cola, CorreoConfig config, ApplicationDbContext context, EnlacesCita enlaces)
    {
        _cola = cola;
        _config = config;
        _context = context;
        _enlaces = enlaces;
    }

    public async Task CitaAgendadaAsync(Cita cita, Paciente paciente, Nutricionista nutricionista)
    {
        var clinica = await ClinicaAsync();
        AlPaciente(cita, paciente, nutricionista, clinica, Aviso.Agendada);
        AlNutriologo(cita, paciente, nutricionista, clinica, Aviso.Agendada);
    }

    public async Task CitaReprogramadaAsync(Cita cita, Paciente paciente, Nutricionista nutricionista, Nutricionista? nutricionistaAnterior)
    {
        var clinica = await ClinicaAsync();
        AlPaciente(cita, paciente, nutricionista, clinica, Aviso.Reprogramada);

        if (nutricionistaAnterior is not null && nutricionistaAnterior.Id != nutricionista.Id)
        {
            AlNutriologo(cita, paciente, nutricionistaAnterior, clinica, Aviso.Cancelada,
                "La cita se reasignó a otro profesional.");
            AlNutriologo(cita, paciente, nutricionista, clinica, Aviso.Agendada);
        }
        else
        {
            AlNutriologo(cita, paciente, nutricionista, clinica, Aviso.Reprogramada);
        }
    }

    public async Task CitaCanceladaAsync(Cita cita, Paciente paciente, Nutricionista nutricionista, string? motivo)
    {
        var clinica = await ClinicaAsync();
        AlPaciente(cita, paciente, nutricionista, clinica, Aviso.Cancelada, motivo);
        AlNutriologo(cita, paciente, nutricionista, clinica, Aviso.Cancelada, motivo);
    }

    public async Task RecordatorioAsync(Cita cita, Paciente paciente, Nutricionista nutricionista)
    {
        var clinica = await ClinicaAsync();
        AlPaciente(cita, paciente, nutricionista, clinica, Aviso.Recordatorio);
    }

    // ── Mensajes ─────────────────────────────────────────────────────

    private enum Aviso { Agendada, Reprogramada, Cancelada, Recordatorio }

    private record Clinica(string Nombre, string? Direccion, string? Telefono, string? LogoUrl);

    private void AlPaciente(Cita cita, Paciente paciente, Nutricionista nutricionista, Clinica clinica,
        Aviso aviso, string? motivo = null)
    {
        if (string.IsNullOrWhiteSpace(paciente.CorreoElectronico)) return;

        var nombre = PrimerNombre(paciente.NombreCompleto);
        var cuando = CuandoEs(cita);
        var hora = ZonaClinica.Formatear(cita.FechaInicio, "HH:mm");
        var fechaLarga = ZonaClinica.Formatear(cita.FechaInicio, "dddd d 'de' MMMM");

        /* Confirmar o avisar que no asistira cambia la cita en el sistema
           (la invitacion de Gmail solo responde por correo al remitente). */
        var confirmar = aviso != Aviso.Cancelada && cita.Estado == EstadoCita.Programada
            ? _enlaces.Url(cita.Id, cita.SecuenciaCalendario, EnlacesCita.Confirmar)
            : null;
        var cancelar = aviso != Aviso.Cancelada && cita.Estado is EstadoCita.Programada or EstadoCita.Confirmada
            ? _enlaces.Url(cita.Id, cita.SecuenciaCalendario, EnlacesCita.Cancelar)
            : null;

        var (titulo, intro, asunto) = aviso switch
        {
            Aviso.Agendada => ($"Te esperamos, *{nombre}*",
                confirmar is not null
                    ? "Tu cita quedó agendada. Confírmanos que vas para apartar tu lugar; te mandaremos un recordatorio un día antes."
                    : "Tu cita quedó agendada. Te mandaremos un recordatorio un día antes.",
                $"Tu cita del {fechaLarga}, {hora} h"),
            Aviso.Reprogramada => ("Cambiamos tu *cita*",
                confirmar is not null
                    ? $"{nombre}, estos son los nuevos datos de tu cita. Confírmanos que te queda bien para apartar tu lugar."
                    : $"{nombre}, estos son los nuevos datos de tu cita.",
                $"Cambió tu cita: ahora el {fechaLarga}, {hora} h"),
            Aviso.Recordatorio => ($"{Primera(cuando)} te *esperamos*",
                confirmar is not null
                    ? $"{nombre}, te recordamos tu cita de nutrición. Si aún no la confirmas, hazlo con un toque."
                    : $"{nombre}, te recordamos tu cita de nutrición. ¡Nos vemos pronto!",
                $"Recordatorio: tu cita es {cuando} a las {hora} h"),
            _ => ("Cancelamos tu *cita*",
                $"{nombre}, cancelamos la siguiente cita. Si quieres, puedes agendar otra cuando gustes.",
                $"Cancelamos tu cita del {fechaLarga}"),
        };

        var datos = new List<PlantillaCorreo.Dato>
        {
            new("Hora", Horario(cita)),
            new("Con", nutricionista.NombreCompleto, nutricionista.Especialidad),
            new("Dónde", clinica.Nombre, clinica.Direccion),
        };
        if (aviso == Aviso.Cancelada && !string.IsNullOrWhiteSpace(motivo)) datos.Add(new("Motivo", motivo));

        var botones = new List<PlantillaCorreo.Boton>();
        if (aviso == Aviso.Cancelada)
        {
            if (UrlSitio("/reservar") is { } reservar) botones.Add(new("Agendar otra cita", reservar, Primario: true));
        }
        else
        {
            if (confirmar is not null) botones.Add(new("Confirmar asistencia", confirmar, Primario: true));
            if (aviso != Aviso.Recordatorio)
            {
                botones.Add(new("Google Calendar", GoogleCalendar(cita, clinica,
                    $"Cita de nutrición con {nutricionista.NombreCompleto}")));
            }
            if (Mapa(clinica) is { } mapa) botones.Add(new("Cómo llegar", mapa));
            if (cancelar is not null) botones.Add(new("No podré asistir", cancelar, Peligro: true));
        }

        var cierre = clinica.Telefono is null
            ? "¿Dudas o necesitas cambiar tu cita? Responde a este correo."
            : $"¿Dudas o necesitas cambiar tu cita? Llámanos al *{clinica.Telefono}* o responde a este correo.";

        var contenido = new PlantillaCorreo.Contenido
        {
            Titulo = titulo,
            Introduccion = intro,
            FechaDestacada = FechaBoleto(cita),
            Datos = datos,
            Botones = botones,
            Ayuda = confirmar is null ? null : "Es un solo toque, no necesitas iniciar sesión.",
            Nota = aviso == Aviso.Cancelada ? "Si la tenías en tu calendario, se quitará sola con este aviso." : null,
            Cierre = cierre,
            Pie = $"{clinica.Nombre}{(clinica.Direccion is null ? "" : $" · {clinica.Direccion}")}\nRecibes este correo porque tienes una cita con nosotros.",
            Tono = aviso == Aviso.Cancelada ? "peligro" : "marca",
            ClinicaNombre = clinica.Nombre,
            LogoUrl = clinica.LogoUrl,
        };

        // El recordatorio no lleva invitacion: la cita ya esta en su calendario.
        var metodo = aviso switch { Aviso.Cancelada => "CANCEL", Aviso.Recordatorio => null, _ => "REQUEST" };
        _cola.Encolar(new MensajeCorreo
        {
            Para = paciente.CorreoElectronico,
            ParaNombre = paciente.NombreCompleto,
            Asunto = asunto,
            Html = PlantillaCorreo.Html(contenido),
            Texto = PlantillaCorreo.TextoPlano(contenido),
            Ics = metodo is null ? null : CalendarioIcs.Generar(Evento(cita, clinica,
                $"Cita de nutrición con {nutricionista.NombreCompleto}",
                paciente.CorreoElectronico, paciente.NombreCompleto), metodo),
            IcsMetodo = metodo,
        }, $"Cita{aviso}Paciente", cita.Id);
    }

    private void AlNutriologo(Cita cita, Paciente paciente, Nutricionista nutricionista, Clinica clinica,
        Aviso aviso, string? motivo = null)
    {
        if (string.IsNullOrWhiteSpace(nutricionista.CorreoElectronico)) return;

        var nombre = PrimerNombre(nutricionista.NombreCompleto);
        var (titulo, intro, asunto) = aviso switch
        {
            Aviso.Agendada => ("Tienes una *nueva cita*",
                $"{nombre}, se agendó una cita con {paciente.NombreCompleto}.",
                $"Nueva cita: {paciente.NombreCompleto}, {FechaCorta(cita)}"),
            Aviso.Reprogramada => ("Una cita *cambió*",
                $"{nombre}, la cita con {paciente.NombreCompleto} tiene nuevos datos.",
                $"Cita reprogramada: {paciente.NombreCompleto}, {FechaCorta(cita)}"),
            _ => ("Cita *cancelada*",
                $"{nombre}, se canceló la cita con {paciente.NombreCompleto}; ese horario queda libre.",
                $"Cita cancelada: {paciente.NombreCompleto}, {FechaCorta(cita)}"),
        };

        var datos = new List<PlantillaCorreo.Dato>
        {
            new("Hora", Horario(cita)),
            new("Paciente", paciente.NombreCompleto, string.IsNullOrWhiteSpace(paciente.Telefono) ? null : paciente.Telefono),
        };
        if (aviso != Aviso.Cancelada && !string.IsNullOrWhiteSpace(cita.Motivo)) datos.Add(new("Motivo", cita.Motivo));
        if (aviso == Aviso.Cancelada && !string.IsNullOrWhiteSpace(motivo)) datos.Add(new("Motivo", motivo));

        var botones = new List<PlantillaCorreo.Boton>();
        if (UrlSitio("/citas") is { } agenda) botones.Add(new("Abrir mi agenda", agenda, Primario: true));
        if (aviso != Aviso.Cancelada && !string.IsNullOrWhiteSpace(paciente.Telefono))
            botones.Add(new("Llamar al paciente", $"tel:{SoloDigitos(paciente.Telefono)}"));

        var contenido = new PlantillaCorreo.Contenido
        {
            Titulo = titulo,
            Introduccion = intro,
            FechaDestacada = FechaBoleto(cita),
            Datos = datos,
            Botones = botones,
            Pie = "Aviso automático de la agenda de NutriClínica.",
            Tono = aviso == Aviso.Cancelada ? "peligro" : "marca",
            ClinicaNombre = clinica.Nombre,
            LogoUrl = clinica.LogoUrl,
        };

        var metodo = aviso == Aviso.Cancelada ? "CANCEL" : "REQUEST";
        _cola.Encolar(new MensajeCorreo
        {
            Para = nutricionista.CorreoElectronico,
            ParaNombre = nutricionista.NombreCompleto,
            Asunto = asunto,
            Html = PlantillaCorreo.Html(contenido),
            Texto = PlantillaCorreo.TextoPlano(contenido),
            Ics = CalendarioIcs.Generar(Evento(cita, clinica,
                $"Consulta: {paciente.NombreCompleto}",
                nutricionista.CorreoElectronico, nutricionista.NombreCompleto), metodo),
            IcsMetodo = metodo,
        }, $"Cita{aviso}Nutriologo", cita.Id);
    }

    // ── Enlaces ──────────────────────────────────────────────────────

    /// <summary>Para quien abre el correo en un cliente que no reconoce la invitacion.</summary>
    private static string GoogleCalendar(Cita cita, Clinica clinica, string titulo)
    {
        static string F(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'");
        var url = "https://calendar.google.com/calendar/render?action=TEMPLATE"
                  + $"&text={Uri.EscapeDataString(titulo)}"
                  + $"&dates={F(cita.FechaInicio)}/{F(cita.FechaFin)}"
                  + $"&details={Uri.EscapeDataString(string.IsNullOrWhiteSpace(cita.Motivo) ? $"Cita en {clinica.Nombre}" : $"Motivo: {cita.Motivo}")}";
        if (clinica.Direccion is not null) url += $"&location={Uri.EscapeDataString(clinica.Direccion)}";
        return url;
    }

    private static string? Mapa(Clinica clinica) => clinica.Direccion is null
        ? null
        : $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(clinica.Direccion)}";

    private static string? Llamar(Clinica clinica) =>
        clinica.Telefono is null ? null : $"tel:{SoloDigitos(clinica.Telefono)}";

    private string? UrlSitio(string ruta) =>
        Uri.TryCreate(_config.UrlSitio, UriKind.Absolute, out var u) ? $"{u.ToString().TrimEnd('/')}{ruta}" : null;

    private static string SoloDigitos(string telefono) =>
        new(telefono.Where((ch, i) => char.IsDigit(ch) || (ch == '+' && i == 0)).ToArray());

    // ── Utilidades ───────────────────────────────────────────────────

    private CalendarioIcs.Evento Evento(Cita cita, Clinica clinica, string titulo, string correo, string nombre) => new(
        Uid: $"cita-{cita.Id}@nutriclinica",
        Secuencia: cita.SecuenciaCalendario,
        InicioUtc: cita.FechaInicio,
        FinUtc: cita.FechaFin,
        Titulo: titulo,
        Descripcion: string.IsNullOrWhiteSpace(cita.Motivo) ? $"Cita en {clinica.Nombre}." : $"Motivo: {cita.Motivo}",
        Lugar: clinica.Direccion,
        OrganizadorCorreo: string.IsNullOrWhiteSpace(_config.Remitente) ? "avisos@nutriclinica.local" : _config.Remitente,
        OrganizadorNombre: clinica.Nombre,
        AsistenteCorreo: correo,
        AsistenteNombre: nombre);

    private async Task<Clinica> ClinicaAsync()
    {
        var datos = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();
        return new Clinica(
            string.IsNullOrWhiteSpace(datos?.RazonSocial) ? "NutriClínica" : datos.RazonSocial,
            string.IsNullOrWhiteSpace(datos?.Direccion) ? null : datos.Direccion,
            string.IsNullOrWhiteSpace(datos?.Telefono) ? null : datos.Telefono,
            string.IsNullOrWhiteSpace(datos?.LogoUrl) ? UrlSitio("/logo-correo.png") : datos.LogoUrl);
    }

    /// <summary>"hoy", "mañana" o "el jueves 12", en hora de la clinica.</summary>
    private static string CuandoEs(Cita cita)
    {
        var dia = ZonaClinica.ALocal(cita.FechaInicio).Date;
        var hoy = ZonaClinica.Ahora().Date;
        return (dia - hoy).Days switch
        {
            0 => "hoy",
            1 => "mañana",
            _ => $"el {ZonaClinica.Formatear(cita.FechaInicio, "dddd d")}",
        };
    }

    private static PlantillaCorreo.Fecha FechaBoleto(Cita c) => new(
        ZonaClinica.Formatear(c.FechaInicio, "dddd"),
        ZonaClinica.Formatear(c.FechaInicio, "%d"),
        ZonaClinica.Formatear(c.FechaInicio, "MMMM"));

    private static string FechaCorta(Cita c) => ZonaClinica.Formatear(c.FechaInicio, "ddd d MMM, HH:mm");

    private static string Horario(Cita c) =>
        $"{ZonaClinica.Formatear(c.FechaInicio, "HH:mm")} – {ZonaClinica.Formatear(c.FechaFin, "HH:mm")} h";

    private static string PrimerNombre(string nombre) =>
        nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;

    private static string Primera(string texto) => texto.Length == 0 ? texto : char.ToUpper(texto[0]) + texto[1..];
}
