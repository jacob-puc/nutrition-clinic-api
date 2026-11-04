using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Agenda.Services;
using nutriclinica_backend.Features.Citas.DTOs;
using nutriclinica_backend.Features.Citas.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Citas.Services;

public class CitaService : ICitaService
{
    private static readonly Dictionary<EstadoCita, EstadoCita[]> TransicionesPermitidas = new()
    {
        [EstadoCita.Programada] = [EstadoCita.Confirmada, EstadoCita.Cancelada, EstadoCita.NoAsistio],
        [EstadoCita.Confirmada] = [EstadoCita.EnCurso, EstadoCita.Cancelada, EstadoCita.NoAsistio],
        [EstadoCita.EnCurso] = [EstadoCita.Cancelada, EstadoCita.NoAsistio],
        [EstadoCita.Completada] = [],
        [EstadoCita.Cancelada] = [],
        [EstadoCita.NoAsistio] = [EstadoCita.Programada]
    };

    // Estados desde los que tiene sentido abrir un encuentro clinico.
    // Nunca desde Cancelada ni NoAsistio: sin atencion no hay registro clinico.
    public static readonly EstadoCita[] EstadosAtendibles =
    [
        EstadoCita.Confirmada,
        EstadoCita.EnCurso
    ];

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearCitaDto> _crearCitaValidator;
    private readonly IValidator<ActualizarCitaDto> _actualizarCitaValidator;
    private readonly IValidator<CambiarEstadoCitaDto> _cambiarEstadoCitaValidator;
    private readonly IAgendaService _agenda;

    public CitaService(
        ApplicationDbContext context,
        IValidator<CrearCitaDto> crearCitaValidator,
        IValidator<ActualizarCitaDto> actualizarCitaValidator,
        IValidator<CambiarEstadoCitaDto> cambiarEstadoCitaValidator,
        IAgendaService agenda)
    {
        _context = context;
        _agenda = agenda;
        _crearCitaValidator = crearCitaValidator;
        _actualizarCitaValidator = actualizarCitaValidator;
        _cambiarEstadoCitaValidator = cambiarEstadoCitaValidator;
    }

    public async Task<CitaRespuestaDto> CrearCitaAsync(CrearCitaDto dto)
    {
        await _crearCitaValidator.ValidateAndThrowAsync(dto);

        var paciente = await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.PacienteId && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {dto.PacienteId}");

        var nutricionista = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == dto.NutricionistaId && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un nutricionista activo con ID: {dto.NutricionistaId}");
        ValidarQueAtiende(nutricionista);
        await ValidarHorarioAsync(dto.NutricionistaId, dto.FechaInicio, dto.FechaFin);

        await ValidarDisponibilidadAgendaAsync(dto.NutricionistaId, dto.PacienteId, dto.FechaInicio, dto.FechaFin);

        var cita = new Cita
        {
            PacienteId = dto.PacienteId,
            NutricionistaId = dto.NutricionistaId,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            Estado = EstadoCita.Programada,
            Motivo = dto.Motivo,
            Canal = dto.Canal,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Citas.Add(cita);
        await _context.SaveChangesAsync();

        return MapToDto(cita, paciente.NombreCompleto, nutricionista.NombreCompleto, null);
    }

    public async Task<CitaRespuestaDto> ActualizarCitaAsync(Guid citaId, ActualizarCitaDto dto)
    {
        await _actualizarCitaValidator.ValidateAndThrowAsync(dto);

        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.Id == citaId)
            ?? throw new KeyNotFoundException($"No se encontró la cita con ID: {citaId}");

        if (cita.Estado == EstadoCita.EnCurso || EsTerminal(cita.Estado))
        {
            throw new ValidationException(
                $"No se puede modificar una cita en estado {cita.Estado}. Cambia su estado primero.");
        }

        var nutricionista = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == dto.NutricionistaId && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un nutricionista activo con ID: {dto.NutricionistaId}");
        ValidarQueAtiende(nutricionista);

        var paciente = await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cita.PacienteId && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {cita.PacienteId}");
        /* Solo si cambia la franja o quien atiende: editar el motivo de una
           cita vieja no debe fallar porque despues cambio el horario. */
        var cambiaFranja = cita.NutricionistaId != dto.NutricionistaId
                           || cita.FechaInicio != dto.FechaInicio
                           || cita.FechaFin != dto.FechaFin;
        if (cambiaFranja)
            await ValidarHorarioAsync(dto.NutricionistaId, dto.FechaInicio, dto.FechaFin);

        await ValidarDisponibilidadAgendaAsync(dto.NutricionistaId, cita.PacienteId, dto.FechaInicio, dto.FechaFin, citaId);

        cita.NutricionistaId = dto.NutricionistaId;
        cita.FechaInicio = dto.FechaInicio;
        cita.FechaFin = dto.FechaFin;
        cita.Motivo = dto.Motivo;

        await _context.SaveChangesAsync();

        return MapToDto(cita, paciente.NombreCompleto, nutricionista.NombreCompleto, null);
    }

    public async Task<CitaRespuestaDto> ObtenerCitaPorIdAsync(Guid citaId)
    {
        var cita = await _context.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Nutricionista)
            .Include(c => c.Consulta)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == citaId)
            ?? throw new KeyNotFoundException($"No se encontró la cita con ID: {citaId}");

        return MapToDto(cita, cita.Paciente.NombreCompleto, cita.Nutricionista.NombreCompleto, cita.Consulta?.Id);
    }

    public async Task<CitaRespuestaDto> CambiarEstadoAsync(Guid citaId, CambiarEstadoCitaDto dto)
    {
        await _cambiarEstadoCitaValidator.ValidateAndThrowAsync(dto);

        var cita = await _context.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Nutricionista)
            .Include(c => c.Consulta)
            .FirstOrDefaultAsync(c => c.Id == citaId)
            ?? throw new KeyNotFoundException($"No se encontró la cita con ID: {citaId}");

        var permitidas = TransicionesPermitidas[cita.Estado];

        if (!permitidas.Contains(dto.Estado))
        {
            throw new ConflictException(
                $"No se puede cambiar el estado de la cita de {cita.Estado} a {dto.Estado}.");
        }

        cita.Estado = dto.Estado;

        if (dto.Estado == EstadoCita.Cancelada)
        {
            cita.FechaCancelacion = DateTime.UtcNow;
            cita.MotivoCancelacion = dto.Motivo;
        }

        await _context.SaveChangesAsync();

        return MapToDto(cita, cita.Paciente.NombreCompleto, cita.Nutricionista.NombreCompleto, cita.Consulta?.Id);
    }

    public async Task<RespuestaPaginadaDto<CitaRespuestaDto>> ObtenerCitasAsync(CitaFiltroDto filtro)
    {
        var query = _context.Citas.AsNoTracking().AsQueryable();

        if (filtro.NutricionistaId.HasValue)
        {
            query = query.Where(c => c.NutricionistaId == filtro.NutricionistaId.Value);
        }

        if (filtro.PacienteId.HasValue)
        {
            query = query.Where(c => c.PacienteId == filtro.PacienteId.Value);
        }

        if (filtro.Estado.HasValue)
        {
            query = query.Where(c => c.Estado == filtro.Estado.Value);
        }

        if (filtro.Desde.HasValue)
        {
            query = query.Where(c => c.FechaFin >= filtro.Desde.Value);
        }

        if (filtro.Hasta.HasValue)
        {
            query = query.Where(c => c.FechaInicio <= filtro.Hasta.Value);
        }

        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, 100);
        var totalRegistros = await query.CountAsync();

        var proyeccion = await (
            from c in query
            join p in _context.Pacientes.AsNoTracking() on c.PacienteId equals p.Id
            join n in _context.Nutricionistas.AsNoTracking() on c.NutricionistaId equals n.Id
            select new
            {
                Cita = c,
                PacienteNombre = p.NombreCompleto,
                NutricionistaNombre = n.NombreCompleto,
                ConsultaId = c.Consulta == null ? (Guid?)null : c.Consulta.Id
            })
            .OrderBy(x => x.Cita.FechaInicio)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new RespuestaPaginadaDto<CitaRespuestaDto>
        {
            TotalRegistros = totalRegistros,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanoPagina),
            Datos = proyeccion
                .Select(x => MapToDto(x.Cita, x.PacienteNombre, x.NutricionistaNombre, x.ConsultaId))
                .ToList()
        };
    }

    public async Task<IEnumerable<CitaRespuestaDto>> ObtenerCitasPorPacienteAsync(Guid pacienteId)
    {
        var proyeccion = await (
            from c in _context.Citas.AsNoTracking()
            join p in _context.Pacientes.AsNoTracking() on c.PacienteId equals p.Id
            join n in _context.Nutricionistas.AsNoTracking() on c.NutricionistaId equals n.Id
            where c.PacienteId == pacienteId
            orderby c.FechaInicio descending
            select new
            {
                Cita = c,
                PacienteNombre = p.NombreCompleto,
                NutricionistaNombre = n.NombreCompleto,
                ConsultaId = c.Consulta == null ? (Guid?)null : c.Consulta.Id
            })
            .ToListAsync();

        return proyeccion
            .Select(x => MapToDto(x.Cita, x.PacienteNombre, x.NutricionistaNombre, x.ConsultaId))
            .ToList();
    }

    public async Task EliminarCitaAsync(Guid citaId)
    {
        var cita = await _context.Citas
            .FirstOrDefaultAsync(c => c.Id == citaId)
            ?? throw new KeyNotFoundException($"No se encontró la cita con ID: {citaId}");

        if (cita.Estado != EstadoCita.Programada)
        {
            throw new ValidationException(
                "Solo se pueden eliminar citas que estén en estado Programada. " +
                "Usa el endpoint de cambio de estado para cancelarla.");
        }

        _context.Citas.Remove(cita);
        await _context.SaveChangesAsync();
    }

    private async Task ValidarDisponibilidadAgendaAsync(
        Guid nutricionistaId,
        Guid pacienteId,
        DateTime fechaInicio,
        DateTime fechaFin,
        Guid? citaExcluidaId = null)
    {
        var estadosBloqueantes = new[]
        {
            EstadoCita.Programada,
            EstadoCita.Confirmada,
            EstadoCita.EnCurso
        };
        var existeCitaNutricionista = await _context.Citas
            .AsNoTracking()
            .AnyAsync(c => c.NutricionistaId == nutricionistaId
                           && estadosBloqueantes.Contains(c.Estado)
                           && (!citaExcluidaId.HasValue || c.Id != citaExcluidaId.Value)
                           && c.FechaInicio < fechaFin
                           && fechaInicio < c.FechaFin);
        if (existeCitaNutricionista)
        {
            throw new ConflictException("El nutricionista no está disponible en el horario seleccionado.");
        }
        var existeCitaPaciente = await _context.Citas
            .AsNoTracking()
            .AnyAsync(c => c.PacienteId == pacienteId
                           && estadosBloqueantes.Contains(c.Estado)
                           && (!citaExcluidaId.HasValue || c.Id != citaExcluidaId.Value)
                           && c.FechaInicio < fechaFin
                           && fechaInicio < c.FechaFin);
        if (existeCitaPaciente)
        {
            throw new ConflictException("El paciente ya tiene una cita en el horario seleccionado.");
        }
    }
    /// <summary>Horario de atencion y ausencias de quien atiende (409, como los choques).</summary>
    private async Task ValidarHorarioAsync(Guid nutricionistaId, DateTime inicio, DateTime fin)
    {
        var motivo = await _agenda.MotivoNoDisponibleAsync(nutricionistaId, inicio, fin);
        if (motivo is not null) throw new ConflictException(motivo);
    }

    /// <summary>Las cuentas de recepcion agendan, pero no se les puede asignar una cita.</summary>
    private static void ValidarQueAtiende(Nutricionista nutricionista)
    {
        if (nutricionista.Rol == RolUsuario.Recepcion)
        {
            throw new ValidationException(
                [new FluentValidation.Results.ValidationFailure("NutricionistaId", "Recepción no atiende pacientes: elige a un nutricionista.")]);
        }
    }

    private static bool EsTerminal(EstadoCita estado) =>
        TransicionesPermitidas[estado].Length == 0;

    private static CitaRespuestaDto MapToDto(Cita cita, string pacienteNombre, string nutricionistaNombre, Guid? consultaId) => new()
    {
        Id = cita.Id,
        PacienteId = cita.PacienteId,
        PacienteNombre = pacienteNombre,
        NutricionistaId = cita.NutricionistaId,
        NutricionistaNombre = nutricionistaNombre,
        FechaInicio = cita.FechaInicio,
        FechaFin = cita.FechaFin,
        Estado = cita.Estado,
        Motivo = cita.Motivo,
        Canal = cita.Canal,
        ConsultaId = consultaId,
        FechaCreacion = cita.FechaCreacion,
        FechaCancelacion = cita.FechaCancelacion,
        MotivoCancelacion = cita.MotivoCancelacion
    };
}
