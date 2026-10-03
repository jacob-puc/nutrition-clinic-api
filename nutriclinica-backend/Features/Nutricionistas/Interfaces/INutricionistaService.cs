using nutriclinica_backend.Features.Nutricionistas.DTOs;

namespace nutriclinica_backend.Features.Nutricionistas.Interfaces;

public interface INutricionistaService
{
    Task<NutricionistaRespuestaDto> CrearNutricionistaAsync(CrearNutricionistaDto dto);
    Task<NutricionistaRespuestaDto> ActualizarNutricionistaAsync(Guid id, ActualizarNutricionistaDto dto);

    Task EstablecerContrasenaAsync(Guid id, string contrasena);
    Task<NutricionistaRespuestaDto> ObtenerNutricionistaPorIdAsync(Guid id);
    Task<IEnumerable<NutricionistaRespuestaDto>> ObtenerNutricionistasAsync(bool incluirInactivos);
    Task EliminarNutricionistaAsync(Guid id);
}
