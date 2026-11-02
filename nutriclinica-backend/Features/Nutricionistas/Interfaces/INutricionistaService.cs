using nutriclinica_backend.Features.Nutricionistas.DTOs;

namespace nutriclinica_backend.Features.Nutricionistas.Interfaces;

public interface INutricionistaService
{
    Task<NutricionistaRespuestaDto> CrearNutricionistaAsync(CrearNutricionistaDto dto);

    Task<NutricionistaRespuestaDto> ActualizarNutricionistaAsync(
        Guid id, ActualizarNutricionistaDto dto, Guid actorId, bool actorEsAdministrador);

    /// <param name="temporal">Si es true, la cuenta debe cambiarla en su proximo inicio de sesion.</param>
    Task EstablecerContrasenaAsync(Guid id, string contrasena, bool temporal);

    Task<NutricionistaRespuestaDto> ObtenerNutricionistaPorIdAsync(Guid id);
    Task<IEnumerable<NutricionistaRespuestaDto>> ObtenerNutricionistasAsync(bool incluirInactivos);
    Task EliminarNutricionistaAsync(Guid id, Guid actorId);
    Task<NutricionistaRespuestaDto> ReactivarNutricionistaAsync(Guid id);

    Task<IEnumerable<CitaPendienteDto>> ObtenerCitasPendientesAsync(Guid id);
    Task<ResultadoReasignacionDto> ReasignarCitasAsync(Guid id, Guid destinoId, DateOnly? desde = null, DateOnly? hasta = null);
}
