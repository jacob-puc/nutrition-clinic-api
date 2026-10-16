using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Menus.Services;

public class MenuAsignadoService : IMenuAsignadoService
{
    private const decimal GramosDeReferencia = 100m;

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearMenuAsignadoDto> _crearValidator;
    private readonly IValidator<ActualizarMenuAsignadoDto> _actualizarValidator;
    private readonly IValidator<MenuAsignadoFiltroDto> _filtroValidator;

    public MenuAsignadoService(
        ApplicationDbContext context,
        IValidator<CrearMenuAsignadoDto> crearValidator,
        IValidator<ActualizarMenuAsignadoDto> actualizarValidator,
        IValidator<MenuAsignadoFiltroDto> filtroValidator)
    {
        _context = context;
        _crearValidator = crearValidator;
        _actualizarValidator = actualizarValidator;
        _filtroValidator = filtroValidator;
    }

    public async Task<MenuAsignadoRespuestaDto> ObtenerMenuAsignadoPorIdAsync(Guid menuId)
    {
        var existe = await _context.MenusAsignados
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == menuId)
            ?? throw new KeyNotFoundException(
                $"No se encontró un menú asignado con ID: {menuId}");

        return await CargarMenuAsync(menuId);
    }

    public async Task<IEnumerable<MenuAsignadoRespuestaDto>> ObtenerMenuAsignadoAsync(
        MenuAsignadoFiltroDto filtro)
    {
        await _filtroValidator.ValidateAndThrowAsync(filtro);

        var query = _context.MenusAsignados.AsNoTracking().AsQueryable();

        if (filtro.SoloActivos ?? true)
        {
            query = query.Where(m => m.IsActive);
        }

        if (filtro.PacienteId.HasValue)
        {
            query = query.Where(m => m.PacienteId == filtro.PacienteId.Value);
        }

        if (filtro.Estado.HasValue)
        {
            query = query.Where(m => m.Estado == filtro.Estado.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            query = query.Where(m => m.FechaInicio >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            query = query.Where(m => m.FechaInicio <= filtro.FechaHasta.Value);
        }

        var menus = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "fechainicio" => await (filtro.Descendente
                ? query.OrderByDescending(m => m.FechaInicio).ToListAsync()
                : query.OrderBy(m => m.FechaInicio).ToListAsync()),
            "fechacreacion" => await (filtro.Descendente
                ? query.OrderByDescending(m => m.FechaCreacion).ToListAsync()
                : query.OrderBy(m => m.FechaCreacion).ToListAsync()),
            _ => await (filtro.Descendente
                ? query.OrderByDescending(m => m.Nombre).ToListAsync()
                : query.OrderBy(m => m.Nombre).ToListAsync())
        };

        var resultado = new List<MenuAsignadoRespuestaDto>();

        foreach (var menu in menus)
        {
            resultado.Add(await CargarMenuAsync(menu.Id));
        }

        return resultado;
    }

    public async Task<MenuAsignadoRespuestaDto> AsignarPlantillaAsync(CrearMenuAsignadoDto dto)
    {
        await _crearValidator.ValidateAndThrowAsync(dto);

        await ValidarPacienteAsync(dto.PacienteId);

        var plantilla = await _context.PlantillasMenu
            .AsNoTracking()
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == dto.PlantillaMenuId)
            ?? throw new KeyNotFoundException(
                $"No se encontró una plantilla con ID: {dto.PlantillaMenuId}");

        if (!plantilla.IsActive)
        {
            throw new ConflictException("No se puede asignar una plantilla eliminada.");
        }

        if (!plantilla.Detalles.Any())
        {
            throw new ConflictException(
                "La plantilla no tiene platos asignados. Agrégalos antes de usarla en un paciente.");
        }

        var menu = new MenuAsignado
        {
            Id = Guid.NewGuid(),
            PacienteId = dto.PacienteId,
            PlantillaMenuId = plantilla.Id,
            Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? plantilla.Nombre : dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
                ? plantilla.Descripcion
                : dto.Descripcion.Trim(),
            ObjetivoCalorico = dto.ObjetivoCalorico ?? plantilla.ObjetivoCalorico,
            FechaInicio = dto.FechaInicio,
            Estado = dto.Estado,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        menu.Detalles = plantilla.Detalles
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .Select(d => new DetalleMenuAsignado
            {
                Id = Guid.NewGuid(),
                MenuAsignadoId = menu.Id,
                PlatoId = d.PlatoId,
                DiaSemana = d.DiaSemana,
                TiempoComida = d.TiempoComida,
                Cantidad = d.Cantidad,
                Indicaciones = d.Indicaciones,
                Orden = d.Orden
            })
            .ToList();

        _context.MenusAsignados.Add(menu);

        await _context.SaveChangesAsync();

        return await CargarMenuAsync(menu.Id);
    }

    public async Task<MenuAsignadoRespuestaDto> DuplicarMenuAsignadoAsync(
        Guid menuId,
        string? nombreNuevo,
        Guid? pacienteId)
    {
        var original = await _context.MenusAsignados
            .AsNoTracking()
            .Include(m => m.Detalles)
            .FirstOrDefaultAsync(m => m.Id == menuId)
            ?? throw new KeyNotFoundException($"No se encontró un menú asignado con ID: {menuId}");

        if (pacienteId.HasValue && pacienteId.Value != original.PacienteId)
        {
            await ValidarPacienteAsync(pacienteId.Value);
        }

        var copia = new MenuAsignado
        {
            Id = Guid.NewGuid(),
            PacienteId = pacienteId ?? original.PacienteId,
            PlantillaMenuId = original.PlantillaMenuId,
            Nombre = nombreNuevo?.Trim() ?? $"{original.Nombre} (copia)",
            Descripcion = original.Descripcion,
            ObjetivoCalorico = original.ObjetivoCalorico,
            FechaInicio = original.FechaInicio,
            Estado = MenuAsignadoEstado.Borrador,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        copia.Detalles = original.Detalles
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .Select(d => new DetalleMenuAsignado
            {
                Id = Guid.NewGuid(),
                MenuAsignadoId = copia.Id,
                PlatoId = d.PlatoId,
                DiaSemana = d.DiaSemana,
                TiempoComida = d.TiempoComida,
                Cantidad = d.Cantidad,
                Indicaciones = d.Indicaciones,
                Orden = d.Orden
            })
            .ToList();

        _context.MenusAsignados.Add(copia);

        await _context.SaveChangesAsync();

        return await CargarMenuAsync(copia.Id);
    }

    public async Task<MenuAsignadoRespuestaDto> ActualizarMenuAsignadoAsync(
        Guid menuId,
        ActualizarMenuAsignadoDto dto)
    {
        await _actualizarValidator.ValidateAndThrowAsync(dto);

        var menu = await _context.MenusAsignados
            .FirstOrDefaultAsync(m => m.Id == menuId)
            ?? throw new KeyNotFoundException(
                $"No se encontró un menú asignado con ID: {menuId}");

        if (menu.Estado == MenuAsignadoEstado.Finalizado)
        {
            throw new ConflictException(
                "Un plan finalizado no se modifica. Duplica el plan para hacer cambios.");
        }

        if (menu.Estado != MenuAsignadoEstado.Borrador
            && dto.FechaInicio.HasValue
            && dto.FechaInicio.Value != menu.FechaInicio)
        {
            throw new ConflictException(
                "La fecha de inicio solo se puede cambiar mientras el plan está en borrador.");
        }

        await ValidarPlatosAsync(dto.Detalles.Select(d => d.PlatoId));
        ValidarDuplicadosDeComida(dto.Detalles);

        menu.Nombre = dto.Nombre.Trim();
        menu.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
            ? null
            : dto.Descripcion.Trim();
        menu.ObjetivoCalorico = dto.ObjetivoCalorico;
        menu.Estado = dto.Estado;

        if (dto.FechaInicio.HasValue)
        {
            menu.FechaInicio = dto.FechaInicio.Value;
        }

        var existentes = await _context.DetallesMenuAsignado
            .Where(d => d.MenuAsignadoId == menuId)
            .ToListAsync();

        var porId = existentes.ToDictionary(d => d.Id);
        var conservados = new List<DetalleMenuAsignado>();
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
                conservados.Add(new DetalleMenuAsignado
                {
                    Id = Guid.NewGuid(),
                    MenuAsignadoId = menuId,
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

        _context.DetallesMenuAsignado.RemoveRange(eliminados);
        _context.DetallesMenuAsignado.AddRange(conservados.Where(c => !existentes.Contains(c)));

        await _context.SaveChangesAsync();

        return await CargarMenuAsync(menuId);
    }

    public async Task RestaurarMenuAsignadoAsync(Guid menuId)
    {
        var menuAsignado = await _context.MenusAsignados
            .FirstOrDefaultAsync(p => p.Id == menuId)
            ?? throw new KeyNotFoundException($"No se encontró el menú asignado con ID: {menuId}");

        if (menuAsignado.IsActive)
        {
            throw new ConflictException("El menú asignado ya está activo.");
        }

        menuAsignado.IsActive = true;
        await _context.SaveChangesAsync();

    }

    public async Task EliminarMenuAsignadoAsync(Guid menuId)
    {
        var menuAsignado = await _context.MenusAsignados
            .FirstOrDefaultAsync(m => m.Id == menuId)
            ?? throw new KeyNotFoundException($"No se encontró el menú asignado con ID: {menuId}");

        if (!menuAsignado.IsActive)
        {
            throw new ConflictException("El menú asignado ya está inactivo.");
        }

        if (menuAsignado.Estado == MenuAsignadoEstado.Activo)
        {
            throw new ConflictException("No se puede eliminar un menú asignado activo.");
        }
        
        
        menuAsignado.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task<MenuAsignadoRespuestaDto> CargarMenuAsync(Guid menuId)
    {
        var menu = await _context.MenusAsignados
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == menuId)
            ?? throw new KeyNotFoundException($"No se encontró un menú asignado con ID: {menuId}");

        var detalles = await _context.DetallesMenuAsignado
            .AsNoTracking()
            .Include(d => d.Plato)
            .ThenInclude(p => p.Detalles)
            .ThenInclude(d => d.Alimento)
            .Where(d => d.MenuAsignadoId == menuId)
            .ToListAsync();

        var pacienteNombre = await _context.Pacientes
            .AsNoTracking()
            .Where(p => p.Id == menu.PacienteId)
            .Select(p => p.NombreCompleto)
            .FirstOrDefaultAsync();

        var plantillaNombre = menu.PlantillaMenuId.HasValue
            ? await _context.PlantillasMenu
                .AsNoTracking()
                .Where(p => p.Id == menu.PlantillaMenuId.Value)
                .Select(p => p.Nombre)
                .FirstOrDefaultAsync()
            : null;

        return MapearMenu(menu, detalles, pacienteNombre, plantillaNombre);
    }

    
    private static MenuAsignadoRespuestaDto MapearMenu(
        MenuAsignado menu,
         List<DetalleMenuAsignado> detalles,
        string? pacienteNombre,
        string? plantillaNombre)
    {
        var detallesDto = detalles
            .Select(d =>
            {
                var macrosDelPlato = CalcularMacrosDelPlato(d.Plato);
                var factor = d.Cantidad;

                return new DetalleMenuAsignadoRespuestaDto
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

        return new MenuAsignadoRespuestaDto
        {
            Id = menu.Id,
            PacienteId = menu.PacienteId,
            PacienteNombre = pacienteNombre ?? string.Empty,
            PlantillaMenuId = menu.PlantillaMenuId,
            PlantillaMenuNombre = plantillaNombre,
            Nombre = menu.Nombre,
            Descripcion = menu.Descripcion,
            ObjetivoCalorico = menu.ObjetivoCalorico,
            FechaInicio = menu.FechaInicio,
            Estado = menu.Estado,
            Detalles = detallesDto,
            CaloriasTotales = caloriasTotales,
            ProteinasTotales = decimal.Round(detallesDto.Sum(d => d.Proteinas), 2),
            CarbohidratosTotales = decimal.Round(detallesDto.Sum(d => d.Carbohidratos), 2),
            GrasasTotales = decimal.Round(detallesDto.Sum(d => d.Grasas), 2),
            DesviacionCalorica = menu.ObjetivoCalorico.HasValue
                ? decimal.Round(caloriasTotales - menu.ObjetivoCalorico.Value, 2)
                : null,
            ResumenPorTiempoComida = resumen,
            FechaCreacion = menu.FechaCreacion,
            IsActive = menu.IsActive
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

    private async Task ValidarPacienteAsync(Guid pacienteId)
    {
        var existe = await _context.Pacientes
            .AnyAsync(p => p.Id == pacienteId && p.IsActive);

        if (!existe)
        {
            throw new KeyNotFoundException(
                $"No se encontró un paciente activo con ID: {pacienteId}");
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

    private static void ValidarDuplicadosDeComida(List<ActualizarDetallesMenuAsignadoDto> detalles)
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
