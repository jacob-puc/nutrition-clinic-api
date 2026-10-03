using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Citas.DTOs;
using nutriclinica_backend.Features.Citas.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Route("api/citas")]
public class CitasController : ControllerBase
{
    private readonly ICitaService _citaService;

    public CitasController(ICitaService citaService)
    {
        _citaService = citaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaRespuestaDto>>> ObtenerCitas([FromQuery] CitaFiltroDto filtro)
    {
        return Ok(await _citaService.ObtenerCitasAsync(filtro));
    }

    [HttpGet("{citaId:guid}")]
    public async Task<ActionResult<CitaRespuestaDto>> ObtenerCitaPorId(Guid citaId)
    {
        return Ok(await _citaService.ObtenerCitaPorIdAsync(citaId));
    }

    [HttpPost]
    public async Task<ActionResult<CitaRespuestaDto>> CrearCita([FromBody] CrearCitaDto dto)
    {
        var cita = await _citaService.CrearCitaAsync(dto);
        return CreatedAtAction(nameof(ObtenerCitaPorId), new { citaId = cita.Id }, cita);
    }

    [HttpPut("{citaId:guid}")]
    public async Task<ActionResult<CitaRespuestaDto>> ActualizarCita(
        Guid citaId,
        [FromBody] ActualizarCitaDto dto)
    {
        return Ok(await _citaService.ActualizarCitaAsync(citaId, dto));
    }

    [HttpPatch("{citaId:guid}/estado")]
    public async Task<ActionResult<CitaRespuestaDto>> CambiarEstado(
        Guid citaId,
        [FromBody] CambiarEstadoCitaDto dto)
    {
        return Ok(await _citaService.CambiarEstadoAsync(citaId, dto));
    }

    [HttpDelete("{citaId:guid}")]
    public async Task<IActionResult> EliminarCita(Guid citaId)
    {
        await _citaService.EliminarCitaAsync(citaId);
        return NoContent();
    }
}
