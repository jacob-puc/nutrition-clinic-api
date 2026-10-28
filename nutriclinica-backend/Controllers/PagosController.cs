using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Features.Transacciones.DTOs;
using nutriclinica_backend.Features.Transacciones.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pagos")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _pagoService;
    private readonly IFacturaService _facturaService;

    public PagosController(IPagoService pagoService, IFacturaService facturaService)
    {
        _pagoService = pagoService;
        _facturaService = facturaService;
    }

    [HttpGet]
    public async Task<ActionResult<RespuestaPaginadaDto<PagoRespuestaDto>>> ObtenerPagos(
        [FromQuery] PagoFiltroDto filtro)
    {
        return Ok(await _pagoService.ObtenerPagosAsync(filtro));
    }

    [HttpGet("{pagoId:guid}")]
    public async Task<ActionResult<PagoRespuestaDto>> ObtenerPagoPorId(Guid pagoId)
    {
        return Ok(await _pagoService.ObtenerPagoPorIdAsync(pagoId));
    }

    [HttpPost]
    public async Task<ActionResult<PagoRespuestaDto>> CrearPago([FromBody] CrearPagoDto dto)
    {
        var pago = await _pagoService.CrearPagoAsync(dto);
        return CreatedAtAction(nameof(ObtenerPagoPorId), new { pagoId = pago.Id }, pago);
    }

    [HttpPost("{pagoId:guid}/anular")]
    public async Task<ActionResult<PagoRespuestaDto>> AnularPago(
        Guid pagoId,
        [FromBody] AnularPagoDto dto)
    {
        return Ok(await _pagoService.AnularPagoAsync(pagoId, dto));
    }

    [HttpGet("{pagoId:guid}/comprobante")]
    public async Task<IActionResult> ObtenerComprobante(Guid pagoId)
    {
        var pago = await _pagoService.ObtenerPagoPorIdAsync(pagoId);
        var pdf = await _pagoService.GenerarComprobantePdfAsync(pagoId);

        return File(pdf, "application/pdf", $"comprobante-{pago.NumeroRecibo}.pdf");
    }

    [HttpPost("{pagoId:guid}/factura")]
    public async Task<ActionResult<FacturaRespuestaDto>> EmitirFactura(
        Guid pagoId,
        [FromBody] CrearFacturaDto dto)
    {
        dto.PagoId = pagoId;
        var factura = await _facturaService.EmitirFacturaAsync(dto);

        return Created($"/api/facturas/{factura.Id}", factura);
    }
}
