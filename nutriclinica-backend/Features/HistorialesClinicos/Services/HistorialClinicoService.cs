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

        return new HistorialClinicoRespuestaDto
        {
            Id = historial.Id,
            PacienteId = historial.PacienteId,
            Alergias = historial.Alergias,
            AlimentosFavoritos = historial.AlimentosFavoritos,
            AlimentosNoFavoritos = historial.AlimentosNoFavoritos
        };
    }

    public async Task<HistorialClinicoRespuestaDto> UpsertHistorialClinicoAsync(Guid pacienteId, CrearHistorialClinicoDto dto)
    {
        await _crearHistorialValidator.ValidateAndThrowAsync(dto);

        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException($"No se encontró un paciente activo con el ID proporcionado: {pacienteId}");
        }

        var historial = await _context.HistorialesClinicos
            .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

        if (historial == null)
        {
            historial = new HistorialClinico
            {
                PacienteId = pacienteId,
                Alergias = dto.Alergias,
                AlimentosFavoritos = dto.AlimentosFavoritos,
                AlimentosNoFavoritos = dto.AlimentosNoFavoritos
            };
            _context.HistorialesClinicos.Add(historial);
        }
        else
        {
            historial.Alergias = dto.Alergias;
            historial.AlimentosFavoritos = dto.AlimentosFavoritos;
            historial.AlimentosNoFavoritos = dto.AlimentosNoFavoritos;
        }

        await _context.SaveChangesAsync();

        return new HistorialClinicoRespuestaDto
        {
            Id = historial.Id,
            PacienteId = historial.PacienteId,
            Alergias = historial.Alergias,
            AlimentosFavoritos = historial.AlimentosFavoritos,
            AlimentosNoFavoritos = historial.AlimentosNoFavoritos
        };
    }
}