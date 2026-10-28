using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Features.Transacciones.DTOs;
using nutriclinica_backend.Features.Transacciones.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/facturas")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _facturaService;

    public FacturasController(IFacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    [HttpGet]
    public async Task<ActionResult<RespuestaPaginadaDto<FacturaRespuestaDto>>> ObtenerFacturas(
        [FromQuery] FacturaFiltroDto filtro)
    {
        return Ok(await _facturaService.ObtenerFacturasAsync(filtro));
    }

    [HttpGet("{facturaId:guid}")]
    public async Task<ActionResult<FacturaRespuestaDto>> ObtenerFacturaPorId(Guid facturaId)
    {
        return Ok(await _facturaService.ObtenerFacturaPorIdAsync(facturaId));
    }

    [HttpPost]
    public async Task<ActionResult<FacturaRespuestaDto>> EmitirFactura(
        [FromBody] CrearFacturaDto dto)
    {
        var factura = await _facturaService.EmitirFacturaAsync(dto);

        return CreatedAtAction(
            nameof(ObtenerFacturaPorId),
            new { facturaId = factura.Id },
            factura);
    }

    [HttpGet("{facturaId:guid}/pdf")]
    public async Task<IActionResult> ObtenerFacturaPdf(Guid facturaId)
    {
        var factura = await _facturaService.ObtenerFacturaPorIdAsync(facturaId);
        var pdf = await _facturaService.GenerarFacturaPdfAsync(facturaId);

        return File(pdf, "application/pdf", $"factura-{factura.Serie}-{factura.Folio:D4}.pdf");
    }
}
