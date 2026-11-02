using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Agenda.DTOs;
using nutriclinica_backend.Features.Agenda.Services;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Horario de atencion y ausencias. Todos los consultan (la agenda los usa
/// para ofrecer solo horas validas); el horario de la clinica lo cambia el
/// administrador, y el de cada persona, ella misma o el administrador.
/// </summary>
[ApiController]
[Authorize]
public class AgendaController : ControllerBase
{
    private readonly IAgendaService _agenda;

    public AgendaController(IAgendaService agenda)
    {
        _agenda = agenda;
    }

    [HttpGet("api/horarios")]
    public async Task<ActionResult<HorariosRespuestaDto>> ObtenerHorarios()
    {
        return Ok(await _agenda.ObtenerHorariosAsync());
    }

    [HttpPut("api/horarios/clinica")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<HorariosRespuestaDto>> ActualizarHorarioClinica(
        [FromBody] ActualizarHorarioClinicaDto dto)
    {
        return Ok(await _agenda.ActualizarHorarioClinicaAsync(dto));
    }

    [HttpPut("api/nutricionistas/{id:guid}/horario")]
    public async Task<ActionResult<HorarioPersonaDto>> ActualizarHorarioPersona(
        Guid id,
        [FromBody] ActualizarHorarioPersonaDto dto)
    {
        if (!PuedeEditar(id)) return Forbid();
        return Ok(await _agenda.ActualizarHorarioPersonaAsync(id, dto));
    }

    /// <summary>Ausencias que terminan de "desde" (hoy, por defecto) en adelante.</summary>
    [HttpGet("api/ausencias")]
    public async Task<ActionResult<IEnumerable<AusenciaDto>>> ObtenerAusencias(
        [FromQuery] Guid? nutricionistaId,
        [FromQuery] DateOnly? desde)
    {
        return Ok(await _agenda.ObtenerAusenciasAsync(nutricionistaId, desde));
    }

    [HttpPost("api/nutricionistas/{id:guid}/ausencias")]
    public async Task<ActionResult<AusenciaCreadaDto>> CrearAusencia(Guid id, [FromBody] CrearAusenciaDto dto)
    {
        if (!PuedeEditar(id)) return Forbid();
        return Ok(await _agenda.CrearAusenciaAsync(id, dto));
    }

    [HttpDelete("api/nutricionistas/{id:guid}/ausencias/{ausenciaId:guid}")]
    public async Task<IActionResult> EliminarAusencia(Guid id, Guid ausenciaId)
    {
        if (!PuedeEditar(id)) return Forbid();
        await _agenda.EliminarAusenciaAsync(id, ausenciaId);
        return NoContent();
    }

    /// <summary>
    /// Huecos libres de una persona entre dos fechas (maximo 31 dias). Sin
    /// duracion usa la de su perfil; el paso es cada cuanto puede empezar.
    /// </summary>
    [HttpGet("api/nutricionistas/{id:guid}/disponibilidad")]
    public async Task<ActionResult<DisponibilidadDto>> ObtenerDisponibilidad(
        Guid id,
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? duracion,
        [FromQuery] int paso = 30)
    {
        return Ok(await _agenda.ObtenerDisponibilidadAsync(id, desde, hasta ?? desde, duracion, paso));
    }

    /// <summary>Lo mismo para todo el equipo que atiende ("con quien este libre").</summary>
    [HttpGet("api/disponibilidad")]
    public async Task<ActionResult<IEnumerable<DisponibilidadDto>>> ObtenerDisponibilidadEquipo(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? duracion,
        [FromQuery] int paso = 30)
    {
        return Ok(await _agenda.ObtenerDisponibilidadEquipoAsync(desde, hasta ?? desde, duracion, paso));
    }

    private bool PuedeEditar(Guid nutricionistaId) =>
        User.EsAdministrador() || User.ObtenerId() == nutricionistaId;
}
