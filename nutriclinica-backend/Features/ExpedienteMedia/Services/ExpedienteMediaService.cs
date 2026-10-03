using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.Consultas.Services;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.ExpedienteMedia.Services;

public class ExpedienteMediaService : IExpedienteMediaService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearFotoDto> _crearFotoValidator;
    private readonly IValidator<ActualizarFotoDto> _actualizarFotoValidator;
    private readonly IValidator<CrearDocumentoDto> _crearDocumentoValidator;
    private readonly IValidator<ActualizarDocumentoDto> _actualizarDocumentoValidator;

    public ExpedienteMediaService(
        ApplicationDbContext context,
        IValidator<CrearFotoDto> crearFotoValidator,
        IValidator<ActualizarFotoDto> actualizarFotoValidator,
        IValidator<CrearDocumentoDto> crearDocumentoValidator,
        IValidator<ActualizarDocumentoDto> actualizarDocumentoValidator)
    {
        _context = context;
        _crearFotoValidator = crearFotoValidator;
        _actualizarFotoValidator = actualizarFotoValidator;
        _crearDocumentoValidator = crearDocumentoValidator;
        _actualizarDocumentoValidator = actualizarDocumentoValidator;
    }

    public async Task<FotoRespuestaDto> RegistrarFotoAsync(Guid pacienteId, CrearFotoDto dto)
    {
        await _crearFotoValidator.ValidateAndThrowAsync(dto);

        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {pacienteId}");
        }

        await ValidadorConsultaVinculo.ValidarPertenenciaAsync(_context, dto.ConsultaId, pacienteId);

        var foto = new FotoSeguimiento
        {
            PacienteId = pacienteId,
            ConsultaId = dto.ConsultaId,
            UrlFoto = dto.UrlFoto,
            Tipo = dto.Tipo,
            Notas = dto.Notas,
            FechaSubida = DateTime.UtcNow
        };

        _context.FotosSeguimiento.Add(foto);
        await _context.SaveChangesAsync();

        return MapFotoToDto(foto);
    }

    public async Task<FotoRespuestaDto> RegistrarFotoEnConsultaAsync(Guid consultaId, CrearFotoDto dto)
    {
        var pacienteId = await ValidadorConsultaVinculo
            .ResolverPacienteDesdeConsultaAsync(_context, consultaId);

        var dtoConVinculo = new CrearFotoDto
        {
            UrlFoto = dto.UrlFoto,
            Tipo = dto.Tipo,
            Notas = dto.Notas,
            ConsultaId = consultaId
        };

        return await RegistrarFotoAsync(pacienteId, dtoConVinculo);
    }

    public async Task<IEnumerable<FotoRespuestaDto>> ObtenerFotosPorPacienteAsync(Guid pacienteId)
    {
        return await _context.FotosSeguimiento
            .AsNoTracking()
            .Where(f => f.PacienteId == pacienteId)
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
    }

    public async Task<FotoRespuestaDto> ObtenerFotoPorIdAsync(Guid pacienteId, Guid fotoId)
    {
        var foto = await _context.FotosSeguimiento
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró la foto de seguimiento");

        return MapFotoToDto(foto);
    }

    public async Task<FotoRespuestaDto> ActualizarFotoAsync(Guid pacienteId, Guid fotoId, ActualizarFotoDto dto)
    {
        await _actualizarFotoValidator.ValidateAndThrowAsync(dto);

        var foto = await _context.FotosSeguimiento
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró la foto de seguimiento");

        foto.UrlFoto = dto.UrlFoto;
        foto.Tipo = dto.Tipo;
        foto.Notas = dto.Notas;

        await _context.SaveChangesAsync();

        return MapFotoToDto(foto);
    }

    public async Task EliminarFotoAsync(Guid pacienteId, Guid fotoId)
    {
        var foto = await _context.FotosSeguimiento
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró la foto de seguimiento");

        _context.FotosSeguimiento.Remove(foto);
        await _context.SaveChangesAsync();
    }

    public async Task<DocumentoRespuestaDto> RegistrarDocumentoAsync(Guid pacienteId, CrearDocumentoDto dto)
    {
        await _crearDocumentoValidator.ValidateAndThrowAsync(dto);

        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {pacienteId}");
        }

        await ValidadorConsultaVinculo.ValidarPertenenciaAsync(_context, dto.ConsultaId, pacienteId);

        var documento = new DocumentoPaciente
        {
            PacienteId = pacienteId,
            ConsultaId = dto.ConsultaId,
            NombreDocumento = dto.NombreDocumento,
            UrlDocumento = dto.UrlDocumento,
            Tipo = dto.Tipo,
            Observaciones = dto.Observaciones,
            FechaSubida = DateTime.UtcNow
        };

        _context.DocumentosPaciente.Add(documento);
        await _context.SaveChangesAsync();

        return MapDocumentoToDto(documento);
    }

    public async Task<DocumentoRespuestaDto> RegistrarDocumentoEnConsultaAsync(Guid consultaId, CrearDocumentoDto dto)
    {
        var pacienteId = await ValidadorConsultaVinculo
            .ResolverPacienteDesdeConsultaAsync(_context, consultaId);

        var dtoConVinculo = new CrearDocumentoDto
        {
            NombreDocumento = dto.NombreDocumento,
            UrlDocumento = dto.UrlDocumento,
            Tipo = dto.Tipo,
            Observaciones = dto.Observaciones,
            ConsultaId = consultaId
        };

        return await RegistrarDocumentoAsync(pacienteId, dtoConVinculo);
    }

    public async Task<IEnumerable<DocumentoRespuestaDto>> ObtenerDocumentosPorPacienteAsync(Guid pacienteId)
    {
        return await _context.DocumentosPaciente
            .AsNoTracking()
            .Where(d => d.PacienteId == pacienteId)
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
    }

    public async Task<DocumentoRespuestaDto> ObtenerDocumentoPorIdAsync(Guid pacienteId, Guid documentoId)
    {
        var documento = await _context.DocumentosPaciente
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró el documento del paciente");

        return MapDocumentoToDto(documento);
    }

    public async Task<DocumentoRespuestaDto> ActualizarDocumentoAsync(Guid pacienteId, Guid documentoId, ActualizarDocumentoDto dto)
    {
        await _actualizarDocumentoValidator.ValidateAndThrowAsync(dto);

        var documento = await _context.DocumentosPaciente
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró el documento del paciente");

        documento.NombreDocumento = dto.NombreDocumento;
        documento.UrlDocumento = dto.UrlDocumento;
        documento.Tipo = dto.Tipo;
        documento.Observaciones = dto.Observaciones;

        await _context.SaveChangesAsync();

        return MapDocumentoToDto(documento);
    }

    public async Task EliminarDocumentoAsync(Guid pacienteId, Guid documentoId)
    {
        var documento = await _context.DocumentosPaciente
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.PacienteId == pacienteId)
            ?? throw new KeyNotFoundException("No se encontró el documento del paciente");

        _context.DocumentosPaciente.Remove(documento);
        await _context.SaveChangesAsync();
    }

    private static FotoRespuestaDto MapFotoToDto(FotoSeguimiento foto) => new()
    {
        Id = foto.Id,
        PacienteId = foto.PacienteId,
        ConsultaId = foto.ConsultaId,
        UrlFoto = foto.UrlFoto ?? string.Empty,
        Tipo = foto.Tipo,
        FechaSubida = foto.FechaSubida,
        Notas = foto.Notas
    };

    private static DocumentoRespuestaDto MapDocumentoToDto(DocumentoPaciente documento) => new()
    {
        Id = documento.Id,
        PacienteId = documento.PacienteId,
        ConsultaId = documento.ConsultaId,
        NombreDocumento = documento.NombreDocumento,
        UrlDocumento = documento.UrlDocumento,
        Tipo = documento.Tipo,
        FechaSubida = documento.FechaSubida,
        Observaciones = documento.Observaciones
    };
}