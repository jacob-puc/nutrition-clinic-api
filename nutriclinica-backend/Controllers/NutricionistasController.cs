using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Nutricionistas.DTOs;
using nutriclinica_backend.Features.Nutricionistas.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/nutricionistas")]
public class NutricionistasController : ControllerBase
{
    private readonly INutricionistaService _nutricionistaService;

    public NutricionistasController(INutricionistaService nutricionistaService)
    {
        _nutricionistaService = nutricionistaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NutricionistaRespuestaDto>>> ObtenerNutricionistas(
        [FromQuery] bool incluirInactivos = false)
    {
        return Ok(await _nutricionistaService.ObtenerNutricionistasAsync(incluirInactivos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NutricionistaRespuestaDto>> ObtenerNutricionistaPorId(Guid id)
    {
        return Ok(await _nutricionistaService.ObtenerNutricionistaPorIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<NutricionistaRespuestaDto>> CrearNutricionista(
        [FromBody] CrearNutricionistaDto dto)
    {
        var nutricionista = await _nutricionistaService.CrearNutricionistaAsync(dto);
        return CreatedAtAction(nameof(ObtenerNutricionistaPorId), new { id = nutricionista.Id }, nutricionista);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NutricionistaRespuestaDto>> ActualizarNutricionista(
        Guid id,
        [FromBody] ActualizarNutricionistaDto dto)
    {
        return Ok(await _nutricionistaService.ActualizarNutricionistaAsync(id, dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> EliminarNutricionista(Guid id)
    {
        await _nutricionistaService.EliminarNutricionistaAsync(id);
        return NoContent();
    }

    [HttpPut("{id:guid}/contrasena")]
    public async Task<IActionResult> EstablecerContrasena(
        Guid id,
        [FromBody] EstablecerContrasenaDto dto)
    {
        await _nutricionistaService.EstablecerContrasenaAsync(id, dto.Contrasena);
        return NoContent();
    }
}
