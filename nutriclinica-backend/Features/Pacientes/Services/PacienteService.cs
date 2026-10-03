using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Antropometria.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.HistorialesClinicos.DTOs;
using nutriclinica_backend.Features.Pacientes.DTOs;
using nutriclinica_backend.Features.Pacientes.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Pacientes.Services;

public class PacienteService : IPacienteService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearPacienteDto> _crearPacienteValidator;
    private readonly IValidator<ActualizarPacienteDto> _actualizarPacienteValidator;
    private readonly IValidator<ActualizarObjetivoPacienteDto> _actualizarObjetivoValidator;

    public PacienteService(
        ApplicationDbContext context,
        IValidator<CrearPacienteDto> crearPacienteValidator,
        IValidator<ActualizarPacienteDto> actualizarPacienteValidator,
        IValidator<ActualizarObjetivoPacienteDto> actualizarObjetivoValidator)
    {
        _context = context;
        _crearPacienteValidator = crearPacienteValidator;
        _actualizarPacienteValidator = actualizarPacienteValidator;
        _actualizarObjetivoValidator = actualizarObjetivoValidator;
    }

    public async Task<PacienteRespuestaDto> CrearPacienteAsync(CrearPacienteDto dto)
    {
        await _crearPacienteValidator.ValidateAndThrowAsync(dto);

        var paciente = new Paciente
        {
            NombreCompleto = dto.NombreCompleto,
            Direccion = dto.Direccion,
            Telefono = dto.Telefono,
            CorreoElectronico = dto.CorreoElectronico,
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            IsActive = true
        };

        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();

        return MapToDto(paciente);
    }

    public async Task<PacienteRespuestaDto> ActualizarPacienteAsync(Guid id, ActualizarPacienteDto dto)
    {
        await _actualizarPacienteValidator.ValidateAndThrowAsync(dto);

        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {id}");

        paciente.NombreCompleto = dto.NombreCompleto;
        paciente.CorreoElectronico = dto.CorreoElectronico;
        paciente.Telefono = dto.Telefono;
        paciente.Direccion = dto.Direccion;
        paciente.FechaNacimiento = dto.FechaNacimiento;
        paciente.Sexo = dto.Sexo;

        await _context.SaveChangesAsync();

        return MapToDto(paciente);
    }

    public async Task<PacienteRespuestaDto> ActualizarObjetivoAsync(
        Guid id,
        ActualizarObjetivoPacienteDto dto)
    {
        await _actualizarObjetivoValidator.ValidateAndThrowAsync(dto);

        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {id}");

        paciente.TituloObjetivo = dto.TituloObjetivo?.Trim();
        paciente.PesoObjetivo = dto.PesoObjetivo;

        await _context.SaveChangesAsync();

        return MapToDto(paciente);
    }

    public async Task EliminarPacienteAsync(Guid id)
    {
        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {id}");

        paciente.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task<PacienteRespuestaDto> ObtenerPacientePorIdAsync(Guid id)
    {
        var paciente = await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {id}");

        return MapToDto(paciente);
    }

    public async Task<IEnumerable<PacienteRespuestaDto>> ObtenerPacientesAsync()
    {
        var pacientes = await _context.Pacientes
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync();

        return pacientes.Select(MapToDto).ToList();
    }

    public async Task<ExpedienteCompletoDto> ObtenerExpedienteCompletoAsync(Guid id)
    {
        var paciente = await _context.Pacientes
            .Include(p => p.HistorialClinico)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {id}");

        var medidas = await _context.MedidasAntropometricas
            .AsNoTracking()
            .Where(m => m.PacienteId == id)
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

        var fotos = await _context.FotosSeguimiento
            .AsNoTracking()
            .Where(f => f.PacienteId == id)
            .OrderByDescending(f => f.FechaSubida)
            .Select(f => new FotoRespuestaDto
            {
                Id = f.Id,
                PacienteId = f.PacienteId,
                ConsultaId = f.ConsultaId,
                UrlFoto = f.UrlFoto ?? string.Empty,
                Tipo = f.Tipo,
                FechaSubida = f.FechaSubida,
                Notas = f.Notas
            })
            .ToListAsync();

        var documentos = await _context.DocumentosPaciente
            .AsNoTracking()
            .Where(d => d.PacienteId == id)
            .OrderByDescending(d => d.FechaSubida)
            .Select(d => new DocumentoRespuestaDto
            {
                Id = d.Id,
                PacienteId = d.PacienteId,
                ConsultaId = d.ConsultaId,
                NombreDocumento = d.NombreDocumento,
                UrlDocumento = d.UrlDocumento,
                Tipo = d.Tipo,
                FechaSubida = d.FechaSubida,
                Observaciones = d.Observaciones
            })
            .ToListAsync();

        return new ExpedienteCompletoDto
        {
            PacienteId = paciente.Id,
            NombreCompleto = paciente.NombreCompleto,
            CorreoElectronico = paciente.CorreoElectronico,
            Telefono = paciente.Telefono,
            Edad = CalculadoraClinica.CalcularEdad(paciente.FechaNacimiento),
            HistorialClinico = paciente.HistorialClinico == null
                ? null
                : new HistorialClinicoRespuestaDto
                {
                    Id = paciente.HistorialClinico.Id,
                    PacienteId = paciente.HistorialClinico.PacienteId,
                    Alergias = paciente.HistorialClinico.Alergias,
                    AlimentosFavoritos = paciente.HistorialClinico.AlimentosFavoritos,
                    AlimentosNoFavoritos = paciente.HistorialClinico.AlimentosNoFavoritos
                },
            MedidasAntropometricas = medidas,
            Fotos = fotos,
            Documentos = documentos
        };
    }

    private static PacienteRespuestaDto MapToDto(Paciente paciente) => new()
    {
        Id = paciente.Id,
        NombreCompleto = paciente.NombreCompleto,
        Direccion = paciente.Direccion,
        Telefono = paciente.Telefono,
        CorreoElectronico = paciente.CorreoElectronico,
        FechaNacimiento = paciente.FechaNacimiento,
        Edad = CalculadoraClinica.CalcularEdad(paciente.FechaNacimiento),
        Sexo = paciente.Sexo,
        TituloObjetivo = paciente.TituloObjetivo,
        PesoObjetivo = paciente.PesoObjetivo,
        FechaRegistro = paciente.FechaRegistro
    };
}
