using System.Text;

namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>
/// Evento iCalendar (RFC 5545) para invitaciones iMIP (RFC 6047). El UID es
/// fijo por cita y SEQUENCE sube en cada cambio: asi el calendario del
/// destinatario actualiza o cancela el mismo evento en vez de duplicarlo.
/// </summary>
public static class CalendarioIcs
{
    public record Evento(
        string Uid,
        int Secuencia,
        DateTime InicioUtc,
        DateTime FinUtc,
        string Titulo,
        string Descripcion,
        string? Lugar,
        string OrganizadorCorreo,
        string OrganizadorNombre,
        string AsistenteCorreo,
        string AsistenteNombre);

    /// <param name="metodo">REQUEST (crear o actualizar) o CANCEL.</param>
    public static string Generar(Evento e, string metodo)
    {
        var cancelar = metodo == "CANCEL";
        var lineas = new List<string>
        {
            "BEGIN:VCALENDAR",
            "PRODID:-//NutriClinica//Agenda//ES",
            "VERSION:2.0",
            "CALSCALE:GREGORIAN",
            $"METHOD:{metodo}",
            "BEGIN:VEVENT",
            $"UID:{e.Uid}",
            $"SEQUENCE:{e.Secuencia}",
            $"DTSTAMP:{Fecha(DateTime.UtcNow)}",
            $"DTSTART:{Fecha(e.InicioUtc)}",
            $"DTEND:{Fecha(e.FinUtc)}",
            $"SUMMARY:{Escapar(e.Titulo)}",
            $"DESCRIPTION:{Escapar(e.Descripcion)}",
        };
        if (!string.IsNullOrWhiteSpace(e.Lugar)) lineas.Add($"LOCATION:{Escapar(e.Lugar)}");
        lineas.Add($"ORGANIZER;CN={Parametro(e.OrganizadorNombre)}:mailto:{e.OrganizadorCorreo}");
        lineas.Add(
            $"ATTENDEE;CN={Parametro(e.AsistenteNombre)};ROLE=REQ-PARTICIPANT;"
            + $"PARTSTAT={(cancelar ? "DECLINED" : "ACCEPTED")};RSVP=FALSE:mailto:{e.AsistenteCorreo}");
        lineas.Add($"STATUS:{(cancelar ? "CANCELLED" : "CONFIRMED")}");
        lineas.Add("TRANSP:OPAQUE");
        if (!cancelar)
        {
            lineas.AddRange(
            [
                "BEGIN:VALARM",
                "ACTION:DISPLAY",
                "DESCRIPTION:Recordatorio de cita",
                "TRIGGER:-PT1H",
                "END:VALARM",
            ]);
        }
        lineas.Add("END:VEVENT");
        lineas.Add("END:VCALENDAR");

        var sb = new StringBuilder();
        foreach (var linea in lineas) sb.Append(Plegar(linea)).Append("\r\n");
        return sb.ToString();
    }

    private static string Fecha(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'");

    private static string Escapar(string texto) => texto
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    private static string Parametro(string texto) => $"\"{texto.Replace("\"", "'")}\"";

    /// <summary>Lineas de mas de 75 octetos se parten con CRLF + espacio.</summary>
    private static string Plegar(string linea)
    {
        var bytes = Encoding.UTF8.GetBytes(linea);
        if (bytes.Length <= 75) return linea;

        var sb = new StringBuilder();
        var actual = 0;
        var limite = 75;
        foreach (var rune in linea.EnumerateRunes())
        {
            var largo = rune.Utf8SequenceLength;
            if (actual + largo > limite)
            {
                sb.Append("\r\n ");
                actual = 1;
                limite = 75;
            }
            sb.Append(rune.ToString());
            actual += largo;
        }
        return sb.ToString();
    }
}
