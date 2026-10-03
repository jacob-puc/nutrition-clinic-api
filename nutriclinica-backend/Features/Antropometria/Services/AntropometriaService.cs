using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Antropometria.DTOs;
using nutriclinica_backend.Features.Antropometria.Interfaces;
using nutriclinica_backend.Features.Consultas.Services;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Antropometria.Services;

public class AntropometriaService : IAntropometriaService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearMedidaDto> _crearMedidaValidator;
    private readonly IValidator<ActualizarMedidaDto> _actualizarMedidaValidator;

    public AntropometriaService(
        ApplicationDbContext context,
        IValidator<CrearMedidaDto> crearMedidaValidator,
        IValidator<ActualizarMedidaDto> actualizarMedidaValidator)
    {
        _context = context;
        _crearMedidaValidator = crearMedidaValidator;
        _actualizarMedidaValidator = actualizarMedidaValidator;
    }

    public async Task<MedidaRespuestaDto> RegistrarMedidaAsync(Guid pacienteId, CrearMedidaDto dto)
    {
        await _crearMedidaValidator.ValidateAndThrowAsync(dto);

        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {pacienteId}");
        }

        await ValidadorConsultaVinculo.ValidarPertenenciaAsync(_context, dto.ConsultaId, pacienteId);

        var medida = new MedidaAntropometrica
        {
            PacienteId = pacienteId,
            ConsultaId = dto.ConsultaId,
            Peso = dto.Peso,
            Estatura = dto.Estatura,
            Imc = CalculadoraClinica.CalcularImc(dto.Peso, dto.Estatura),
            PorcentajeGrasa = dto.PorcentajeGrasa,
            PorcentajeMasaMuscular = dto.PorcentajeMasaMuscular,
            MedidaCintura = dto.MedidaCintura,
            MedidaCadera = dto.MedidaCadera,
            NotasObservaciones = dto.NotasObservaciones,
            FechaMedicion = DateTime.UtcNow
        };

        _context.MedidasAntropometricas.Add(medida);
        await _context.SaveChangesAsync();

        return MapToDto(medida);
    }

    public async Task<MedidaRespuestaDto> RegistrarMedidaEnConsultaAsync(Guid consultaId, CrearMedidaDto dto)
    {
        // El paciente se deriva de la consulta: el cliente nunca lo envia.
        var pacienteId = await ValidadorConsultaVinculo
            .ResolverPacienteDesdeConsultaAsync(_context, consultaId);

        var dtoConVinculo = new CrearMedidaDto
        {
            Peso = dto.Peso,
            Estatura = dto.Estatura,
            PorcentajeGrasa = dto.PorcentajeGrasa,
            PorcentajeMasaMuscular = dto.PorcentajeMasaMuscular,
            MedidaCintura = dto.MedidaCintura,
            MedidaCadera = dto.MedidaCadera,
            NotasObservaciones = dto.NotasObservaciones,
            ConsultaId = consultaId
        };

        return await RegistrarMedidaAsync(pacienteId, dtoConVinculo);
    }

    public async Task<IEnumerable<MedidaRespuestaDto>> ObtenerMedidasPorPacienteIdAsync(Guid pacienteId)
    {
        return await _context.MedidasAntropometricas
            .AsNoTracking()
            .Where(m => m.PacienteId == pacienteId)
            .OrderByDescending(m => m.FechaMedicion)
            .Select(m => new MedidaRespuestaDto
            {
                Id = m.Id,
                PacienteId = m.PacienteId,
                ConsultaId = m.ConsultaId,
                FechaMedicion = m.FechaMedicion,
                Peso = m.Peso,
                Estatura = m.Estatura,
                Imc = m.Imc,
                PorcentajeGrasa = m.PorcentajeGrasa,
                PorcentajeMasaMuscular = m.PorcentajeMasaMuscular,
                MedidaCintura = m.MedidaCintura,
                MedidaCadera = m.MedidaCadera,
                NotasObservaciones = m.NotasObservaciones
            })
            .ToListAsync();
    }

    public async Task<MedidaRespuestaDto> ObtenerMedidaPorIdAsync(Guid pacienteId, Guid medidaId)
    {
        var medida = await _context.MedidasAntropometricas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medidaId && m.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró la medición antropométrica");

        return MapToDto(medida);
    }

    public async Task<MedidaRespuestaDto> ActualizarMedidaAsync(Guid pacienteId, Guid medidaId, ActualizarMedidaDto dto)
    {
        await _actualizarMedidaValidator.ValidateAndThrowAsync(dto);

        var medida = await _context.MedidasAntropometricas
            .FirstOrDefaultAsync(m => m.Id == medidaId && m.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró la medición antropométrica");

        medida.Peso = dto.Peso;
        medida.Estatura = dto.Estatura;
        medida.PorcentajeGrasa = dto.PorcentajeGrasa;
        medida.PorcentajeMasaMuscular = dto.PorcentajeMasaMuscular;
        medida.MedidaCintura = dto.MedidaCintura;
        medida.MedidaCadera = dto.MedidaCadera;
        medida.NotasObservaciones = dto.NotasObservaciones;
        medida.Imc = CalculadoraClinica.CalcularImc(dto.Peso, dto.Estatura);

        await _context.SaveChangesAsync();

        return MapToDto(medida);
    }

    public async Task EliminarMedidaAsync(Guid pacienteId, Guid medidaId)
    {
        var medida = await _context.MedidasAntropometricas
            .FirstOrDefaultAsync(m => m.Id == medidaId && m.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró la medición antropométrica");

        _context.MedidasAntropometricas.Remove(medida);
        await _context.SaveChangesAsync();
    }

    private static MedidaRespuestaDto MapToDto(MedidaAntropometrica medida) => new()
    {
        Id = medida.Id,
        PacienteId = medida.PacienteId,
        ConsultaId = medida.ConsultaId,
        FechaMedicion = medida.FechaMedicion,
        Peso = medida.Peso,
        Estatura = medida.Estatura,
        Imc = medida.Imc,
        PorcentajeGrasa = medida.PorcentajeGrasa,
        PorcentajeMasaMuscular = medida.PorcentajeMasaMuscular,
        MedidaCintura = medida.MedidaCintura,
        MedidaCadera = medida.MedidaCadera,
        NotasObservaciones = medida.NotasObservaciones
    };
}