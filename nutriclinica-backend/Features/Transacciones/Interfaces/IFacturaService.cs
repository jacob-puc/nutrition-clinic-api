using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Features.Transacciones.DTOs;

namespace nutriclinica_backend.Features.Transacciones.Interfaces;

public interface IFacturaService
{
    Task<FacturaRespuestaDto> EmitirFacturaAsync(CrearFacturaDto dto);
    Task<RespuestaPaginadaDto<FacturaRespuestaDto>> ObtenerFacturasAsync(FacturaFiltroDto filtro);
    Task<FacturaRespuestaDto> ObtenerFacturaPorIdAsync(Guid facturaId);
    Task<byte[]> GenerarFacturaPdfAsync(Guid facturaId);
}
