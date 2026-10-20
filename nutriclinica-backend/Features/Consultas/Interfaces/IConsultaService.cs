using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Features.Consultas.DTOs;

namespace nutriclinica_backend.Features.Consultas.Interfaces;

public interface IConsultaService
{
    Task<ConsultaRespuestaDto> CrearConsultaAsync(CrearConsultaDto dto);
    Task<ConsultaRespuestaDto> CerrarConsultaAsync(Guid consultaId, CerrarConsultaDto dto);
    Task<ConsultaRespuestaDto> ObtenerConsultaPorIdAsync(Guid consultaId);
    Task<RespuestaPaginadaDto<ConsultaRespuestaDto>> ObtenerConsultasAsync(ConsultaFiltroDto filtro);
    Task<IEnumerable<ConsultaRespuestaDto>> ObtenerConsultasPorPacienteAsync(Guid pacienteId);
    Task<ConsultaRespuestaDto?> ObtenerConsultaPorCitaAsync(Guid citaId);
}
