using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Notificaciones;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Envio de documentos por correo. Las rutas cuelgan del recurso que se
/// envia, como las de los PDF. Plan e historial son informacion clinica
/// (solo quienes atienden); comprobante y factura, cualquiera con sesion.
/// </summary>
[ApiController]
[Authorize]
public class EnviosController : ControllerBase
{
    private readonly IEnvioDocumentos _envios;

    public EnviosController(IEnvioDocumentos envios)
    {
        _envios = envios;
    }

    [Authorize(Roles = Roles.Clinicos)]
    [HttpPost("api/menu-asignados/{menuId:guid}/enviar")]
    public async Task<ActionResult<EnvioDocumentoDto>> EnviarPlan(Guid menuId, [FromBody] EnviarDocumentoDto dto) =>
        Ok(await _envios.PlanAsync(menuId, dto, UsuarioId()));

    [Authorize(Roles = Roles.Clinicos)]
    [HttpPost("api/pacientes/{pacienteId:guid}/historial-clinico/enviar")]
    public async Task<ActionResult<EnvioDocumentoDto>> EnviarHistorial(Guid pacienteId, [FromBody] EnviarDocumentoDto dto) =>
        Ok(await _envios.HistorialAsync(pacienteId, dto, UsuarioId()));

    [HttpPost("api/pagos/{pagoId:guid}/enviar")]
    public async Task<ActionResult<EnvioDocumentoDto>> EnviarComprobante(Guid pagoId, [FromBody] EnviarDocumentoDto dto) =>
        Ok(await _envios.ComprobanteAsync(pagoId, dto, UsuarioId()));

    [HttpPost("api/facturas/{facturaId:guid}/enviar")]
    public async Task<ActionResult<EnvioDocumentoDto>> EnviarFactura(Guid facturaId, [FromBody] EnviarDocumentoDto dto) =>
        Ok(await _envios.FacturaAsync(facturaId, dto, UsuarioId()));

    /// <summary>Ultimos envios de un documento: para mostrar "Enviado el ..." y reenviar.</summary>
    [HttpGet("api/envios")]
    public async Task<ActionResult<List<EnvioDocumentoDto>>> Envios([FromQuery] Guid referenciaId) =>
        Ok(await _envios.EnviosAsync(referenciaId));

    private Guid UsuarioId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;
}
