using nutriclinica_backend.Features.Menus.DTOs;

namespace nutriclinica_backend.Features.Menus.Interfaces;

public interface IPlatoService
{
    Task<PlatoRespuestaDto> CrearPlatoAsync(CrearPlatoDto dto);
    Task<PlatoRespuestaDto> ActualizarPlatoAsync(Guid platoId, ActualizarPlatoDto dto);
    Task<PlatoRespuestaDto> ObtenerPlatoPorIdAsync(Guid platoId);
    Task<IEnumerable<PlatoRespuestaDto>> ObtenerPlatosAsync(PlatoFiltroDto filtro);
    Task<PlatoRespuestaDto> DuplicarPlatoAsync(Guid platoId, string? nombreNuevo);
    Task RestaurarPlatoAsync(Guid platoId);
    Task EliminarPlatoAsync(Guid platoId);
}
