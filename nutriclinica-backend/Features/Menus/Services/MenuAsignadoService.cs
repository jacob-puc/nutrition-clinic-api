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

    public MenuAsignadoService(
        ApplicationDbContext context,
        IValidator<CrearMenuAsignadoDto> crearValidator)
    {
        _context = context;
        _crearValidator = crearValidator;
    }

    public async Task<MenuAsignadoRespuestaDto> AsignarPlantillaAsync(CrearMenuAsignadoDto dto)
    {
        await _crearValidator.ValidateAndThrowAsync(dto);

        await ValidarPacienteAsync(dto.PacienteId);

        var plantilla = await _context.PlantillasMenu
            .AsNoTracking()
            .Include(p => p.Detalles)
            .ThenInclude(d => d.Plato)
            .ThenInclude(p => p.Detalles)
            .ThenInclude(d => d.Alimento)
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

        menu.Detalles = ClonarDetallesDePlantilla(plantilla, menu.Id);

        var copiasDePlato = CopiarPlatosDePlantilla(plantilla);

        ReapuntarDetallesACopias(menu.Detalles, plantilla.Detalles, copiasDePlato);

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
            .ThenInclude(d => d.Plato)
            .ThenInclude(p => p.Detalles)
            .ThenInclude(d => d.Alimento)
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

        var platosOriginales = CargarPlatosAgrupados(original.Detalles);

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

        var clonados = CopiarPlatosDeMenu(original.Detalles, platosOriginales);

        foreach (var detalle in copia.Detalles)
        {
            var originalId = original.Detalles
                .First(d => d.DiaSemana == detalle.DiaSemana
                            && d.TiempoComida == detalle.TiempoComida
                            && d.Orden == detalle.Orden)
                .PlatoId;

            detalle.PlatoId = clonados[originalId];
        }

        _context.MenusAsignados.Add(copia);

        await _context.SaveChangesAsync();

        return await CargarMenuAsync(copia.Id);
    }

    private static List<DetalleMenuAsignado> ClonarDetallesDePlantilla(
        PlantillaMenu plantilla,
        Guid menuId)
    {
        return plantilla.Detalles
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .Select(d => new DetalleMenuAsignado
            {
                Id = Guid.NewGuid(),
                MenuAsignadoId = menuId,
                PlatoId = d.PlatoId,
                DiaSemana = d.DiaSemana,
                TiempoComida = d.TiempoComida,
                Cantidad = d.Cantidad,
                Indicaciones = d.Indicaciones,
                Orden = d.Orden
            })
            .ToList();
    }

    private Dictionary<Guid, Guid> CopiarPlatosDePlantilla(PlantillaMenu plantilla)
    {
        var platosUnicos = plantilla.Detalles
            .Select(d => d.Plato)
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var copias = new Dictionary<Guid, Guid>();

        foreach (var (idOriginal, plato) in platosUnicos)
        {
            var copia = CopiarPlato(plato);
            copias[idOriginal] = copia.Id;
            _context.Platos.Add(copia);
        }

        return copias;
    }

    private Dictionary<Guid, Guid> CopiarPlatosDeMenu(
        ICollection<DetalleMenuAsignado> detalles,
        Dictionary<Guid, Plato> platosOriginales)
    {
        var copias = new Dictionary<Guid, Guid>();

        foreach (var idOriginal in detalles.Select(d => d.PlatoId).Distinct())
        {
            var copia = CopiarPlato(platosOriginales[idOriginal]);
            copias[idOriginal] = copia.Id;
            _context.Platos.Add(copia);
        }

        return copias;
    }

    private static Plato CopiarPlato(Plato original)
    {
        var copia = new Plato
        {
            Id = Guid.NewGuid(),
            Nombre = original.Nombre,
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

        return copia;
    }

    private static void ReapuntarDetallesACopias(
        ICollection<DetalleMenuAsignado> detalles,
        ICollection<DetallePlantillaMenu> detallesPlantilla,
        Dictionary<Guid, Guid> copias)
    {
        var originales = detallesPlantilla
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.TiempoComida)
            .ThenBy(d => d.Orden)
            .ToList();

        for (var i = 0; i < detalles.Count; i++)
        {
            var detalle = detalles.ElementAt(i);
            detalle.PlatoId = copias[originales[i].PlatoId];
        }
    }

    private static Dictionary<Guid, Plato> CargarPlatosAgrupados(
        ICollection<DetalleMenuAsignado> detalles)
    {
        return detalles
            .Select(d => d.Plato)
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First());
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
}
