using nutriclinica_backend.Features.Citas.DTOs;

namespace nutriclinica_backend.Features.Citas.Interfaces;

public interface ICitaService
{
    Task<CitaRespuestaDto> CrearCitaAsync(CrearCitaDto dto);
    Task<CitaRespuestaDto> ActualizarCitaAsync(Guid citaId, ActualizarCitaDto dto);
    Task<CitaRespuestaDto> ObtenerCitaPorIdAsync(Guid citaId);
    Task<CitaRespuestaDto> CambiarEstadoAsync(Guid citaId, CambiarEstadoCitaDto dto);
    Task<IEnumerable<CitaRespuestaDto>> ObtenerCitasAsync(CitaFiltroDto filtro);
    Task<IEnumerable<CitaRespuestaDto>> ObtenerCitasPorPacienteAsync(Guid pacienteId);
    Task EliminarCitaAsync(Guid citaId);
}
