using System.Globalization;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Alimentos.DTOs;
using nutriclinica_backend.Features.Alimentos.DTOs.Validators;
using nutriclinica_backend.Features.Alimentos.Interfaces;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs.Validators;
using nutriclinica_backend.Infrastructure.Persistence;
using nutriclinica_backend.Infrastructure.Storage;

namespace nutriclinica_backend.Features.Alimentos.Services;

public class AlimentoService : IAlimentoService
{
    private const int CaloriasPorGramoDeProteina = 4;
    private const int CaloriasPorGramoDeCarbohidrato = 4;
    private const int CaloriasPorGramoDeGrasa = 9;
    private const decimal ToleranciaCalorica = 5m;

    private static readonly string[] TiposImagenPermitidos =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly StorageConfig _storageConfig;
    private readonly IValidator<CrearAlimentoDto> _crearAlimentoValidator;
    private readonly IValidator<ActualizarAlimentoDto> _actualizarAlimentoValidator;
    private readonly IValidator<AlimentoFiltroDto> _filtroValidator;

    public AlimentoService(
        ApplicationDbContext context,
        IStorageService storageService,
        StorageConfig storageConfig,
        IValidator<CrearAlimentoDto> crearAlimentoValidator,
        IValidator<ActualizarAlimentoDto> actualizarAlimentoValidator,
        IValidator<AlimentoFiltroDto> filtroValidator)
    {
        _context = context;
        _storageService = storageService;
        _storageConfig = storageConfig;
        _crearAlimentoValidator = crearAlimentoValidator;
        _actualizarAlimentoValidator = actualizarAlimentoValidator;
        _filtroValidator = filtroValidator;
    }

    public async Task<AlimentoRespuestaDto> CrearAlimentoAsync(CrearAlimentoDto dto)
    {
        await _crearAlimentoValidator.ValidateAndThrowAsync(dto);

        var nombreNormalizado = NormalizarNombre(dto.Nombre);

        await ValidarNombreNoDuplicadoAsync(nombreNormalizado, null);

        var alimento = new Alimento
        {
            Id = Guid.NewGuid(),
            Nombre = dto.Nombre.Trim(),
            NombreNormalizado = nombreNormalizado,
            Calorias = dto.Calorias,
            Proteinas = dto.Proteinas,
            Carbohidratos = dto.Carbohidratos,
            Grasas = dto.Grasas,
            ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim(),
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        _context.Alimentos.Add(alimento);
        await _context.SaveChangesAsync();

        return MapToDto(alimento);
    }

    public async Task<AlimentoRespuestaDto> ActualizarAlimentoAsync(
        Guid alimentoId,
        ActualizarAlimentoDto dto)
    {
        await _actualizarAlimentoValidator.ValidateAndThrowAsync(dto);

        var alimento = await _context.Alimentos
            .FirstOrDefaultAsync(a => a.Id == alimentoId)
            ?? throw new KeyNotFoundException($"No se encontró un alimento con ID: {alimentoId}");

        var nombreNormalizado = NormalizarNombre(dto.Nombre);

        await ValidarNombreNoDuplicadoAsync(nombreNormalizado, alimentoId);

        alimento.Nombre = dto.Nombre.Trim();
        alimento.NombreNormalizado = nombreNormalizado;
        alimento.Calorias = dto.Calorias;
        alimento.Proteinas = dto.Proteinas;
        alimento.Carbohidratos = dto.Carbohidratos;
        alimento.Grasas = dto.Grasas;
        alimento.ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim();

        await _context.SaveChangesAsync();

        return MapToDto(alimento);
    }

    public async Task<AlimentoRespuestaDto> ObtenerAlimentoPorIdAsync(Guid alimentoId)
    {
        var alimento = await _context.Alimentos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alimentoId)
            ?? throw new KeyNotFoundException($"No se encontró un alimento con ID: {alimentoId}");

        return MapToDto(alimento);
    }

    public async Task<IEnumerable<AlimentoRespuestaDto>> ObtenerAlimentosAsync(AlimentoFiltroDto filtro)
    {
        await _filtroValidator.ValidateAndThrowAsync(filtro);

        var query = _context.Alimentos.AsNoTracking().AsQueryable();

        if (filtro.SoloActivos ?? true)
        {
            query = query.Where(a => a.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var termino = NormalizarNombre(filtro.Buscar);
            query = query.Where(a => a.NombreNormalizado.Contains(termino));
        }

        if (filtro.CaloriasMinimas.HasValue)
        {
            query = query.Where(a => a.Calorias >= filtro.CaloriasMinimas.Value);
        }

        if (filtro.CaloriasMaximas.HasValue)
        {
            query = query.Where(a => a.Calorias <= filtro.CaloriasMaximas.Value);
        }

        var ordenado = AplicarOrden(query, filtro.OrdenarPor, filtro.Descendente);

        var alimentos = await ordenado.ToListAsync();

        return alimentos.Select(MapToDto).ToList();
    }

    public async Task<AlimentoRespuestaDto> RestaurarAlimentoAsync(Guid alimentoId)
    {
        var alimento = await _context.Alimentos
            .FirstOrDefaultAsync(a => a.Id == alimentoId)
            ?? throw new KeyNotFoundException($"No se encontró un alimento con ID: {alimentoId}");

        if (alimento.IsActive)
        {
            throw new ConflictException("El alimento ya está activo.");
        }

        await ValidarNombreNoDuplicadoAsync(alimento.NombreNormalizado, alimentoId);

        alimento.IsActive = true;
        await _context.SaveChangesAsync();

        return MapToDto(alimento);
    }

    public async Task<AlimentoRespuestaDto> SubirImagenAlimentoAsync(Guid alimentoId, IFormFile imagen)
    {
        ValidarImagen(imagen);

        var alimento = await _context.Alimentos
            .FirstOrDefaultAsync(a => a.Id == alimentoId)
            ?? throw new KeyNotFoundException($"No se encontró un alimento con ID: {alimentoId}");

        await using var buffer = new MemoryStream();
        await imagen.CopyToAsync(buffer);

        var extension = imagen.ContentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var ruta = $"alimentos/{alimentoId}/{Guid.NewGuid()}{extension}";

        await _storageService.SubirArchivoAsync(
            _storageConfig.BucketMedia,
            ruta,
            buffer.ToArray(),
            imagen.ContentType);

        alimento.ImagenUrl = _storageService.GenerarUrlPublica(_storageConfig.BucketMedia, ruta);

        await _context.SaveChangesAsync();

        return MapToDto(alimento);
    }

    public async Task EliminarAlimentoAsync(Guid alimentoId)
    {
        var alimento = await _context.Alimentos
            .FirstOrDefaultAsync(a => a.Id == alimentoId)
            ?? throw new KeyNotFoundException($"No se encontró un alimento con ID: {alimentoId}");

        if (!alimento.IsActive)
        {
            throw new ConflictException("El alimento ya está eliminado.");
        }

        alimento.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static void ValidarImagen(IFormFile imagen)
    {
        if (imagen is null || imagen.Length == 0)
        {
            throw new ValidationException("Debes adjuntar una imagen.");
        }

        if (!TiposImagenPermitidos.Contains(imagen.ContentType.ToLowerInvariant()))
        {
            throw new ValidationException("Solo se permiten imágenes JPEG, PNG o WEBP.");
        }

        if (imagen.Length > ValidacionArchivo.MaximoTamanioFotoBytes)
        {
            throw new ValidationException(
                $"La imagen no puede superar {ValidacionArchivo.TextoTamanioFoto}.");
        }
    }

    private async Task ValidarNombreNoDuplicadoAsync(string nombreNormalizado, Guid? alimentoExcluido)
    {
        var existe = await _context.Alimentos
            .AnyAsync(a => a.IsActive
                           && a.NombreNormalizado == nombreNormalizado
                           && (!alimentoExcluido.HasValue || a.Id != alimentoExcluido.Value));

        if (existe)
        {
            throw new ConflictException("Ya existe un alimento activo con ese nombre.");
        }
    }

    private static IOrderedQueryable<Alimento> AplicarOrden(
        IQueryable<Alimento> query,
        string? ordenarPor,
        bool descendente)
    {
        var campo = ordenarPor?.ToLowerInvariant() ?? "nombre";

        Func<IQueryable<Alimento>, IOrderedQueryable<Alimento>> ordenar =
            campo switch
            {
                "calorias" => q => q.OrderBy(a => a.Calorias),
                "proteinas" => q => q.OrderBy(a => a.Proteinas),
                "carbohidratos" => q => q.OrderBy(a => a.Carbohidratos),
                "grasas" => q => q.OrderBy(a => a.Grasas),
                "fechacreacion" => q => q.OrderBy(a => a.FechaCreacion),
                _ => q => q.OrderBy(a => a.NombreNormalizado)
            };

        var resultado = ordenar(query);

        return descendente ? Descender(resultado, campo) : resultado;
    }

    private static IOrderedQueryable<Alimento> Descender(
        IOrderedQueryable<Alimento> ordenado,
        string campo) => campo switch
        {
            "calorias" => ordenado.OrderByDescending(a => a.Calorias),
            "proteinas" => ordenado.OrderByDescending(a => a.Proteinas),
            "carbohidratos" => ordenado.OrderByDescending(a => a.Carbohidratos),
            "grasas" => ordenado.OrderByDescending(a => a.Grasas),
            "fechacreacion" => ordenado.OrderByDescending(a => a.FechaCreacion),
            _ => ordenado.OrderByDescending(a => a.NombreNormalizado)
        };

    private static string NormalizarNombre(string nombre)
    {
        var descompuesto = nombre.Trim().Normalize(NormalizationForm.FormD);

        var limpio = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);

            if (categoria == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            limpio.Append(caracter);
        }

        return limpio
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    private static AlimentoRespuestaDto MapToDto(Alimento alimento)
    {
        var caloriasEstimadas =
            (alimento.Proteinas * CaloriasPorGramoDeProteina)
            + (alimento.Carbohidratos * CaloriasPorGramoDeCarbohidrato)
            + (alimento.Grasas * CaloriasPorGramoDeGrasa);

        return new AlimentoRespuestaDto
        {
            Id = alimento.Id,
            Nombre = alimento.Nombre,
            Calorias = alimento.Calorias,
            Proteinas = alimento.Proteinas,
            Carbohidratos = alimento.Carbohidratos,
            Grasas = alimento.Grasas,
            ImagenUrl = alimento.ImagenUrl,
            FechaCreacion = alimento.FechaCreacion,
            IsActive = alimento.IsActive,
            CaloriasEstimadas = decimal.Round(caloriasEstimadas, 2),
            DiferenciaCalorica = decimal.Round(alimento.Calorias - caloriasEstimadas, 2)
        };
    }

    public static bool EsInconsistente(AlimentoRespuestaDto alimento) =>
        Math.Abs(alimento.DiferenciaCalorica) > ToleranciaCalorica;
}
