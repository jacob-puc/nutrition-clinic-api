using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Citas.Services;
using nutriclinica_backend.Features.Consultas.DTOs;
using nutriclinica_backend.Features.Consultas.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Consultas.Services;

public class ConsultaService : IConsultaService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearConsultaDto> _crearConsultaValidator;
    private readonly IValidator<CerrarConsultaDto> _cerrarConsultaValidator;

    public ConsultaService(
        ApplicationDbContext context,
        IValidator<CrearConsultaDto> crearConsultaValidator,
        IValidator<CerrarConsultaDto> cerrarConsultaValidator)
    {
        _context = context;
        _crearConsultaValidator = crearConsultaValidator;
        _cerrarConsultaValidator = cerrarConsultaValidator;
    }

    public async Task<ConsultaRespuestaDto> CrearConsultaAsync(CrearConsultaDto dto)
    {
        await _crearConsultaValidator.ValidateAndThrowAsync(dto);

        Cita? cita = null;
        Guid nutricionistaId;

        if (dto.CitaId.HasValue)
        {
            cita = await _context.Citas
                .Include(c => c.Consulta)
                .FirstOrDefaultAsync(c => c.Id == dto.CitaId.Value)
                ?? throw new KeyNotFoundException($"No se encontró la cita con ID: {dto.CitaId}");

            if (cita.PacienteId != dto.PacienteId)
            {
                throw new ValidationException(
                    $"La cita {cita.Id} pertenece a otro paciente. No se puede registrar la consulta sobre ella.");
            }

            if (cita.Consulta != null)
            {
                throw new ConflictException(
                    $"La cita {cita.Id} ya tiene la consulta {cita.Consulta.Id} registrada.");
            }

            if (!CitaService.EstadosAtendibles.Contains(cita.Estado))
            {
                throw new ValidationException(
                    $"No se puede registrar una consulta sobre una cita en estado {cita.Estado}. " +
                    "Solo se admiten citas Confirmada o EnCurso.");
            }

            // El nutricionista es el de la cita salvo que se indique otro Explicitamente.
            nutricionistaId = dto.NutricionistaId ?? cita.NutricionistaId;
        }
        else
        {
            nutricionistaId = dto.NutricionistaId!.Value;
        }

        var paciente = await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.PacienteId && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {dto.PacienteId}");

        var nutricionista = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nutricionistaId && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un nutricionista activo con ID: {nutricionistaId}");

        var consulta = new Consulta
        {
            PacienteId = dto.PacienteId,
            CitaId = cita?.Id,
            NutricionistaId = nutricionistaId,
            TipoConsulta = dto.TipoConsulta,
            // Cita.FechaInicio es el slot agendado; Consulta.FechaInicio es el horario real.
            // No se hereda el de la cita: el paciente puede attenderse antes de la hora.
            FechaInicio = dto.FechaInicio ?? DateTime.UtcNow,
            NotasClinicas = dto.NotasClinicas,
            FechaCreacion = DateTime.UtcNow
        };

        if (cita != null)
        {
            // Abrir la consulta significa que el paciente ya está siendo atendido.
            cita.Estado = EstadoCita.EnCurso;
        }

        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();

        return MapToDto(consulta, paciente.NombreCompleto, nutricionista.NombreCompleto);
    }

    public async Task<ConsultaRespuestaDto> CerrarConsultaAsync(Guid consultaId, CerrarConsultaDto dto)
    {
        await _cerrarConsultaValidator.ValidateAndThrowAsync(dto);

        var consulta = await _context.Consultas
            .Include(c => c.Cita)
            .FirstOrDefaultAsync(c => c.Id == consultaId)
            ?? throw new KeyNotFoundException($"No se encontró la consulta con ID: {consultaId}");

        if (consulta.FechaFin.HasValue)
        {
            throw new ConflictException($"La consulta {consultaId} ya fue cerrada el {consulta.FechaFin:yyyy-MM-dd}.");
        }

        var fechaFin = dto.FechaFin ?? DateTime.UtcNow;

        if (fechaFin < consulta.FechaInicio)
        {
            throw new ValidationException("La fecha de fin no puede ser anterior a la fecha de inicio de la consulta.");
        }

        consulta.FechaFin = fechaFin;

        if (!string.IsNullOrWhiteSpace(dto.NotasClinicas))
        {
            consulta.NotasClinicas = dto.NotasClinicas;
        }

        // Cerrar la consulta completa la cita que la originó.
        if (consulta.Cita != null)
        {
            consulta.Cita.Estado = EstadoCita.Completada;
        }

        await _context.SaveChangesAsync();

        var pacienteNombre = await _context.Pacientes
            .AsNoTracking()
            .Where(p => p.Id == consulta.PacienteId)
            .Select(p => p.NombreCompleto)
            .FirstAsync();

        var nutricionistaNombre = await _context.Nutricionistas
            .AsNoTracking()
            .Where(n => n.Id == consulta.NutricionistaId)
            .Select(n => n.NombreCompleto)
            .FirstAsync();

        return MapToDto(consulta, pacienteNombre, nutricionistaNombre);
    }

    public async Task<ConsultaRespuestaDto> ObtenerConsultaPorIdAsync(Guid consultaId)
    {
        var consulta = await _context.Consultas
            .Include(c => c.Paciente)
            .Include(c => c.Nutricionista)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == consultaId)
            ?? throw new KeyNotFoundException($"No se encontró la consulta con ID: {consultaId}");

        return MapToDto(consulta, consulta.Paciente.NombreCompleto, consulta.Nutricionista.NombreCompleto);
    }

    public async Task<IEnumerable<ConsultaRespuestaDto>> ObtenerConsultasAsync(ConsultaFiltroDto filtro)
    {
        var query = _context.Consultas.AsNoTracking().AsQueryable();

        if (filtro.PacienteId.HasValue)
        {
            query = query.Where(c => c.PacienteId == filtro.PacienteId.Value);
        }

        if (filtro.NutricionistaId.HasValue)
        {
            query = query.Where(c => c.NutricionistaId == filtro.NutricionistaId.Value);
        }

        if (filtro.CitaId.HasValue)
        {
            query = query.Where(c => c.CitaId == filtro.CitaId.Value);
        }

        if (filtro.TipoConsulta.HasValue)
        {
            query = query.Where(c => c.TipoConsulta == filtro.TipoConsulta.Value);
        }

        if (filtro.SoloSinCierre == true)
        {
            query = query.Where(c => c.FechaFin == null);
        }

        if (filtro.Desde.HasValue)
        {
            query = query.Where(c => c.FechaFin >= filtro.Desde.Value);
        }

        if (filtro.Hasta.HasValue)
        {
            query = query.Where(c => c.FechaInicio <= filtro.Hasta.Value);
        }

        var proyeccion = await (
            from c in query
            join p in _context.Pacientes.AsNoTracking() on c.PacienteId equals p.Id
            join n in _context.Nutricionistas.AsNoTracking() on c.NutricionistaId equals n.Id
            select new
            {
                Consulta = c,
                PacienteNombre = p.NombreCompleto,
                NutricionistaNombre = n.NombreCompleto
            })
            .OrderByDescending(x => x.Consulta.FechaInicio)
            .ToListAsync();

        return proyeccion
            .Select(x => MapToDto(x.Consulta, x.PacienteNombre, x.NutricionistaNombre))
            .ToList();
    }

    public async Task<IEnumerable<ConsultaRespuestaDto>> ObtenerConsultasPorPacienteAsync(Guid pacienteId)
    {
        var pacienteExiste = await _context.Pacientes
            .AsNoTracking()
            .AnyAsync(p => p.Id == pacienteId);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException($"No se encontró un paciente con ID: {pacienteId}");
        }

        return await ObtenerConsultasAsync(new ConsultaFiltroDto { PacienteId = pacienteId });
    }

    public async Task<ConsultaRespuestaDto?> ObtenerConsultaPorCitaAsync(Guid citaId)
    {
        return await ObtenerConsultasAsync(new ConsultaFiltroDto { CitaId = citaId }) is { } lista
            ? lista.FirstOrDefault()
            : null;
    }

    private static ConsultaRespuestaDto MapToDto(
        Consulta consulta,
        string pacienteNombre,
        string nutricionistaNombre) => new()
    {
        Id = consulta.Id,
        PacienteId = consulta.PacienteId,
        PacienteNombre = pacienteNombre,
        CitaId = consulta.CitaId,
        NutricionistaId = consulta.NutricionistaId,
        NutricionistaNombre = nutricionistaNombre,
        TipoConsulta = consulta.TipoConsulta,
        FechaInicio = consulta.FechaInicio,
        FechaFin = consulta.FechaFin,
        NotasClinicas = consulta.NotasClinicas,
        FechaCreacion = consulta.FechaCreacion
    };
}
