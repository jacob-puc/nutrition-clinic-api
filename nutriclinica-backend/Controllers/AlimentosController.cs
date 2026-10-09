using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Alimentos.DTOs;
using nutriclinica_backend.Features.Alimentos.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/alimentos")]
public class AlimentosController : ControllerBase
{
    private readonly IAlimentoService _alimentoService;

    public AlimentosController(IAlimentoService alimentoService)
    {
        _alimentoService = alimentoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlimentoRespuestaDto>>> ObtenerAlimentos(
        [FromQuery] AlimentoFiltroDto filtro)
    {
        return Ok(await _alimentoService.ObtenerAlimentosAsync(filtro));
    }

    [HttpGet("{alimentoId:guid}")]
    public async Task<ActionResult<AlimentoRespuestaDto>> ObtenerAlimentoPorId(Guid alimentoId)
    {
        return Ok(await _alimentoService.ObtenerAlimentoPorIdAsync(alimentoId));
    }

    [HttpPost]
    public async Task<ActionResult<AlimentoRespuestaDto>> CrearAlimento(CrearAlimentoDto dto)
    {
        var alimento = await _alimentoService.CrearAlimentoAsync(dto);
        return CreatedAtAction(nameof(ObtenerAlimentoPorId), new { alimentoId = alimento.Id }, alimento);
    }

    [HttpPost("{alimentoId:guid}/imagen")]
    public async Task<ActionResult<AlimentoRespuestaDto>> SubirImagenAlimento(
        Guid alimentoId,
        [FromForm] IFormFile imagen)
    {
        return Ok(await _alimentoService.SubirImagenAlimentoAsync(alimentoId, imagen));
    }

    [HttpPut("{alimentoId:guid}")]
    public async Task<ActionResult> ActualizarAlimento(Guid alimentoId, [FromBody] ActualizarAlimentoDto dto)
    {
        return Ok(await _alimentoService.ActualizarAlimentoAsync(alimentoId, dto));
    }

    [HttpPatch("{alimentoId:guid}/restaurar")]
    public async Task<ActionResult> RestaurarAlimento(Guid alimentoId)
    {
        return Ok(await _alimentoService.RestaurarAlimentoAsync(alimentoId));
    }

    [HttpDelete("{alimentoId:guid}")]
    public async Task<ActionResult> EliminarAlimento(Guid alimentoId)
    {
        await _alimentoService.EliminarAlimentoAsync(alimentoId);
        return NoContent();
    }
    
}