using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using nutriclinica_backend.Features.PortalPaciente;
using nutriclinica_backend.Features.Reservas.DTOs;
using nutriclinica_backend.Features.Reservas.Services;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Cuentas de paciente (app movil): registro, verificacion, inicio de sesion
/// con contraseña o Google y recuperacion. Publico, con limite por IP.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/mi/auth")]
[EnableRateLimiting(LimitesReserva.Cuentas)]
public class PacienteAuthController : ControllerBase
{
    private readonly CuentaPacienteService _cuentas;

    public PacienteAuthController(CuentaPacienteService cuentas)
    {
        _cuentas = cuentas;
    }

    [HttpPost("registro")]
    public async Task<ActionResult<VerificacionPendienteDto>> Registrar([FromBody] RegistroPacienteDto dto) =>
        Ok(await _cuentas.RegistrarAsync(dto));

    [HttpPost("verificar")]
    public async Task<ActionResult<TokenPacienteDto>> Verificar([FromBody] VerificarCodigoDto dto) =>
        Ok(await _cuentas.VerificarAsync(dto));

    [HttpPost("reenviar-codigo")]
    public async Task<IActionResult> ReenviarCodigo([FromBody] CorreoDto dto)
    {
        await _cuentas.ReenviarCodigoAsync(dto);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenPacienteDto>> Login([FromBody] LoginPacienteDto dto) =>
        Ok(await _cuentas.LoginAsync(dto));

    [HttpPost("google")]
    public async Task<ActionResult<TokenPacienteDto>> Google([FromBody] GooglePacienteDto dto) =>
        Ok(await _cuentas.GoogleAsync(dto));

    [HttpPost("olvide-contrasena")]
    public async Task<IActionResult> OlvideContrasena([FromBody] CorreoDto dto)
    {
        await _cuentas.OlvideContrasenaAsync(dto);
        return NoContent();
    }

    [HttpPost("restablecer-contrasena")]
    public async Task<ActionResult<TokenPacienteDto>> Restablecer([FromBody] RestablecerContrasenaDto dto) =>
        Ok(await _cuentas.RestablecerAsync(dto));

    [HttpPost("refresh")]
    [DisableRateLimiting]
    public async Task<ActionResult<TokenPacienteDto>> Refresh([FromBody] RefreshPacienteDto dto) =>
        Ok(await _cuentas.RefreshAsync(dto.RefreshToken ?? "")
           ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Inicia sesión de nuevo."));

    [HttpPost("logout")]
    [Authorize(Roles = Roles.Paciente)]
    [DisableRateLimiting]
    public async Task<IActionResult> Logout()
    {
        await _cuentas.LogoutAsync(User.ObtenerId());
        return NoContent();
    }
}

/// <summary>Datos del paciente en sesion. Nunca recibe un id de paciente: sale del token.</summary>
[ApiController]
[Authorize(Roles = Roles.Paciente)]
[Route("api/mi")]
public class PortalPacienteController : ControllerBase
{
    private readonly PortalPacienteService _portal;
    private readonly IReservaService _reservas;

    public PortalPacienteController(PortalPacienteService portal, IReservaService reservas)
    {
        _portal = portal;
        _reservas = reservas;
    }

    // ── Agendar (mismas reglas y disponibilidad que /reservar) ──────────

    [HttpGet("agenda/opciones")]
    public async Task<ActionResult<OpcionesReservaDto>> OpcionesAgenda() =>
        Ok(await _reservas.ObtenerOpcionesAsync());

    [HttpGet("agenda/disponibilidad")]
    public async Task<ActionResult<List<DiaPublicoDto>>> Disponibilidad(
        [FromQuery] Guid? nutricionistaId, [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta) =>
        Ok(await _reservas.ObtenerDisponibilidadAsync(nutricionistaId, desde, hasta));

    public record AgendarMiCitaDto(Guid? NutricionistaId, DateTime Inicio, string? Motivo);

    [HttpPost("citas")]
    [EnableRateLimiting(LimitesReserva.Crear)]
    public async Task<ActionResult<ReservaConfirmadaDto>> Agendar([FromBody] AgendarMiCitaDto dto) =>
        Ok(await _reservas.CrearParaPacienteAsync(User.ObtenerPacienteId(), dto.NutricionistaId, dto.Inicio, dto.Motivo));

    [HttpGet("perfil")]
    public async Task<ActionResult<MiPerfilDto>> Perfil() =>
        Ok(await _portal.PerfilAsync(User.ObtenerPacienteId(), User.ObtenerId()));

    [HttpGet("citas")]
    public async Task<ActionResult<List<MiCitaDto>>> Citas() =>
        Ok(await _portal.CitasAsync(User.ObtenerPacienteId()));

    [HttpPost("citas/{citaId:guid}/confirmar")]
    public async Task<ActionResult<MiCitaDto>> Confirmar(Guid citaId) =>
        Ok(await _portal.ConfirmarCitaAsync(User.ObtenerPacienteId(), citaId));

    [HttpPost("citas/{citaId:guid}/cancelar")]
    public async Task<ActionResult<MiCitaDto>> Cancelar(Guid citaId, [FromBody] CancelarMiCitaDto? dto) =>
        Ok(await _portal.CancelarCitaAsync(User.ObtenerPacienteId(), citaId, dto?.Motivo));

    /// <summary>204 si todavia no tiene un plan activo.</summary>
    [HttpGet("plan")]
    public async Task<ActionResult<MiPlanDto>> Plan()
    {
        var plan = await _portal.PlanAsync(User.ObtenerPacienteId());
        return plan is null ? NoContent() : Ok(plan);
    }

    [HttpGet("progreso")]
    public async Task<ActionResult<MiProgresoDto>> Progreso() =>
        Ok(await _portal.ProgresoAsync(User.ObtenerPacienteId()));
}
