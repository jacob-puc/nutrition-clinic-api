using System.Globalization;

namespace nutriclinica_backend.Core.Utils;

/// <summary>
/// Hora de la clinica (Mexico) para todo lo que se imprime. La base guarda
/// instantes en UTC y el servidor en Render corre en UTC, asi que formatear
/// un DateTime "tal cual" o usar DateTime.Now imprime 6 horas de mas.
/// Mismo criterio que lib/zona-horaria.ts en el front.
/// </summary>
public static class ZonaClinica
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    private static readonly TimeZoneInfo Zona = ResolverZona();

    /* "America/Mexico_City" existe en Linux y, con ICU, tambien en Windows.
       Si el contenedor no trae la base de zonas, se usa UTC-6 fijo (Mexico
       ya no tiene horario de verano desde 2022). */
    private static TimeZoneInfo ResolverZona()
    {
        foreach (var id in new[] { "America/Mexico_City", "Central Standard Time (Mexico)" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Mexico", TimeSpan.FromHours(-6), "Mexico", "Mexico");
    }

    /// <summary>Instante UTC (o sin especificar, como lo devuelve EF) -> hora de Mexico.</summary>
    public static DateTime ALocal(DateTime instante)
    {
        var utc = instante.Kind switch
        {
            DateTimeKind.Utc => instante,
            DateTimeKind.Local => instante.ToUniversalTime(),
            _ => DateTime.SpecifyKind(instante, DateTimeKind.Utc),
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zona);
    }

    public static DateTime Ahora() => ALocal(DateTime.UtcNow);

    /// <summary>Hora de Mexico (reloj de pared) -> instante UTC.</summary>
    public static DateTime AUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zona);

    public static string Formatear(DateTime instante, string formato) =>
        ALocal(instante).ToString(formato, Cultura);

    /// <summary>"dd/MM/yyyy HH:mm" en hora de Mexico.</summary>
    public static string FechaHora(DateTime instante) => Formatear(instante, "dd/MM/yyyy HH:mm");

    public static string Fecha(DateTime instante) => Formatear(instante, "dd/MM/yyyy");
}
