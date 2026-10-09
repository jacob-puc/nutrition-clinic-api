using nutriclinica_backend.Features.Alimentos.DTOs;

namespace nutriclinica_backend.Features.Alimentos.Interfaces;

public interface IAlimentoService
{
    Task<AlimentoRespuestaDto> CrearAlimentoAsync(CrearAlimentoDto dto);
    Task<AlimentoRespuestaDto> ActualizarAlimentoAsync(Guid alimentoId, ActualizarAlimentoDto dto);
    Task<AlimentoRespuestaDto> ObtenerAlimentoPorIdAsync(Guid alimentoId);
    Task<IEnumerable<AlimentoRespuestaDto>> ObtenerAlimentosAsync(AlimentoFiltroDto filtro);
    Task<AlimentoRespuestaDto> RestaurarAlimentoAsync(Guid alimentoId);
    Task EliminarAlimentoAsync(Guid alimentoId);
}
