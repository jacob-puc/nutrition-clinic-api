using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Antropometria.DTOs;
using nutriclinica_backend.Features.Antropometria.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/medidas")]
public class AntropometriaController : ControllerBase
{
    private readonly IAntropometriaService _antropometriaService;

    public AntropometriaController(IAntropometriaService antropometriaService)
    {
        _antropometriaService = antropometriaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedidaRespuestaDto>>> ObtenerMedidasPorPaciente(Guid pacienteId)
    {
        return Ok(await _antropometriaService.ObtenerMedidasPorPacienteIdAsync(pacienteId));
    }

    [HttpGet("{medidaId:guid}")]
    public async Task<ActionResult<MedidaRespuestaDto>> ObtenerMedidaPorId(Guid pacienteId, Guid medidaId)
    {
        return Ok(await _antropometriaService.ObtenerMedidaPorIdAsync(pacienteId, medidaId));
    }

    [HttpPost]
    public async Task<ActionResult<MedidaRespuestaDto>> RegistrarMedida(Guid pacienteId, [FromBody] CrearMedidaDto dto)
    {
        var medida = await _antropometriaService.RegistrarMedidaAsync(pacienteId, dto);
        return CreatedAtAction(nameof(ObtenerMedidaPorId), new { pacienteId, medidaId = medida.Id }, medida);
    }

    [HttpPut("{medidaId:guid}")]
    public async Task<ActionResult<MedidaRespuestaDto>> ActualizarMedida(
        Guid pacienteId,
        Guid medidaId,
        [FromBody] ActualizarMedidaDto dto)
    {
        return Ok(await _antropometriaService.ActualizarMedidaAsync(pacienteId, medidaId, dto));
    }

    [HttpDelete("{medidaId:guid}")]
    public async Task<IActionResult> EliminarMedida(Guid pacienteId, Guid medidaId)
    {
        await _antropometriaService.EliminarMedidaAsync(pacienteId, medidaId);
        return NoContent();
    }
}