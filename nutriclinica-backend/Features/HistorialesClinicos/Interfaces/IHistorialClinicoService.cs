using nutriclinica_backend.Features.HistorialesClinicos.DTOs;

namespace nutriclinica_backend.Features.HistorialesClinicos.Interfaces;

public interface IHistorialClinicoService
{
    Task<HistorialClinicoRespuestaDto> ObtenerHistorialClinicoPorPacienteIdAsync(Guid pacienteId);
    Task<HistorialClinicoRespuestaDto> UpsertHistorialClinicoAsync(Guid pacienteId, CrearHistorialClinicoDto dto);
}