using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Features.Transacciones.DTOs;

namespace nutriclinica_backend.Features.Transacciones.Interfaces;

public interface IPagoService
{
    Task<PagoRespuestaDto> CrearPagoAsync(CrearPagoDto dto);
    Task<RespuestaPaginadaDto<PagoRespuestaDto>> ObtenerPagosAsync(PagoFiltroDto filtro);
    Task<PagoRespuestaDto> ObtenerPagoPorIdAsync(Guid pagoId);
    Task<PagoRespuestaDto> AnularPagoAsync(Guid pagoId, AnularPagoDto dto);
    Task<byte[]> GenerarComprobantePdfAsync(Guid pagoId);
}
