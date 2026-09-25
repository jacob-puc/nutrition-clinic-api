using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
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

    public PacienteService(
        ApplicationDbContext context,
        IValidator<CrearPacienteDto> crearPacienteValidator,
        IValidator<ActualizarPacienteDto> actualizarPacienteValidator)
    {
        _context = context;
        _crearPacienteValidator = crearPacienteValidator;
        _actualizarPacienteValidator = actualizarPacienteValidator;
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
            Edad = CalcularEdad(dto.FechaNacimiento),
            IsActive = true
        };

        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();

        return new PacienteRespuestaDto
        {
            Id = paciente.Id,
            NombreCompleto = paciente.NombreCompleto,
            Direccion = paciente.Direccion,
            Telefono = paciente.Telefono,
            CorreoElectronico = paciente.CorreoElectronico,
            FechaNacimiento = paciente.FechaNacimiento,
            Edad = paciente.Edad,
            Sexo = paciente.Sexo,
            FechaRegistro = paciente.FechaRegistro
        };
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
        paciente.Edad = CalcularEdad(dto.FechaNacimiento);

        await _context.SaveChangesAsync();

        return new PacienteRespuestaDto
        {
            Id = paciente.Id,
            NombreCompleto = paciente.NombreCompleto,
            CorreoElectronico = paciente.CorreoElectronico,
            Telefono = paciente.Telefono,
            Direccion = paciente.Direccion,
            FechaNacimiento = paciente.FechaNacimiento,
            Edad = paciente.Edad,
            Sexo = paciente.Sexo
        };
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

        return new PacienteRespuestaDto
        {
            Id = paciente.Id,
            NombreCompleto = paciente.NombreCompleto,
            Direccion = paciente.Direccion,
            Telefono = paciente.Telefono,
            CorreoElectronico = paciente.CorreoElectronico,
            FechaNacimiento = paciente.FechaNacimiento,
            Edad = paciente.Edad,
            Sexo = paciente.Sexo,
            FechaRegistro = paciente.FechaRegistro
        };
    }

    public async Task<IEnumerable<PacienteRespuestaDto>> ObtenerPacientesAsync()
    {
        return await _context.Pacientes
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new PacienteRespuestaDto
            {
                Id = p.Id,
                NombreCompleto = p.NombreCompleto,
                Direccion = p.Direccion,
                Telefono = p.Telefono,
                CorreoElectronico = p.CorreoElectronico,
                FechaNacimiento = p.FechaNacimiento,
                Edad = p.Edad,
                Sexo = p.Sexo,
                FechaRegistro = p.FechaRegistro
            })
            .ToListAsync();
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
                CitaId = m.CitaId,
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
                CitaId = f.CitaId,
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
                CitaId = d.CitaId,
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
            Edad = paciente.Edad,
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

    private static int CalcularEdad(DateTime? fechaNacimiento)
    {
        if (!fechaNacimiento.HasValue) return 0;

        var hoy = DateTime.Today;
        var edad = hoy.Year - fechaNacimiento.Value.Year;
        if (fechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;

        return edad;
    }
}