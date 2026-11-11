using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.HistorialesClinicos.DTOs;
using nutriclinica_backend.Features.HistorialesClinicos.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.HistorialesClinicos.Services;

public class HistorialClinicoService : IHistorialClinicoService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearHistorialClinicoDto> _crearHistorialValidator;

    public HistorialClinicoService(
        ApplicationDbContext context,
        IValidator<CrearHistorialClinicoDto> crearHistorialValidator)
    {
        _context = context;
        _crearHistorialValidator = crearHistorialValidator;
    }

    public async Task<HistorialClinicoRespuestaDto> ObtenerHistorialClinicoPorPacienteIdAsync(Guid pacienteId)
    {
        var historial = await _context.HistorialesClinicos
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("El paciente no tiene historial clínico registrado");

        return MapToDto(historial);
    }

    public async Task<HistorialClinicoRespuestaDto> UpsertHistorialClinicoAsync(Guid pacienteId, CrearHistorialClinicoDto dto)
    {
        await _crearHistorialValidator.ValidateAndThrowAsync(dto);

        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException("No encontramos a ese paciente. Puede que se haya eliminado.");
        }

        var historial = await _context.HistorialesClinicos
            .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

        if (historial == null)
        {
            historial = new HistorialClinico { PacienteId = pacienteId };
            _context.HistorialesClinicos.Add(historial);
        }

        historial.Enfermedades = Limpiar(dto.Enfermedades);
        historial.Alergias = Limpiar(dto.Alergias);
        historial.AlimentosFavoritos = Limpiar(dto.AlimentosFavoritos);
        historial.AlimentosNoFavoritos = Limpiar(dto.AlimentosNoFavoritos);

        await _context.SaveChangesAsync();

        return MapToDto(historial);
    }

    /// <summary>Sin espacios sobrantes ni repetidos ("Nueces" y "nueces " son lo mismo).</summary>
    private static List<string> Limpiar(IEnumerable<string> valores) =>
        valores
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .DistinctBy(v => v.ToLowerInvariant())
            .ToList();

    private static HistorialClinicoRespuestaDto MapToDto(HistorialClinico historial) => new()
    {
        Id = historial.Id,
        PacienteId = historial.PacienteId,
        Enfermedades = historial.Enfermedades,
        Alergias = historial.Alergias,
        AlimentosFavoritos = historial.AlimentosFavoritos,
        AlimentosNoFavoritos = historial.AlimentosNoFavoritos
    };
}