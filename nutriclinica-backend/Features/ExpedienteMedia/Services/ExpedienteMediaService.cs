using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.Consultas.Services;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;
using nutriclinica_backend.Infrastructure.Storage;

namespace nutriclinica_backend.Features.ExpedienteMedia.Services;

public class ExpedienteMediaService : IExpedienteMediaService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IValidator<CrearFotoDto> _crearFotoValidator;
    private readonly IValidator<ActualizarFotoDto> _actualizarFotoValidator;
    private readonly IValidator<CrearDocumentoDto> _crearDocumentoValidator;
    private readonly IValidator<ActualizarDocumentoDto> _actualizarDocumentoValidator;
    private readonly IValidator<SubirFotoDto> _subirFotoValidator;
    private readonly IValidator<SubirDocumentoDto> _subirDocumentoValidator;

    public ExpedienteMediaService(
        ApplicationDbContext context,
        IStorageService storageService,
        IValidator<CrearFotoDto> crearFotoValidator,
        IValidator<ActualizarFotoDto> actualizarFotoValidator,
        IValidator<CrearDocumentoDto> crearDocumentoValidator,
        IValidator<ActualizarDocumentoDto> actualizarDocumentoValidator,
        IValidator<SubirFotoDto> subirFotoValidator,
        IValidator<SubirDocumentoDto> subirDocumentoValidator)
    {
        _context = context;
        _storageService = storageService;
        _crearFotoValidator = crearFotoValidator;
        _actualizarFotoValidator = actualizarFotoValidator;
        _crearDocumentoValidator = crearDocumentoValidator;
        _actualizarDocumentoValidator = actualizarDocumentoValidator;
        _subirFotoValidator = subirFotoValidator;
        _subirDocumentoValidator = subirDocumentoValidator;
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

    public async Task<FotoRespuestaDto> SubirFotoAsync(Guid pacienteId, SubirFotoDto dto)
    {
        await _subirFotoValidator.ValidateAndThrowAsync(dto);
        await ValidarPertenenciaPacienteAsync(pacienteId, dto.ConsultaId);

        var url = await SubirContenidoAsync(pacienteId, "fotos", dto.Archivo);

        return await RegistrarFotoAsync(pacienteId, new CrearFotoDto
        {
            ConsultaId = dto.ConsultaId,
            UrlFoto = url,
            Tipo = dto.Tipo,
            Notas = dto.Notas
        });
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

    public async Task<DocumentoRespuestaDto> SubirDocumentoAsync(Guid pacienteId, SubirDocumentoDto dto)
    {
        await _subirDocumentoValidator.ValidateAndThrowAsync(dto);
        await ValidarPertenenciaPacienteAsync(pacienteId, dto.ConsultaId);

        var url = await SubirContenidoAsync(pacienteId, "documentos", dto.Archivo);

        return await RegistrarDocumentoAsync(pacienteId, new CrearDocumentoDto
        {
            ConsultaId = dto.ConsultaId,
            NombreDocumento = dto.Archivo.FileName,
            UrlDocumento = url,
            Tipo = dto.Tipo,
            Observaciones = dto.Observaciones
        });
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

    private async Task ValidarPertenenciaPacienteAsync(Guid pacienteId, Guid? consultaId)
    {
        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!pacienteExiste)
        {
            throw new KeyNotFoundException($"No se encontró un paciente activo con ID: {pacienteId}");
        }

        await ValidadorConsultaVinculo.ValidarPertenenciaAsync(_context, consultaId, pacienteId);
    }

    private async Task<string> SubirContenidoAsync(Guid pacienteId, string tipoRuta, IFormFile archivo)
    {
        await using var buffer = new MemoryStream();
        await archivo.CopyToAsync(buffer);

        var ruta = $"{pacienteId}/{tipoRuta}/{Guid.NewGuid()}{ExtensionDesdeContentType(archivo.ContentType)}";

        return await _storageService.SubirArchivoAsync(ruta, buffer.ToArray(), archivo.ContentType);
    }

    private static string ExtensionDesdeContentType(string contentType) => contentType.ToLower() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "application/pdf" => ".pdf",
        "application/msword" => ".doc",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
        _ => ".bin"
    };

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