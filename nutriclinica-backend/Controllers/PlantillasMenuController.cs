using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;

namespace nutriclinica_backend.Controllers;


[ApiController]
[Authorize]
[Route("api/plantillas-menu")] 
public class PlantillasMenuController : ControllerBase 
{
    private readonly IPlantillaMenuService _plantillaMenuService;
    
    public PlantillasMenuController(IPlantillaMenuService plantillaMenuService)
    {
        _plantillaMenuService = plantillaMenuService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlantillaMenuRespuestaDto>>> ObtenerPlantillasMenu([FromQuery] PlantillaMenuFiltroDto filtro)
    {
        var plantillasMenu = await _plantillaMenuService.ObtenerPlantillasAsync(filtro);
        return Ok(plantillasMenu);
    }
    [HttpGet("{plantillaId:guid}")]
    public async Task<ActionResult<PlantillaMenuRespuestaDto>> ObtenerPlantillaPorId(Guid plantillaId)
    {
        return Ok(await _plantillaMenuService.ObtenerPlantillaPorIdAsync(plantillaId));
    }
    
    [HttpPost]
    public async Task<ActionResult<PlantillaMenuRespuestaDto>> CrearPlantillaMenu([FromBody] CrearPlantillaMenuDto dto)
    {
        var plantillaMenu = await _plantillaMenuService.CrearPlantillaAsync(dto);
        return CreatedAtAction(nameof(ObtenerPlantillaPorId), new { plantillaId = plantillaMenu.Id }, plantillaMenu);
    }

    [HttpPost("{plantillaId:guid}/duplicar")]
    public async Task<ActionResult> DuplicarPlantilla(Guid plantillaId, [FromBody] DuplicarPlantillaMenuDto? dto)
    {
        return Ok(await _plantillaMenuService.DuplicarPlantillaAsync(plantillaId, dto?.Nombre));
    }

    [HttpPut("{plantillaId:guid}")]
    public async Task<ActionResult> ActualizarPlantillaMenu(Guid plantillaId, [FromBody] ActualizarPlantillaMenuDto dto)
    {
        return Ok(await _plantillaMenuService.ActualizarPlantillaAsync(plantillaId, dto));
    }
    [HttpPatch("{plantillaId:guid}/restaurar")]
    public async Task<ActionResult> RestaurarPlantilla (Guid plantillaId)
    {
        await _plantillaMenuService.RestaurarPlantillaAsync(plantillaId);
        return Ok();
    }
    [HttpDelete("{plantillaId:guid}")]
    public async Task<ActionResult> EliminarPlantilla(Guid plantillaId)
    {
        await _plantillaMenuService.EliminarPlantillaAsync(plantillaId);
        return NoContent();
    }
    
    
    
}