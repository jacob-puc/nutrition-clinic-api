using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/menu-asignados")]
public class MenuAsignadosController : ControllerBase
{
    private readonly IMenuAsignadoService _menuAsignadosService;
    
    public MenuAsignadosController(IMenuAsignadoService menuAsignadosService)
    {
        _menuAsignadosService = menuAsignadosService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuAsignadoRespuestaDto>>> ObtenerMenuAsignados([FromQuery] MenuAsignadoFiltroDto filtro)
    {
        var menuAsignados = await _menuAsignadosService.ObtenerMenuAsignadoAsync(filtro);
        return Ok(menuAsignados);
    }
    [HttpGet("{menuId:guid}")]
    public async Task<ActionResult<MenuAsignadoRespuestaDto>> ObtenerMenuAsignadoPorId(Guid menuId)
    {
        var menuAsignado = await _menuAsignadosService.ObtenerMenuAsignadoPorIdAsync(menuId);
        return Ok(menuAsignado);
    }
    
    [HttpPost("asignar")]
    public async Task<ActionResult<MenuAsignadoRespuestaDto>> AsignarPlantilla([FromBody] CrearMenuAsignadoDto dto)
    {
        var menuAsignado = await _menuAsignadosService.AsignarPlantillaAsync(dto);
        return CreatedAtAction(nameof(ObtenerMenuAsignadoPorId), new {menuId = menuAsignado.Id}, menuAsignado);
    }

    [HttpPost("{menuId:guid}/duplicar")]
    public async Task<ActionResult<MenuAsignadoRespuestaDto>> DuplicarMenuAsignado(Guid menuId, [FromBody] DuplicarMenuAsignadoDto? dto)
    {
        return Ok(await _menuAsignadosService.DuplicarMenuAsignadoAsync(menuId, dto?.Nombre, dto?.PacienteId));
    }
    [HttpPut("{menuId:guid}")]
    public async Task<ActionResult<MenuAsignadoRespuestaDto>> ActualizarMenuAsignado(Guid menuId, [FromBody] ActualizarMenuAsignadoDto dto)
    {
        return Ok(await _menuAsignadosService.ActualizarMenuAsignadoAsync(menuId, dto));
    }
    
    [HttpPatch("{menuId:guid}/restaurar")]
    public async Task<ActionResult> RestaurarMenuAsignado(Guid menuId)
    {
        await _menuAsignadosService.RestaurarMenuAsignadoAsync(menuId);
        return Ok();
    }
    
    [HttpDelete("{menuId:guid}")]
    public async Task<ActionResult> EliminarMenuAsignado(Guid menuId)
    {
        await _menuAsignadosService.EliminarMenuAsignadoAsync(menuId);
        return NoContent();
    }
}