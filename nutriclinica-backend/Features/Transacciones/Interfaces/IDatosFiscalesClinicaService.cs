using nutriclinica_backend.Features.Transacciones.DTOs;

namespace nutriclinica_backend.Features.Transacciones.Interfaces;

public interface IDatosFiscalesClinicaService
{
    Task<DatosFiscalesClinicaRespuestaDto> ObtenerAsync();
    Task<DatosFiscalesClinicaRespuestaDto> ActualizarAsync(ActualizarDatosFiscalesClinicaDto dto);
}
