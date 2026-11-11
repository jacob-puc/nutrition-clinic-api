using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Agenda.DTOs;
using nutriclinica_backend.Features.Nutricionistas.DTOs;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Agenda.Services;

public interface IAgendaService
{
    Task<HorariosRespuestaDto> ObtenerHorariosAsync();
    Task<HorariosRespuestaDto> ActualizarHorarioClinicaAsync(ActualizarHorarioClinicaDto dto);
    Task<HorarioPersonaDto> ActualizarHorarioPersonaAsync(Guid nutricionistaId, ActualizarHorarioPersonaDto dto);

    Task<IEnumerable<AusenciaDto>> ObtenerAusenciasAsync(Guid? nutricionistaId, DateOnly? desde);
    Task<AusenciaCreadaDto> CrearAusenciaAsync(Guid nutricionistaId, CrearAusenciaDto dto);
    Task EliminarAusenciaAsync(Guid nutricionistaId, Guid ausenciaId);

    /// <summary>
    /// Por que la persona no puede atender en ese intervalo (ausencia o fuera
    /// de horario), o null si puede. Instantes en UTC.
    /// </summary>
    Task<string?> MotivoNoDisponibleAsync(Guid nutricionistaId, DateTime inicio, DateTime fin);

    /// <summary>
    /// Huecos libres por dia: horario - ausencias - citas que ocupan agenda.
    /// Base para la reservacion en linea. Sin duracion usa la de la persona.
    /// </summary>
    Task<DisponibilidadDto> ObtenerDisponibilidadAsync(
        Guid nutricionistaId, DateOnly desde, DateOnly hasta, int? duracionMinutos, int pasoMinutos);

    /// <summary>Disponibilidad de todo el equipo que atiende, para "con quien sea".</summary>
    Task<IEnumerable<DisponibilidadDto>> ObtenerDisponibilidadEquipoAsync(
        DateOnly desde, DateOnly hasta, int? duracionMinutos, int pasoMinutos);
}

public class AgendaService : IAgendaService
{
    private const int MaxBloquesPorDia = 6;

    private static readonly EstadoCita[] EstadosPendientes = [EstadoCita.Programada, EstadoCita.Confirmada];

    private readonly ApplicationDbContext _context;

    public AgendaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HorariosRespuestaDto> ObtenerHorariosAsync()
    {
        var bloques = await _context.HorariosAtencion.AsNoTracking().ToListAsync();
        var personas = await _context.Nutricionistas
            .AsNoTracking()
            .Where(n => n.IsActive && n.Rol != RolUsuario.Recepcion)
            .Select(n => new { n.Id, n.HorarioPropio, n.DuracionConsultaMinutos })
            .ToListAsync();

        var clinica = Ordenar(bloques.Where(b => b.NutricionistaId == null));

        return new HorariosRespuestaDto
        {
            Clinica = clinica,
            Personas = personas.Select(p => new HorarioPersonaDto
            {
                NutricionistaId = p.Id,
                HorarioPropio = p.HorarioPropio,
                DuracionConsultaMinutos = p.DuracionConsultaMinutos,
                Bloques = p.HorarioPropio ? Ordenar(bloques.Where(b => b.NutricionistaId == p.Id)) : clinica
            }).ToList()
        };
    }

    public async Task<HorariosRespuestaDto> ActualizarHorarioClinicaAsync(ActualizarHorarioClinicaDto dto)
    {
        var nuevos = Convertir(dto.Bloques, null);

        var actuales = await _context.HorariosAtencion.Where(h => h.NutricionistaId == null).ToListAsync();
        _context.HorariosAtencion.RemoveRange(actuales);
        _context.HorariosAtencion.AddRange(nuevos);
        await _context.SaveChangesAsync();

        return await ObtenerHorariosAsync();
    }

    public async Task<HorarioPersonaDto> ActualizarHorarioPersonaAsync(Guid nutricionistaId, ActualizarHorarioPersonaDto dto)
    {
        if (dto.DuracionConsultaMinutos is < 15 or > 240)
            throw Falla("La duración de la consulta debe estar entre 15 y 240 minutos.", "DuracionConsultaMinutos");

        var persona = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == nutricionistaId && n.IsActive)
            ?? throw new KeyNotFoundException("No encontramos esa cuenta. Puede que se haya eliminado.");

        var nuevos = dto.HorarioPropio ? Convertir(dto.Bloques, nutricionistaId) : [];

        var actuales = await _context.HorariosAtencion.Where(h => h.NutricionistaId == nutricionistaId).ToListAsync();
        _context.HorariosAtencion.RemoveRange(actuales);
        _context.HorariosAtencion.AddRange(nuevos);

        persona.HorarioPropio = dto.HorarioPropio;
        persona.DuracionConsultaMinutos = dto.DuracionConsultaMinutos;
        await _context.SaveChangesAsync();

        var horarios = await ObtenerHorariosAsync();
        return horarios.Personas.FirstOrDefault(p => p.NutricionistaId == nutricionistaId)
               ?? new HorarioPersonaDto
               {
                   NutricionistaId = nutricionistaId,
                   HorarioPropio = persona.HorarioPropio,
                   DuracionConsultaMinutos = persona.DuracionConsultaMinutos
               };
    }

    public async Task<IEnumerable<AusenciaDto>> ObtenerAusenciasAsync(Guid? nutricionistaId, DateOnly? desde)
    {
        var limite = desde ?? HoyClinica();
        var query = _context.Ausencias.AsNoTracking().Where(a => a.FechaFin >= limite);
        if (nutricionistaId.HasValue) query = query.Where(a => a.NutricionistaId == nutricionistaId.Value);

        return await query
            .OrderBy(a => a.FechaInicio)
            .Select(a => new AusenciaDto
            {
                Id = a.Id,
                NutricionistaId = a.NutricionistaId,
                NutricionistaNombre = a.Nutricionista.NombreCompleto,
                FechaInicio = a.FechaInicio,
                FechaFin = a.FechaFin,
                Motivo = a.Motivo
            })
            .ToListAsync();
    }

    public async Task<AusenciaCreadaDto> CrearAusenciaAsync(Guid nutricionistaId, CrearAusenciaDto dto)
    {
        if (dto.FechaFin < dto.FechaInicio)
            throw Falla("La fecha final no puede ser anterior a la inicial.", "FechaFin");
        if (dto.FechaFin < HoyClinica())
            throw Falla("La ausencia ya terminó: elige fechas de hoy en adelante.", "FechaFin");
        if (dto.FechaFin.DayNumber - dto.FechaInicio.DayNumber > 365)
            throw Falla("Una ausencia no puede durar más de un año.", "FechaFin");

        var motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim();
        if (motivo?.Length > 150)
            throw Falla("El motivo no puede tener más de 150 caracteres.", "Motivo");

        var persona = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nutricionistaId && n.IsActive)
            ?? throw new KeyNotFoundException("No encontramos esa cuenta. Puede que se haya eliminado.");

        var traslape = await _context.Ausencias.AnyAsync(a =>
            a.NutricionistaId == nutricionistaId && a.FechaInicio <= dto.FechaFin && dto.FechaInicio <= a.FechaFin);
        if (traslape)
            throw Falla("Ya tiene una ausencia registrada en esas fechas.");

        var ausencia = new Ausencia
        {
            NutricionistaId = nutricionistaId,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            Motivo = motivo
        };
        _context.Ausencias.Add(ausencia);
        await _context.SaveChangesAsync();

        return new AusenciaCreadaDto
        {
            Ausencia = new AusenciaDto
            {
                Id = ausencia.Id,
                NutricionistaId = nutricionistaId,
                NutricionistaNombre = persona.NombreCompleto,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin,
                Motivo = ausencia.Motivo
            },
            CitasAfectadas = await CitasEnFechasAsync(nutricionistaId, dto.FechaInicio, dto.FechaFin)
        };
    }

    public async Task EliminarAusenciaAsync(Guid nutricionistaId, Guid ausenciaId)
    {
        var ausencia = await _context.Ausencias
            .FirstOrDefaultAsync(a => a.Id == ausenciaId && a.NutricionistaId == nutricionistaId)
            ?? throw new KeyNotFoundException("No encontramos esa ausencia. Puede que se haya eliminado.");

        _context.Ausencias.Remove(ausencia);
        await _context.SaveChangesAsync();
    }

    public async Task<string?> MotivoNoDisponibleAsync(Guid nutricionistaId, DateTime inicio, DateTime fin)
    {
        var persona = await _context.Nutricionistas
            .AsNoTracking()
            .Where(n => n.Id == nutricionistaId)
            .Select(n => new { n.NombreCompleto, n.HorarioPropio })
            .FirstOrDefaultAsync();
        if (persona is null) return null;

        var nombre = persona.NombreCompleto.Split(' ')[0];
        var desde = ZonaClinica.ALocal(inicio);
        var hasta = ZonaClinica.ALocal(fin);
        var fecha = DateOnly.FromDateTime(desde);

        /* Una cita que termina justo a medianoche sigue siendo del mismo dia. */
        var terminaAMedianoche = hasta.TimeOfDay == TimeSpan.Zero && hasta.Date == desde.Date.AddDays(1);
        if (hasta.Date != desde.Date && !terminaAMedianoche)
            return "La cita debe empezar y terminar el mismo día.";

        var ausencia = await _context.Ausencias
            .AsNoTracking()
            .Where(a => a.NutricionistaId == nutricionistaId && a.FechaInicio <= fecha && fecha <= a.FechaFin)
            .Select(a => new { a.FechaFin, a.Motivo })
            .FirstOrDefaultAsync();
        if (ausencia is not null)
        {
            var hastaTexto = ausencia.FechaFin.ToString("d 'de' MMMM", ZonaClinica.Cultura);
            var motivo = ausencia.Motivo is null ? "" : $" ({ausencia.Motivo.ToLower(ZonaClinica.Cultura)})";
            return $"{nombre} no atiende ese día{motivo}: está ausente hasta el {hastaTexto}.";
        }

        var duenoId = persona.HorarioPropio ? nutricionistaId : (Guid?)null;
        var dia = DiaDe(desde.DayOfWeek);
        var bloquesSemana = await _context.HorariosAtencion
            .AsNoTracking()
            .Where(h => h.NutricionistaId == duenoId)
            .ToListAsync();

        /* Sin horario de clinica configurado no se restringe nada (compatibilidad). */
        if (!persona.HorarioPropio && bloquesSemana.Count == 0) return null;

        var bloques = bloquesSemana.Where(b => b.DiaSemana == dia).OrderBy(b => b.HoraInicio).ToList();
        var nombreDia = NombreDia(dia);
        if (bloques.Count == 0)
            return $"{nombre} no atiende los {nombreDia}.";

        var horaInicio = TimeOnly.FromDateTime(desde);
        var horaFin = terminaAMedianoche ? TimeOnly.MaxValue : TimeOnly.FromDateTime(hasta);
        var dentro = bloques.Any(b => b.HoraInicio <= horaInicio && horaFin <= b.HoraFin);
        if (dentro) return null;

        var rangos = string.Join(" y ", bloques.Select(b => $"{Texto(b.HoraInicio)} a {Texto(b.HoraFin)}"));
        return $"Fuera de horario: los {nombreDia} {nombre} atiende de {rangos}.";
    }

    /* Sin horario configurado se ofrece el rango que la agenda usaba antes. */
    private static readonly TimeOnly AperturaPorDefecto = new(8, 0);
    private static readonly TimeOnly CierrePorDefecto = new(20, 0);
    private const int MaxDiasDisponibilidad = 31;

    private static readonly EstadoCita[] EstadosQueOcupan = [EstadoCita.Programada, EstadoCita.Confirmada, EstadoCita.EnCurso];

    public async Task<DisponibilidadDto> ObtenerDisponibilidadAsync(
        Guid nutricionistaId, DateOnly desde, DateOnly hasta, int? duracionMinutos, int pasoMinutos)
    {
        ValidarRango(desde, hasta, duracionMinutos, pasoMinutos);

        var persona = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nutricionistaId && n.IsActive)
            ?? throw new KeyNotFoundException("No encontramos esa cuenta. Puede que se haya eliminado.");

        if (persona.Rol == RolUsuario.Recepcion)
            throw Falla("Recepción no atiende pacientes.");

        var resultado = await CalcularAsync([persona], desde, hasta, duracionMinutos, pasoMinutos);
        return resultado[0];
    }

    public async Task<IEnumerable<DisponibilidadDto>> ObtenerDisponibilidadEquipoAsync(
        DateOnly desde, DateOnly hasta, int? duracionMinutos, int pasoMinutos)
    {
        ValidarRango(desde, hasta, duracionMinutos, pasoMinutos);

        var personas = await _context.Nutricionistas
            .AsNoTracking()
            .Where(n => n.IsActive && n.Rol != RolUsuario.Recepcion)
            .OrderBy(n => n.NombreCompleto)
            .ToListAsync();

        return await CalcularAsync(personas, desde, hasta, duracionMinutos, pasoMinutos);
    }

    /// <summary>Carga todo en tres consultas y calcula en memoria: son pocas personas y dias.</summary>
    private async Task<List<DisponibilidadDto>> CalcularAsync(
        List<Nutricionista> personas, DateOnly desde, DateOnly hasta, int? duracionMinutos, int pasoMinutos)
    {
        var ids = personas.Select(p => p.Id).ToList();

        var bloques = await _context.HorariosAtencion
            .AsNoTracking()
            .Where(h => h.NutricionistaId == null || ids.Contains(h.NutricionistaId.Value))
            .ToListAsync();
        var clinica = bloques.Where(b => b.NutricionistaId == null).ToList();

        var ausencias = await _context.Ausencias
            .AsNoTracking()
            .Where(a => ids.Contains(a.NutricionistaId) && a.FechaInicio <= hasta && desde <= a.FechaFin)
            .ToListAsync();

        /* Margen de un dia por la diferencia de zona horaria con UTC. */
        var inicioUtc = ZonaClinica.AUtc(desde.ToDateTime(TimeOnly.MinValue)).AddDays(-1);
        var finUtc = ZonaClinica.AUtc(hasta.ToDateTime(TimeOnly.MinValue)).AddDays(2);
        var citas = await _context.Citas
            .AsNoTracking()
            .Where(c => ids.Contains(c.NutricionistaId)
                        && EstadosQueOcupan.Contains(c.Estado)
                        && c.FechaInicio < finUtc
                        && inicioUtc < c.FechaFin)
            .Select(c => new { c.NutricionistaId, c.FechaInicio, c.FechaFin })
            .ToListAsync();

        var ahora = DateTime.UtcNow;
        var resultado = new List<DisponibilidadDto>();

        foreach (var persona in personas)
        {
            var duracion = duracionMinutos ?? persona.DuracionConsultaMinutos;
            var propios = persona.HorarioPropio ? bloques.Where(b => b.NutricionistaId == persona.Id).ToList() : clinica;
            var sinRestriccion = !persona.HorarioPropio && clinica.Count == 0;
            var ocupadas = citas.Where(c => c.NutricionistaId == persona.Id).ToList();
            var nombre = persona.NombreCompleto.Split(' ')[0];

            var dto = new DisponibilidadDto
            {
                NutricionistaId = persona.Id,
                NutricionistaNombre = persona.NombreCompleto,
                DuracionMinutos = duracion
            };

            for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            {
                var dia = new DisponibilidadDiaDto { Fecha = fecha };
                dto.Dias.Add(dia);

                var ausencia = ausencias.FirstOrDefault(a =>
                    a.NutricionistaId == persona.Id && a.FechaInicio <= fecha && fecha <= a.FechaFin);
                if (ausencia is not null)
                {
                    dia.MotivoNoDisponible = ausencia.Motivo ?? "Ausente";
                    continue;
                }

                var diaSemana = DiaDe(fecha.DayOfWeek);
                var franjas = sinRestriccion
                    ? [(AperturaPorDefecto, CierrePorDefecto)]
                    : propios.Where(b => b.DiaSemana == diaSemana)
                        .OrderBy(b => b.HoraInicio)
                        .Select(b => (b.HoraInicio, b.HoraFin))
                        .ToList();

                if (franjas.Count == 0)
                {
                    dia.MotivoNoDisponible = $"{nombre} no atiende los {NombreDia(diaSemana)}";
                    continue;
                }

                foreach (var (abre, cierra) in franjas)
                {
                    /* En minutos del dia: TimeOnly da la vuelta a medianoche. */
                    var limite = (int)cierra.ToTimeSpan().TotalMinutes;
                    for (var minuto = (int)abre.ToTimeSpan().TotalMinutes; minuto + duracion <= limite; minuto += pasoMinutos)
                    {
                        var hora = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minuto));
                        var inicio = ZonaClinica.AUtc(fecha.ToDateTime(hora));
                        var fin = inicio.AddMinutes(duracion);
                        if (inicio <= ahora) continue;
                        if (ocupadas.Any(o => o.FechaInicio < fin && inicio < o.FechaFin)) continue;

                        dia.Huecos.Add(new HuecoDto { Inicio = inicio, Fin = fin, Hora = Texto(hora) });
                    }
                }
            }

            resultado.Add(dto);
        }

        return resultado;
    }

    private static void ValidarRango(DateOnly desde, DateOnly hasta, int? duracionMinutos, int pasoMinutos)
    {
        if (hasta < desde)
            throw Falla("La fecha final no puede ser anterior a la inicial.", "hasta");
        if (hasta.DayNumber - desde.DayNumber >= MaxDiasDisponibilidad)
            throw Falla($"Consulta como máximo {MaxDiasDisponibilidad} días a la vez.", "hasta");
        if (duracionMinutos is < 15 or > 240)
            throw Falla("La duración debe estar entre 15 y 240 minutos.", "duracion");
        if (pasoMinutos is < 5 or > 120)
            throw Falla("El paso debe estar entre 5 y 120 minutos.", "paso");
    }

    private async Task<List<CitaPendienteDto>> CitasEnFechasAsync(Guid nutricionistaId, DateOnly desde, DateOnly hasta)
    {
        var ahora = DateTime.UtcNow;
        var citas = await (
            from c in _context.Citas.AsNoTracking()
            join p in _context.Pacientes.AsNoTracking() on c.PacienteId equals p.Id
            where c.NutricionistaId == nutricionistaId
                  && EstadosPendientes.Contains(c.Estado)
                  && c.FechaFin > ahora
            orderby c.FechaInicio
            select new CitaPendienteDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                PacienteNombre = p.NombreCompleto,
                FechaInicio = c.FechaInicio,
                FechaFin = c.FechaFin,
                Estado = c.Estado.ToString()
            })
            .ToListAsync();

        return citas
            .Where(c =>
            {
                var dia = DateOnly.FromDateTime(ZonaClinica.ALocal(c.FechaInicio));
                return desde <= dia && dia <= hasta;
            })
            .ToList();
    }

    private static List<HorarioAtencion> Convertir(List<BloqueHorarioDto> bloques, Guid? nutricionistaId)
    {
        var resultado = new List<HorarioAtencion>();
        foreach (var b in bloques)
        {
            if (!Enum.IsDefined(b.DiaSemana))
                throw Falla("Día de la semana no válido.", "DiaSemana");

            var inicio = LeerHora(b.HoraInicio);
            var fin = LeerHora(b.HoraFin);
            if (fin <= inicio)
                throw Falla($"{NombreDiaSingular(b.DiaSemana)}: la hora de fin debe ser posterior a la de inicio.");

            resultado.Add(new HorarioAtencion
            {
                Id = Guid.NewGuid(),
                NutricionistaId = nutricionistaId,
                DiaSemana = b.DiaSemana,
                HoraInicio = inicio,
                HoraFin = fin
            });
        }

        foreach (var grupo in resultado.GroupBy(r => r.DiaSemana))
        {
            var lista = grupo.OrderBy(r => r.HoraInicio).ToList();
            if (lista.Count > MaxBloquesPorDia)
                throw Falla($"{NombreDiaSingular(grupo.Key)}: máximo {MaxBloquesPorDia} bloques por día.");
            for (var i = 1; i < lista.Count; i++)
            {
                if (lista[i].HoraInicio < lista[i - 1].HoraFin)
                    throw Falla($"{NombreDiaSingular(grupo.Key)}: hay bloques que se enciman.");
            }
        }

        return resultado;
    }

    private static TimeOnly LeerHora(string valor)
    {
        if (TimeOnly.TryParseExact(valor, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            return hora;
        throw Falla($"Hora no válida: \"{valor}\". Usa el formato HH:mm.");
    }

    private static string Texto(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static List<BloqueHorarioDto> Ordenar(IEnumerable<HorarioAtencion> bloques) =>
        bloques
            .OrderBy(b => b.DiaSemana)
            .ThenBy(b => b.HoraInicio)
            .Select(b => new BloqueHorarioDto
            {
                DiaSemana = b.DiaSemana,
                HoraInicio = Texto(b.HoraInicio),
                HoraFin = Texto(b.HoraFin)
            })
            .ToList();

    private static DateOnly HoyClinica() => DateOnly.FromDateTime(ZonaClinica.Ahora());

    private static DiaSemana DiaDe(DayOfWeek dia) => dia == DayOfWeek.Sunday ? DiaSemana.Domingo : (DiaSemana)(int)dia;

    private static string NombreDia(DiaSemana dia) => dia switch
    {
        DiaSemana.Lunes => "lunes",
        DiaSemana.Martes => "martes",
        DiaSemana.Miercoles => "miércoles",
        DiaSemana.Jueves => "jueves",
        DiaSemana.Viernes => "viernes",
        DiaSemana.Sabado => "sábados",
        _ => "domingos"
    };

    private static string NombreDiaSingular(DiaSemana dia) => dia switch
    {
        DiaSemana.Lunes => "Lunes",
        DiaSemana.Martes => "Martes",
        DiaSemana.Miercoles => "Miércoles",
        DiaSemana.Jueves => "Jueves",
        DiaSemana.Viernes => "Viernes",
        DiaSemana.Sabado => "Sábado",
        _ => "Domingo"
    };

    private static ValidationException Falla(string mensaje, string campo = "") =>
        new([new ValidationFailure(campo, mensaje)]);
}
