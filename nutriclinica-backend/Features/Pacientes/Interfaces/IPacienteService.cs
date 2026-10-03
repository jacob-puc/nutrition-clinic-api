using nutriclinica_backend.Features.Pacientes.DTOs;

namespace nutriclinica_backend.Features.Pacientes.Interfaces;

public interface IPacienteService
{
    Task<PacienteRespuestaDto> CrearPacienteAsync(CrearPacienteDto dto);
    Task<IEnumerable<PacienteRespuestaDto>> ObtenerPacientesAsync();
    Task<PacienteRespuestaDto> ObtenerPacientePorIdAsync(Guid id);
    Task<PacienteRespuestaDto> ActualizarPacienteAsync(Guid id, ActualizarPacienteDto dto);
    Task<PacienteRespuestaDto> ActualizarObjetivoAsync(Guid id, ActualizarObjetivoPacienteDto dto);
    Task EliminarPacienteAsync(Guid id);
    Task<ExpedienteCompletoDto> ObtenerExpedienteCompletoAsync(Guid id);
}