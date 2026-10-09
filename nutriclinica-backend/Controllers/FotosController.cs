using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/fotos")]
public class FotosController : ControllerBase
{
    private readonly IExpedienteMediaService _mediaService;

    public FotosController(IExpedienteMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FotoRespuestaDto>>> ObtenerFotosPorPaciente(Guid pacienteId)
    {
        return Ok(await _mediaService.ObtenerFotosPorPacienteAsync(pacienteId));
    }

    [HttpGet("{fotoId:guid}")]
    public async Task<ActionResult<FotoRespuestaDto>> ObtenerFotoPorId(Guid pacienteId, Guid fotoId)
    {
        return Ok(await _mediaService.ObtenerFotoPorIdAsync(pacienteId, fotoId));
    }

    [HttpPost]
    public async Task<ActionResult<FotoRespuestaDto>> RegistrarFoto(Guid pacienteId, [FromBody] CrearFotoDto dto)
    {
        var foto = await _mediaService.RegistrarFotoAsync(pacienteId, dto);
        return CreatedAtAction(nameof(ObtenerFotoPorId), new { pacienteId, fotoId = foto.Id }, foto);
    }

    [HttpPost("subir")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<FotoRespuestaDto>> SubirFoto(Guid pacienteId, [FromForm] SubirFotoDto dto)
    {
        var foto = await _mediaService.SubirFotoAsync(pacienteId, dto);
        return CreatedAtAction(nameof(ObtenerFotoPorId), new { pacienteId, fotoId = foto.Id }, foto);
    }

    [HttpPut("{fotoId:guid}")]
    public async Task<ActionResult<FotoRespuestaDto>> ActualizarFoto(
        Guid pacienteId,
        Guid fotoId,
        [FromBody] ActualizarFotoDto dto)
    {
        return Ok(await _mediaService.ActualizarFotoAsync(pacienteId, fotoId, dto));
    }

    [HttpDelete("{fotoId:guid}")]
    public async Task<IActionResult> EliminarFoto(Guid pacienteId, Guid fotoId)
    {
        await _mediaService.EliminarFotoAsync(pacienteId, fotoId);
        return NoContent();
    }
}