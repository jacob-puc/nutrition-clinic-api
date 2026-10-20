using nutriclinica_backend.Features.Menus.DTOs;

namespace nutriclinica_backend.Features.Menus.Interfaces;

public interface IPlantillaMenuService
{
    Task<PlantillaMenuRespuestaDto> CrearPlantillaAsync(CrearPlantillaMenuDto dto);
    Task<PlantillaMenuRespuestaDto> ActualizarPlantillaAsync(Guid plantillaId, ActualizarPlantillaMenuDto dto);
    Task<PlantillaMenuRespuestaDto> ObtenerPlantillaPorIdAsync(Guid plantillaId);
    Task<IEnumerable<PlantillaMenuRespuestaDto>> ObtenerPlantillasAsync(PlantillaMenuFiltroDto filtro);
    Task<PlantillaMenuRespuestaDto> DuplicarPlantillaAsync(Guid plantillaId, string? nombreNuevo);
    Task RestaurarPlantillaAsync(Guid plantillaId);
    Task EliminarPlantillaAsync(Guid plantillaId, bool forzar = false);
}
