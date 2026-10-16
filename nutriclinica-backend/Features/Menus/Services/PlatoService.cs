using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Menus.Services;

public class PlatoService : IPlatoService
{
    private const decimal GramosDeReferencia = 100m;

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearPlatoDto> _crearPlatoValidator;
    private readonly IValidator<ActualizarPlatoDto> _actualizarPlatoValidator;
    private readonly IValidator<PlatoFiltroDto> _filtroValidator;

    public PlatoService(
        ApplicationDbContext context,
        IValidator<CrearPlatoDto> crearPlatoValidator,
        IValidator<ActualizarPlatoDto> actualizarPlatoValidator,
        IValidator<PlatoFiltroDto> filtroValidator)
    {
        _context = context;
        _crearPlatoValidator = crearPlatoValidator;
        _actualizarPlatoValidator = actualizarPlatoValidator;
        _filtroValidator = filtroValidator;
    }

    public async Task<PlatoRespuestaDto> CrearPlatoAsync(CrearPlatoDto dto)
    {
        await _crearPlatoValidator.ValidateAndThrowAsync(dto);

        await ValidarNutricionistaAsync(dto.NutricionistaId);
        var alimentos = await CargarAlimentosAsync(dto.Detalles.Select(d => d.AlimentoId));

        var plato = new Plato
        {
            Id = Guid.NewGuid(),
            Nombre = dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim(),
            NutricionistaId = dto.NutricionistaId,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        var detalles = dto.Detalles
            .OrderBy(d => d.Orden)
            .Select(d => new DetallePlato
            {
                Id = Guid.NewGuid(),
                PlatoId = plato.Id,
                AlimentoId = d.AlimentoId,
                RacionGramos = d.RacionGramos,
                Orden = d.Orden
            })
            .ToList();

        plato.Detalles = detalles;

        _context.Platos.Add(plato);
        await _context.SaveChangesAsync();

        return await MapearPlatoAsync(plato, detalles, alimentos);
    }

    public async Task<PlatoRespuestaDto> ActualizarPlatoAsync(Guid platoId, ActualizarPlatoDto dto)
    {
        await _actualizarPlatoValidator.ValidateAndThrowAsync(dto);

        var plato = await _context.Platos
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == platoId)
            ?? throw new KeyNotFoundException($"No se encontró un plato con ID: {platoId}");

        await ValidarNutricionistaAsync(plato.NutricionistaId);
        var alimentos = await CargarAlimentosAsync(dto.Detalles.Select(d => d.AlimentoId));

        plato.Nombre = dto.Nombre.Trim();
        plato.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        plato.ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim();

        var existentes = plato.Detalles.ToDictionary(d => d.Id);
        var conservados = new List<DetallePlato>();
        var orden = 0;

        foreach (var detalleDto in dto.Detalles.OrderBy(d => d.Orden))
        {
            var existente = detalleDto.Id.HasValue
                ? existentes.GetValueOrDefault(detalleDto.Id.Value)
                : null;

            if (existente is not null)
            {
                existente.AlimentoId = detalleDto.AlimentoId;
                existente.RacionGramos = detalleDto.RacionGramos;
                existente.Orden = orden++;
                conservados.Add(existente);
            }
            else
            {
                conservados.Add(new DetallePlato
                {
                    Id = Guid.NewGuid(),
                    PlatoId = plato.Id,
                    AlimentoId = detalleDto.AlimentoId,
                    RacionGramos = detalleDto.RacionGramos,
                    Orden = orden++
                });
            }
        }

        var eliminados = plato.Detalles.Where(d => !conservados.Contains(d)).ToList();

        _context.DetallesPlato.RemoveRange(eliminados);
        _context.DetallesPlato.AddRange(conservados.Where(c => !existentes.ContainsKey(c.Id)));

        await _context.SaveChangesAsync();

        return await MapearPlatoAsync(plato, conservados, alimentos);
    }

    public async Task<PlatoRespuestaDto> ObtenerPlatoPorIdAsync(Guid platoId)
    {
        var plato = await _context.Platos
            .AsNoTracking()
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == platoId)
            ?? throw new KeyNotFoundException($"No se encontró un plato con ID: {platoId}");

        var detalles = plato.Detalles.OrderBy(d => d.Orden).ToList();
        var alimentos = await CargarAlimentosAsync(detalles.Select(d => d.AlimentoId));

        return await MapearPlatoAsync(plato, detalles, alimentos);
    }

    public async Task<IEnumerable<PlatoRespuestaDto>> ObtenerPlatosAsync(PlatoFiltroDto filtro)
    {
        await _filtroValidator.ValidateAndThrowAsync(filtro);

        var query = _context.Platos
            .AsNoTracking()
            .Include(p => p.Detalles)
            .AsQueryable();

        if (filtro.SoloActivos ?? true)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var termino = filtro.Buscar.Trim().ToLowerInvariant();
            query = query.Where(p => p.Nombre.ToLower().Contains(termino));
        }

        if (filtro.NutricionistaId.HasValue)
        {
            query = query.Where(p => p.NutricionistaId == filtro.NutricionistaId.Value);
        }

        if (filtro.SoloCompuestos.HasValue)
        {
            query = filtro.SoloCompuestos.Value
                ? query.Where(p => p.Detalles.Count() > 1)
                : query.Where(p => p.Detalles.Count() == 1);
        }

        var platos = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "calorias" => await OrdenarPorCaloriasAsync(query, filtro.Descendente),
            "fechacreacion" => await (filtro.Descendente
                ? query.OrderByDescending(p => p.FechaCreacion).ToListAsync()
                : query.OrderBy(p => p.FechaCreacion).ToListAsync()),
            _ => await (filtro.Descendente
                ? query.OrderByDescending(p => p.Nombre).ToListAsync()
                : query.OrderBy(p => p.Nombre).ToListAsync())
        };

        return await MapearPlatosAsync(platos);
    }

    public async Task<PlatoRespuestaDto> DuplicarPlatoAsync(Guid platoId, string? nombreNuevo)
    {
        var original = await _context.Platos
            .AsNoTracking()
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == platoId)
            ?? throw new KeyNotFoundException($"No se encontró un plato con ID: {platoId}");

        var copia = new Plato
        {
            Id = Guid.NewGuid(),
            Nombre = nombreNuevo?.Trim() ?? $"{original.Nombre} (copia)",
            Descripcion = original.Descripcion,
            ImagenUrl = original.ImagenUrl,
            NutricionistaId = original.NutricionistaId,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        copia.Detalles = original.Detalles
            .OrderBy(d => d.Orden)
            .Select(d => new DetallePlato
            {
                Id = Guid.NewGuid(),
                PlatoId = copia.Id,
                AlimentoId = d.AlimentoId,
                RacionGramos = d.RacionGramos,
                Orden = d.Orden
            })
            .ToList();

        _context.Platos.Add(copia);
        await _context.SaveChangesAsync();

        return await ObtenerPlatoPorIdAsync(copia.Id);
    }

    public async Task RestaurarPlatoAsync(Guid platoId)
    {
        var plato = await _context.Platos
            .FirstOrDefaultAsync(p => p.Id == platoId)
            ?? throw new KeyNotFoundException($"No se encontró un plato con ID: {platoId}");

        if (plato.IsActive)
        {
            throw new ConflictException("El plato ya está activo.");
        }

        plato.IsActive = true;
        await _context.SaveChangesAsync();
    }

    public async Task EliminarPlatoAsync(Guid platoId)
    {
        var plato = await _context.Platos
            .FirstOrDefaultAsync(p => p.Id == platoId)
            ?? throw new KeyNotFoundException($"No se encontró un plato con ID: {platoId}");

        if (!plato.IsActive)
        {
            throw new ConflictException("El plato ya está eliminado.");
        }

        var enPlantilla = await _context.DetallesPlantillaMenu
            .AnyAsync(d => d.PlatoId == platoId);

        if (enPlantilla)
        {
            throw new ConflictException(
                "No se puede eliminar el plato porque está asignado a una plantilla de menú. "
                + "Quítalo de la plantilla primero.");
        }

        var enPlanDePaciente = await _context.DetallesMenuAsignado
            .AnyAsync(d => d.PlatoId == platoId && d.MenuAsignado.IsActive);

        if (enPlanDePaciente)
        {
            throw new ConflictException(
                "No se puede eliminar el plato porque está en el plan activo de un paciente. "
                + "Quítalo del plan primero.");
        }

        plato.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task ValidarNutricionistaAsync(Guid? nutricionistaId)
    {
        if (!nutricionistaId.HasValue)
        {
            return;
        }

        var existe = await _context.Nutricionistas
            .AnyAsync(n => n.Id == nutricionistaId.Value && n.IsActive);

        if (!existe)
        {
            throw new KeyNotFoundException(
                $"No se encontró un nutricionista activo con ID: {nutricionistaId}");
        }
    }

    private async Task<Dictionary<Guid, Alimento>> CargarAlimentosAsync(IEnumerable<Guid> ids)
    {
        var lista = ids.Distinct().ToList();

        var alimentos = await _context.Alimentos
            .AsNoTracking()
            .Where(a => lista.Contains(a.Id) && a.IsActive)
            .ToDictionaryAsync(a => a.Id);

        var faltantes = lista.Except(alimentos.Keys).ToList();

        if (faltantes.Any())
        {
            throw new KeyNotFoundException(
                $"No se encontraron los siguientes alimentos activos: {string.Join(", ", faltantes)}");
        }

        return alimentos;
    }

    private async Task<List<Plato>> OrdenarPorCaloriasAsync(IQueryable<Plato> query, bool descendente)
    {
        var idsCalorias = await _context.DetallesPlato
            .AsNoTracking()
            .GroupBy(d => d.PlatoId)
            .Select(g => new
            {
                PlatoId = g.Key,
                Calorias = g.Sum(d => d.Alimento.Calorias * d.RacionGramos / GramosDeReferencia)
            })
            .ToDictionaryAsync(x => x.PlatoId, x => x.Calorias);

        var platos = await query.ToListAsync();

        return descendente
            ? platos.OrderByDescending(p => idsCalorias.GetValueOrDefault(p.Id)).ToList()
            : platos.OrderBy(p => idsCalorias.GetValueOrDefault(p.Id)).ToList();
    }

    private async Task<List<PlatoRespuestaDto>> MapearPlatosAsync(List<Plato> platos)
    {
        var todosLosIds = platos.SelectMany(p => p.Detalles).Select(d => d.AlimentoId).Distinct();
        var alimentos = await CargarAlimentosAsync(todosLosIds);

        return platos
            .Select(p => MapearPlato(p, p.Detalles.OrderBy(d => d.Orden).ToList(), alimentos))
            .ToList();
    }

    private Task<PlatoRespuestaDto> MapearPlatoAsync(
        Plato plato,
        List<DetallePlato> detalles,
        Dictionary<Guid, Alimento> alimentos)
    {
        return Task.FromResult(MapearPlato(plato, detalles, alimentos));
    }

    private static PlatoRespuestaDto MapearPlato(
        Plato plato,
        List<DetallePlato> detalles,
        Dictionary<Guid, Alimento> alimentos)
    {
        var detallesDto = detalles
            .OrderBy(d => d.Orden)
            .Select(d =>
            {
                var alimento = alimentos[d.AlimentoId];
                var factor = d.RacionGramos / GramosDeReferencia;

                return new DetallePlatoRespuestaDto
                {
                    Id = d.Id,
                    AlimentoId = d.AlimentoId,
                    AlimentoNombre = alimento.Nombre,
                    RacionGramos = d.RacionGramos,
                    Orden = d.Orden,
                    Calorias = decimal.Round(alimento.Calorias * factor, 2),
                    Proteinas = decimal.Round(alimento.Proteinas * factor, 2),
                    Carbohidratos = decimal.Round(alimento.Carbohidratos * factor, 2),
                    Grasas = decimal.Round(alimento.Grasas * factor, 2)
                };
            })
            .ToList();

        return new PlatoRespuestaDto
        {
            Id = plato.Id,
            Nombre = plato.Nombre,
            Descripcion = plato.Descripcion,
            ImagenUrl = plato.ImagenUrl,
            NutricionistaId = plato.NutricionistaId,
            EsCompuesto = detallesDto.Count > 1,
            Detalles = detallesDto,
            CaloriasTotales = decimal.Round(detallesDto.Sum(d => d.Calorias), 2),
            ProteinasTotales = decimal.Round(detallesDto.Sum(d => d.Proteinas), 2),
            CarbohidratosTotales = decimal.Round(detallesDto.Sum(d => d.Carbohidratos), 2),
            GrasasTotales = decimal.Round(detallesDto.Sum(d => d.Grasas), 2),
            FechaCreacion = plato.FechaCreacion,
            IsActive = plato.IsActive
        };
    }
}
