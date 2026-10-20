using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Menus.Services;

public class PlantillaMenuService : IPlantillaMenuService
{
    private const decimal GramosDeReferencia = 100m;

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearPlantillaMenuDto> _crearValidator;
    private readonly IValidator<ActualizarPlantillaMenuDto> _actualizarValidator;
    private readonly IValidator<PlantillaMenuFiltroDto> _filtroValidator;

    public PlantillaMenuService(
        ApplicationDbContext context,
        IValidator<CrearPlantillaMenuDto> crearValidator,
        IValidator<ActualizarPlantillaMenuDto> actualizarValidator,
        IValidator<PlantillaMenuFiltroDto> filtroValidator)
    {
        _context = context;
        _crearValidator = crearValidator;
        _actualizarValidator = actualizarValidator;
        _filtroValidator = filtroValidator;
    }

    public async Task<PlantillaMenuRespuestaDto> CrearPlantillaAsync(CrearPlantillaMenuDto dto)
    {
        await _crearValidator.ValidateAndThrowAsync(dto);

        await ValidarNutricionistaAsync(dto.NutricionistaId);
        await ValidarPlatosAsync(dto.Detalles.Select(d => d.PlatoId));
        ValidarDuplicadosDeComida(dto.Detalles);

        var plantilla = new PlantillaMenu
        {
            Id = Guid.NewGuid(),
            Nombre = dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            ObjetivoCalorico = dto.ObjetivoCalorico,
            NutricionistaId = dto.NutricionistaId,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        var detalles = dto.Detalles
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .Select(d => new DetallePlantillaMenu
            {
                Id = Guid.NewGuid(),
                PlantillaMenuId = plantilla.Id,
                PlatoId = d.PlatoId,
                DiaSemana = d.DiaSemana,
                TiempoComida = d.TiempoComida,
                Cantidad = d.Cantidad,
                Indicaciones = string.IsNullOrWhiteSpace(d.Indicaciones) ? null : d.Indicaciones.Trim(),
                Orden = d.Orden
            })
            .ToList();

        plantilla.Detalles = detalles;

        _context.PlantillasMenu.Add(plantilla);
        await _context.SaveChangesAsync();

        return await CargarPlantillaAsync(plantilla.Id);
    }

    public async Task<PlantillaMenuRespuestaDto> ActualizarPlantillaAsync(
        Guid plantillaId,
        ActualizarPlantillaMenuDto dto)
    {
        await _actualizarValidator.ValidateAndThrowAsync(dto);

        var plantilla = await _context.PlantillasMenu
            .FirstOrDefaultAsync(p => p.Id == plantillaId)
            ?? throw new KeyNotFoundException($"No se encontró una plantilla con ID: {plantillaId}");

        await ValidarPlatosAsync(dto.Detalles.Select(d => d.PlatoId));
        ValidarDuplicadosDeComida(dto.Detalles);

        plantilla.Nombre = dto.Nombre.Trim();
        plantilla.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        plantilla.ObjetivoCalorico = dto.ObjetivoCalorico;

        var existentes = await _context.DetallesPlantillaMenu
            .Where(d => d.PlantillaMenuId == plantillaId)
            .ToListAsync();

        var porId = existentes.ToDictionary(d => d.Id);
        var conservados = new List<DetallePlantillaMenu>();
        var orden = 0;

        foreach (var detalleDto in dto.Detalles
                     .OrderBy(d => d.DiaSemana)
                     .ThenBy(d => d.TiempoComida)
                     .ThenBy(d => d.Orden))
        {
            var existente = detalleDto.Id.HasValue
                ? porId.GetValueOrDefault(detalleDto.Id.Value)
                : null;

            if (existente is not null)
            {
                existente.PlatoId = detalleDto.PlatoId;
                existente.DiaSemana = detalleDto.DiaSemana;
                existente.TiempoComida = detalleDto.TiempoComida;
                existente.Cantidad = detalleDto.Cantidad;
                existente.Indicaciones = string.IsNullOrWhiteSpace(detalleDto.Indicaciones)
                    ? null
                    : detalleDto.Indicaciones.Trim();
                existente.Orden = orden++;
                conservados.Add(existente);
            }
            else
            {
                conservados.Add(new DetallePlantillaMenu
                {
                    Id = Guid.NewGuid(),
                    PlantillaMenuId = plantillaId,
                    PlatoId = detalleDto.PlatoId,
                    DiaSemana = detalleDto.DiaSemana,
                    TiempoComida = detalleDto.TiempoComida,
                    Cantidad = detalleDto.Cantidad,
                    Indicaciones = string.IsNullOrWhiteSpace(detalleDto.Indicaciones)
                        ? null
                        : detalleDto.Indicaciones.Trim(),
                    Orden = orden++
                });
            }
        }

        var eliminados = existentes.Where(e => !conservados.Contains(e)).ToList();

        _context.DetallesPlantillaMenu.RemoveRange(eliminados);
        _context.DetallesPlantillaMenu.AddRange(conservados.Where(c => !existentes.Contains(c)));

        await _context.SaveChangesAsync();

        return await CargarPlantillaAsync(plantillaId);
    }

    public async Task<PlantillaMenuRespuestaDto> ObtenerPlantillaPorIdAsync(Guid plantillaId)
    {
        var existe = await _context.PlantillasMenu
            .AsNoTracking()
            .AnyAsync(p => p.Id == plantillaId);

        if (!existe)
        {
            throw new KeyNotFoundException($"No se encontró una plantilla con ID: {plantillaId}");
        }

        return await CargarPlantillaAsync(plantillaId);
    }

    public async Task<IEnumerable<PlantillaMenuRespuestaDto>> ObtenerPlantillasAsync(
        PlantillaMenuFiltroDto filtro)
    {
        await _filtroValidator.ValidateAndThrowAsync(filtro);

        var query = _context.PlantillasMenu.AsNoTracking().AsQueryable();

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

        if (filtro.ObjetivoCaloricoMinimo.HasValue)
        {
            query = query.Where(p => p.ObjetivoCalorico >= filtro.ObjetivoCaloricoMinimo.Value);
        }

        if (filtro.ObjetivoCaloricoMaximo.HasValue)
        {
            query = query.Where(p => p.ObjetivoCalorico <= filtro.ObjetivoCaloricoMaximo.Value);
        }

        if (filtro.DiaSemana.HasValue)
        {
            query = query.Where(p => p.Detalles.Any(d => d.DiaSemana == filtro.DiaSemana.Value));
        }

        if (filtro.TiempoComida.HasValue)
        {
            query = query.Where(p => p.Detalles.Any(d => d.TiempoComida == filtro.TiempoComida.Value));
        }

        var plantillas = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "objetivocalorico" => await (filtro.Descendente
                ? query.OrderByDescending(p => p.ObjetivoCalorico).ToListAsync()
                : query.OrderBy(p => p.ObjetivoCalorico).ToListAsync()),
            "fechacreacion" => await (filtro.Descendente
                ? query.OrderByDescending(p => p.FechaCreacion).ToListAsync()
                : query.OrderBy(p => p.FechaCreacion).ToListAsync()),
            _ => await (filtro.Descendente
                ? query.OrderByDescending(p => p.Nombre).ToListAsync()
                : query.OrderBy(p => p.Nombre).ToListAsync())
        };

        var resultado = new List<PlantillaMenuRespuestaDto>();

        foreach (var plantilla in plantillas)
        {
            resultado.Add(await CargarPlantillaAsync(plantilla.Id));
        }

        return resultado;
    }

    public async Task<PlantillaMenuRespuestaDto> DuplicarPlantillaAsync(Guid plantillaId, string? nombreNuevo)
    {
        var original = await _context.PlantillasMenu
            .AsNoTracking()
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == plantillaId)
            ?? throw new KeyNotFoundException($"No se encontró una plantilla con ID: {plantillaId}");

        var copia = new PlantillaMenu
        {
            Id = Guid.NewGuid(),
            Nombre = nombreNuevo?.Trim() ?? $"{original.Nombre} (copia)",
            Descripcion = original.Descripcion,
            ObjetivoCalorico = original.ObjetivoCalorico,
            NutricionistaId = original.NutricionistaId,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        var detalles = original.Detalles
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .Select(d => new DetallePlantillaMenu
            {
                Id = Guid.NewGuid(),
                PlantillaMenuId = copia.Id,
                PlatoId = d.PlatoId,
                DiaSemana = d.DiaSemana,
                TiempoComida = d.TiempoComida,
                Cantidad = d.Cantidad,
                Indicaciones = d.Indicaciones,
                Orden = d.Orden
            })
            .ToList();

        copia.Detalles = detalles;

        _context.PlantillasMenu.Add(copia);
        await _context.SaveChangesAsync();

        return await CargarPlantillaAsync(copia.Id);
    }

    public async Task RestaurarPlantillaAsync(Guid plantillaId)
    {
        var plantilla = await _context.PlantillasMenu
            .FirstOrDefaultAsync(p => p.Id == plantillaId)
            ?? throw new KeyNotFoundException($"No se encontró una plantilla con ID: {plantillaId}");

        if (plantilla.IsActive)
        {
            throw new ConflictException("La plantilla ya está activa.");
        }

        plantilla.IsActive = true;
        await _context.SaveChangesAsync();
    }

    public async Task EliminarPlantillaAsync(Guid plantillaId, bool forzar = false)
    {
        var plantilla = await _context.PlantillasMenu
            .FirstOrDefaultAsync(p => p.Id == plantillaId)
            ?? throw new KeyNotFoundException($"No se encontró una plantilla con ID: {plantillaId}");

        if (!plantilla.IsActive)
        {
            throw new ConflictException("La plantilla ya está eliminada.");
        }

        if (!forzar)
        {
            var enUso = await _context.MenusAsignados
                .CountAsync(m => m.PlantillaMenuId == plantillaId && m.IsActive);

            if (enUso > 0)
            {
                var planes = enUso == 1 ? "1 plan de paciente" : $"{enUso} planes de pacientes";
                throw new ConflictException(
                    $"No se puede eliminar la plantilla porque está en uso por {planes}. "
                    + "Envía forzar=true para eliminarla igual: los planes conservan su asignación "
                    + "y solo desaparece del catálogo de menús.");
            }
        }

        plantilla.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task<PlantillaMenuRespuestaDto> CargarPlantillaAsync(Guid plantillaId)
    {
        var plantilla = await _context.PlantillasMenu
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == plantillaId)
            ?? throw new KeyNotFoundException($"No se encontró una plantilla con ID: {plantillaId}");

        var detalles = await _context.DetallesPlantillaMenu
            .AsNoTracking()
            .Include(d => d.Plato)
            .ThenInclude(p => p.Detalles)
            .ThenInclude(d => d.Alimento)
            .Where(d => d.PlantillaMenuId == plantillaId)
            .ToListAsync();

        var nutricionistaNombre = plantilla.NutricionistaId.HasValue
            ? await _context.Nutricionistas
                .AsNoTracking()
                .Where(n => n.Id == plantilla.NutricionistaId.Value)
                .Select(n => n.NombreCompleto)
                .FirstOrDefaultAsync()
            : null;

        return MapearPlantilla(plantilla, detalles, nutricionistaNombre);
    }

    private static PlantillaMenuRespuestaDto MapearPlantilla(
        PlantillaMenu plantilla,
        List<DetallePlantillaMenu> detalles,
        string? nutricionistaNombre)
    {
        var detallesDto = detalles
            .Select(d =>
            {
                var macrosDelPlato = CalcularMacrosDelPlato(d.Plato);
                var factor = d.Cantidad;

                return new DetallePlantillaMenuRespuestaDto
                {
                    Id = d.Id,
                    PlatoId = d.PlatoId,
                    PlatoNombre = d.Plato.Nombre,
                    DiaSemana = d.DiaSemana,
                    TiempoComida = d.TiempoComida,
                    Cantidad = d.Cantidad,
                    Indicaciones = d.Indicaciones,
                    Orden = d.Orden,
                    Calorias = decimal.Round(macrosDelPlato.Calorias * factor, 2),
                    Proteinas = decimal.Round(macrosDelPlato.Proteinas * factor, 2),
                    Carbohidratos = decimal.Round(macrosDelPlato.Carbohidratos * factor, 2),
                    Grasas = decimal.Round(macrosDelPlato.Grasas * factor, 2)
                };
            })
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .ToList();

        var caloriasTotales = decimal.Round(detallesDto.Sum(d => d.Calorias), 2);

        var resumen = detallesDto
            .GroupBy(d => new { d.DiaSemana, d.TiempoComida })
            .Select(g => new ResumenTiempoComidaDto
            {
                DiaSemana = g.Key.DiaSemana,
                TiempoComida = g.Key.TiempoComida,
                Calorias = decimal.Round(g.Sum(d => d.Calorias), 2),
                Proteinas = decimal.Round(g.Sum(d => d.Proteinas), 2),
                Carbohidratos = decimal.Round(g.Sum(d => d.Carbohidratos), 2),
                Grasas = decimal.Round(g.Sum(d => d.Grasas), 2)
            })
            .OrderBy(r => r.DiaSemana)
            .ThenBy(r => r.TiempoComida)
            .ToList();

        return new PlantillaMenuRespuestaDto
        {
            Id = plantilla.Id,
            Nombre = plantilla.Nombre,
            Descripcion = plantilla.Descripcion,
            ObjetivoCalorico = plantilla.ObjetivoCalorico,
            NutricionistaId = plantilla.NutricionistaId,
            NutricionistaNombre = nutricionistaNombre,
            Detalles = detallesDto,
            CaloriasTotales = caloriasTotales,
            ProteinasTotales = decimal.Round(detallesDto.Sum(d => d.Proteinas), 2),
            CarbohidratosTotales = decimal.Round(detallesDto.Sum(d => d.Carbohidratos), 2),
            GrasasTotales = decimal.Round(detallesDto.Sum(d => d.Grasas), 2),
            DesviacionCalorica = plantilla.ObjetivoCalorico.HasValue
                ? decimal.Round(caloriasTotales - plantilla.ObjetivoCalorico.Value, 2)
                : null,
            ResumenPorTiempoComida = resumen,
            FechaCreacion = plantilla.FechaCreacion,
            IsActive = plantilla.IsActive
        };
    }

    private static (decimal Calorias, decimal Proteinas, decimal Carbohidratos, decimal Grasas)
        CalcularMacrosDelPlato(Plato plato)
    {
        decimal calorias = 0, proteinas = 0, carbohidratos = 0, grasas = 0;

        foreach (var detalle in plato.Detalles)
        {
            var factor = detalle.RacionGramos / GramosDeReferencia;

            calorias += detalle.Alimento.Calorias * factor;
            proteinas += detalle.Alimento.Proteinas * factor;
            carbohidratos += detalle.Alimento.Carbohidratos * factor;
            grasas += detalle.Alimento.Grasas * factor;
        }

        return (calorias, proteinas, carbohidratos, grasas);
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

    private async Task ValidarPlatosAsync(IEnumerable<Guid> platoIds)
    {
        var ids = platoIds.Distinct().ToList();

        var encontrados = await _context.Platos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .Select(p => p.Id)
            .ToHashSetAsync();

        var faltantes = ids.Except(encontrados).ToList();

        if (faltantes.Any())
        {
            throw new KeyNotFoundException(
                $"No se encontraron los siguientes platos activos: {string.Join(", ", faltantes)}");
        }
    }

    private static void ValidarDuplicadosDeComida(List<CrearDetallePlantillaMenuDto> detalles)
    {
        var duplicados = detalles
            .GroupBy(d => new { d.DiaSemana, d.TiempoComida, d.PlatoId })
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.DiaSemana} / {g.Key.TiempoComida}")
            .ToList();

        if (duplicados.Any())
        {
            throw new ValidationException(
                "El mismo plato aparece más de una vez en el mismo tiempo de comida: "
                + string.Join(", ", duplicados)
                + ". Usa una sola línea con la cantidad sumada.");
        }
    }
}
