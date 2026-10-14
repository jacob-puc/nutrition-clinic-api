using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/platos")]
public class PlatosController : ControllerBase
{
    private readonly IPlatoService _platosService;
    
    public PlatosController(IPlatoService platosService)
    {
        _platosService = platosService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlatoRespuestaDto>>> ObtenerPlatos([FromQuery] PlatoFiltroDto filtro)
    {
        return Ok(await _platosService.ObtenerPlatosAsync(filtro));
    }

    [HttpGet("{platoId:Guid}")]
    public async Task<ActionResult<PlatoRespuestaDto>> ObtenerPlatoPorId(Guid platoId)
    {
        return Ok(await _platosService.ObtenerPlatoPorIdAsync(platoId));
    }
    [HttpPost]
    public async Task<ActionResult<PlatoRespuestaDto>> CrearPlato(CrearPlatoDto dto)
    {
        var plato = await _platosService.CrearPlatoAsync(dto);
        return CreatedAtAction(nameof(ObtenerPlatoPorId), new { platoId = plato.Id }, plato);
    }
    [HttpPost("{platoId:Guid}/duplicar")]
    public async Task<ActionResult<PlatoRespuestaDto>> DuplicarPlato(Guid platoId, [FromBody] DuplicarPlatoDto? dto)
    {
        return Ok(await _platosService.DuplicarPlatoAsync(platoId, dto?.Nombre));
    }
    [HttpPut("{platoId:Guid}")]
    public async Task<ActionResult> ActualizarPlato(Guid platoId, [FromBody] ActualizarPlatoDto dto)
    {
        return Ok(await _platosService.ActualizarPlatoAsync(platoId, dto));
    }
    [HttpPatch("{platoId:Guid}/restaurar")]
    public async Task<ActionResult> RestaurarPlato(Guid platoId)
    {
        await _platosService.RestaurarPlatoAsync(platoId);
        return Ok();
    }
    
    [HttpDelete("{platoId:Guid}")]
    public async Task<ActionResult> EliminarPlato(Guid platoId)
    {
        await _platosService.EliminarPlatoAsync(platoId);
        return NoContent();
    }
    
}