using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Features.Nutricionistas.DTOs;
using nutriclinica_backend.Features.Nutricionistas.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Nutricionistas.Services;

public class NutricionistaService : INutricionistaService
{
    private static readonly EstadoCita[] EstadosCitasBloqueantes =
    [
        EstadoCita.Programada,
        EstadoCita.Confirmada,
        EstadoCita.EnCurso
    ];

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearNutricionistaDto> _crearNutricionistaValidator;
    private readonly IValidator<ActualizarNutricionistaDto> _actualizarNutricionistaValidator;

    public NutricionistaService(
        ApplicationDbContext context,
        IValidator<CrearNutricionistaDto> crearNutricionistaValidator,
        IValidator<ActualizarNutricionistaDto> actualizarNutricionistaValidator)
    {
        _context = context;
        _crearNutricionistaValidator = crearNutricionistaValidator;
        _actualizarNutricionistaValidator = actualizarNutricionistaValidator;
    }

    public async Task<NutricionistaRespuestaDto> CrearNutricionistaAsync(CrearNutricionistaDto dto)
    {
        await _crearNutricionistaValidator.ValidateAndThrowAsync(dto);

        await ValidarCorreoDisponibleAsync(dto.CorreoElectronico, null);
        await ValidarColegiaturaDisponibleAsync(dto.NumeroColegiatura, null);

        var nutricionista = new Nutricionista
        {
            NombreCompleto = dto.NombreCompleto,
            CorreoElectronico = dto.CorreoElectronico,
            Telefono = dto.Telefono,
            NumeroColegiatura = dto.NumeroColegiatura,
            Especialidad = dto.Especialidad,
            IsActive = true
        };

        _context.Nutricionistas.Add(nutricionista);
        await _context.SaveChangesAsync();

        return MapToDto(nutricionista);
    }

    public async Task<NutricionistaRespuestaDto> ActualizarNutricionistaAsync(Guid id, ActualizarNutricionistaDto dto)
    {
        await _actualizarNutricionistaValidator.ValidateAndThrowAsync(dto);

        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un nutricionista activo con ID: {id}");

        await ValidarCorreoDisponibleAsync(dto.CorreoElectronico, id);
        await ValidarColegiaturaDisponibleAsync(dto.NumeroColegiatura, id);

        nutricionista.NombreCompleto = dto.NombreCompleto;
        nutricionista.CorreoElectronico = dto.CorreoElectronico;
        nutricionista.Telefono = dto.Telefono;
        nutricionista.NumeroColegiatura = dto.NumeroColegiatura;
        nutricionista.Especialidad = dto.Especialidad;

        await _context.SaveChangesAsync();

        return MapToDto(nutricionista);
    }

    public async Task<NutricionistaRespuestaDto> ObtenerNutricionistaPorIdAsync(Guid id)
    {
        var nutricionista = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un nutricionista activo con ID: {id}");

        return MapToDto(nutricionista);
    }

    public async Task<IEnumerable<NutricionistaRespuestaDto>> ObtenerNutricionistasAsync(bool incluirInactivos)
    {
        var query = _context.Nutricionistas.AsNoTracking();

        if (!incluirInactivos)
        {
            query = query.Where(n => n.IsActive);
        }

        var nutricionistas = await query.ToListAsync();

        return nutricionistas.Select(MapToDto).ToList();
    }

    public async Task EliminarNutricionistaAsync(Guid id)
    {
        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un nutricionista activo con ID: {id}");

        var tieneCitasActivas = await _context.Citas
            .AnyAsync(c => c.NutricionistaId == id && EstadosCitasBloqueantes.Contains(c.Estado));

        if (tieneCitasActivas)
        {
            throw new ValidationException(
                "No se puede desactivar al nutricionista porque tiene citas programadas, confirmadas o en curso.");
        }

        nutricionista.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task ValidarCorreoDisponibleAsync(string correo, Guid? excluirId)
    {
        var normalizado = correo.ToLower();

        var yaExiste = await _context.Nutricionistas
            .AnyAsync(n => n.IsActive
                           && n.CorreoElectronico.ToLower() == normalizado
                           && (!excluirId.HasValue || n.Id != excluirId.Value));

        if (yaExiste)
        {
            throw new ValidationException("Ya existe un nutricionista activo registrado con ese correo electrónico.");
        }
    }

    private async Task ValidarColegiaturaDisponibleAsync(string? numeroColegiatura, Guid? excluirId)
    {
        if (string.IsNullOrWhiteSpace(numeroColegiatura)) return;

        var yaExiste = await _context.Nutricionistas
            .AnyAsync(n => n.NumeroColegiatura == numeroColegiatura
                           && (!excluirId.HasValue || n.Id != excluirId.Value));

        if (yaExiste)
        {
            throw new ValidationException("Ya existe un nutricionista registrado con ese número de colegiatura.");
        }
    }

    private static NutricionistaRespuestaDto MapToDto(Nutricionista nutricionista) => new()
    {
        Id = nutricionista.Id,
        NombreCompleto = nutricionista.NombreCompleto,
        CorreoElectronico = nutricionista.CorreoElectronico,
        Telefono = nutricionista.Telefono,
        NumeroColegiatura = nutricionista.NumeroColegiatura,
        Especialidad = nutricionista.Especialidad,
        IsActive = nutricionista.IsActive,
        FechaRegistro = nutricionista.FechaRegistro
    };
}
