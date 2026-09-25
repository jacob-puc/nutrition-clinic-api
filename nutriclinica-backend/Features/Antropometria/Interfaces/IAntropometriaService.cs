using nutriclinica_backend.Features.Antropometria.DTOs;

namespace nutriclinica_backend.Features.Antropometria.Interfaces;

public interface IAntropometriaService
{
    Task<MedidaRespuestaDto> RegistrarMedidaAsync(Guid pacienteId, CrearMedidaDto dto);
    Task<IEnumerable<MedidaRespuestaDto>> ObtenerMedidasPorPacienteIdAsync(Guid pacienteId);
    Task<MedidaRespuestaDto> ObtenerMedidaPorIdAsync(Guid pacienteId, Guid medidaId);
    Task<MedidaRespuestaDto> ActualizarMedidaAsync(Guid pacienteId, Guid medidaId, ActualizarMedidaDto dto);
    Task EliminarMedidaAsync(Guid pacienteId, Guid medidaId);
}