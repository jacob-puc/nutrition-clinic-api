using nutriclinica_backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Pacientes.DTOs;
using nutriclinica_backend.Features.Pacientes.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes")]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _pacienteService;

    public PacientesController(IPacienteService pacienteService)
    {
        _pacienteService = pacienteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PacienteRespuestaDto>>> ObtenerPacientes()
    {
        return Ok(await _pacienteService.ObtenerPacientesAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PacienteRespuestaDto>> ObtenerPacientePorId(Guid id)
    {
        return Ok(await _pacienteService.ObtenerPacientePorIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<PacienteRespuestaDto>> CrearPaciente([FromBody] CrearPacienteDto dto)
    {
        var paciente = await _pacienteService.CrearPacienteAsync(dto);
        return CreatedAtAction(nameof(ObtenerPacientePorId), new { id = paciente.Id }, paciente);
    }

    [Authorize(Roles = Roles.Clinicos)]
    [HttpGet("{id:guid}/expediente")]
    public async Task<ActionResult<ExpedienteCompletoDto>> ObtenerExpedienteCompleto(Guid id)
    {
        return Ok(await _pacienteService.ObtenerExpedienteCompletoAsync(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PacienteRespuestaDto>> ActualizarPaciente(Guid id, [FromBody] ActualizarPacienteDto dto)
    {
        return Ok(await _pacienteService.ActualizarPacienteAsync(id, dto));
    }

    /// <summary>El objetivo se define en consulta, no al alta, asi que va aparte del CRUD del paciente.</summary>
    [Authorize(Roles = Roles.Clinicos)]
    [HttpPut("{id:guid}/objetivo")]
    public async Task<ActionResult<PacienteRespuestaDto>> ActualizarObjetivo(
        Guid id,
        [FromBody] ActualizarObjetivoPacienteDto dto)
    {
        return Ok(await _pacienteService.ActualizarObjetivoAsync(id, dto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> EliminarPaciente(Guid id)
    {
        await _pacienteService.EliminarPacienteAsync(id);
        return NoContent();
    }
}