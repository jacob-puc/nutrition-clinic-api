using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Nutricionistas.DTOs;
using nutriclinica_backend.Features.Nutricionistas.Interfaces;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Cuentas del equipo. Cualquiera puede consultar la lista (la agenda la usa
/// para elegir quien atiende); dar de alta, cambiar roles, restablecer
/// contraseñas y desactivar es solo del administrador.
/// </summary>
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
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<NutricionistaRespuestaDto>> CrearNutricionista(
        [FromBody] CrearNutricionistaDto dto)
    {
        var nutricionista = await _nutricionistaService.CrearNutricionistaAsync(dto);
        return CreatedAtAction(nameof(ObtenerNutricionistaPorId), new { id = nutricionista.Id }, nutricionista);
    }

    /// <summary>El administrador edita a cualquiera; los demas solo su propia ficha y sin rol.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NutricionistaRespuestaDto>> ActualizarNutricionista(
        Guid id,
        [FromBody] ActualizarNutricionistaDto dto)
    {
        var actorId = User.ObtenerId();
        var esAdministrador = User.EsAdministrador();

        if (!esAdministrador && id != actorId) return Forbid();

        return Ok(await _nutricionistaService.ActualizarNutricionistaAsync(id, dto, actorId, esAdministrador));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> EliminarNutricionista(Guid id)
    {
        await _nutricionistaService.EliminarNutricionistaAsync(id, User.ObtenerId());
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivar")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<NutricionistaRespuestaDto>> ReactivarNutricionista(Guid id)
    {
        return Ok(await _nutricionistaService.ReactivarNutricionistaAsync(id));
    }

    /// <summary>Contraseña temporal puesta por el administrador: se pide cambiarla al entrar.</summary>
    [HttpPut("{id:guid}/contrasena")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> EstablecerContrasena(
        Guid id,
        [FromBody] EstablecerContrasenaDto dto)
    {
        await _nutricionistaService.EstablecerContrasenaAsync(id, dto.Contrasena, temporal: id != User.ObtenerId());
        return NoContent();
    }

    [HttpGet("{id:guid}/citas-pendientes")]
    public async Task<ActionResult<IEnumerable<CitaPendienteDto>>> ObtenerCitasPendientes(Guid id)
    {
        return Ok(await _nutricionistaService.ObtenerCitasPendientesAsync(id));
    }

    [HttpPost("{id:guid}/reasignar-citas")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<ResultadoReasignacionDto>> ReasignarCitas(
        Guid id,
        [FromBody] ReasignarCitasDto dto)
    {
        return Ok(await _nutricionistaService.ReasignarCitasAsync(id, dto.NutricionistaDestinoId, dto.Desde, dto.Hasta));
    }
}
