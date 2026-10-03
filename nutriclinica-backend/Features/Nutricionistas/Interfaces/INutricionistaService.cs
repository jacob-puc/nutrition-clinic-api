using nutriclinica_backend.Features.Nutricionistas.DTOs;

namespace nutriclinica_backend.Features.Nutricionistas.Interfaces;

public interface INutricionistaService
{
    Task<NutricionistaRespuestaDto> CrearNutricionistaAsync(CrearNutricionistaDto dto);
    Task<NutricionistaRespuestaDto> ActualizarNutricionistaAsync(Guid id, ActualizarNutricionistaDto dto);
    Task<NutricionistaRespuestaDto> ObtenerNutricionistaPorIdAsync(Guid id);
    Task<IEnumerable<NutricionistaRespuestaDto>> ObtenerNutricionistasAsync(bool incluirInactivos);
    Task EliminarNutricionistaAsync(Guid id);
}
