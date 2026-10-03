using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/documentos")]
public class DocumentosController : ControllerBase
{
    private readonly IExpedienteMediaService _mediaService;

    public DocumentosController(IExpedienteMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentoRespuestaDto>>> ObtenerDocumentosPorPaciente(Guid pacienteId)
    {
        return Ok(await _mediaService.ObtenerDocumentosPorPacienteAsync(pacienteId));
    }

    [HttpGet("{documentoId:guid}")]
    public async Task<ActionResult<DocumentoRespuestaDto>> ObtenerDocumentoPorId(Guid pacienteId, Guid documentoId)
    {
        return Ok(await _mediaService.ObtenerDocumentoPorIdAsync(pacienteId, documentoId));
    }

    [HttpPost]
    public async Task<ActionResult<DocumentoRespuestaDto>> RegistrarDocumento(Guid pacienteId, [FromBody] CrearDocumentoDto dto)
    {
        var documento = await _mediaService.RegistrarDocumentoAsync(pacienteId, dto);
        return CreatedAtAction(nameof(ObtenerDocumentoPorId), new { pacienteId, documentoId = documento.Id }, documento);
    }

    [HttpPut("{documentoId:guid}")]
    public async Task<ActionResult<DocumentoRespuestaDto>> ActualizarDocumento(
        Guid pacienteId,
        Guid documentoId,
        [FromBody] ActualizarDocumentoDto dto)
    {
        return Ok(await _mediaService.ActualizarDocumentoAsync(pacienteId, documentoId, dto));
    }

    [HttpDelete("{documentoId:guid}")]
    public async Task<IActionResult> EliminarDocumento(Guid pacienteId, Guid documentoId)
    {
        await _mediaService.EliminarDocumentoAsync(pacienteId, documentoId);
        return NoContent();
    }
}