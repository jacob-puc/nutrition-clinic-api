using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.PortalPaciente;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Controllers;

/// <summary>Para el personal: quien usa la app e invitar a quien no.</summary>
[ApiController]
[Authorize(Roles = Roles.Personal)]
public class AppPacientesController : ControllerBase
{
    private readonly InvitacionesApp _invitaciones;

    public AppPacientesController(InvitacionesApp invitaciones)
    {
        _invitaciones = invitaciones;
    }

    [HttpGet("api/pacientes/app-estado")]
    public async Task<ActionResult<List<EstadoAppPacienteDto>>> Estados() =>
        Ok(await _invitaciones.EstadosAsync());

    [HttpPost("api/pacientes/{pacienteId:guid}/invitar-app")]
    public async Task<ActionResult<EstadoAppPacienteDto>> Invitar(Guid pacienteId) =>
        Ok(await _invitaciones.InvitarAsync(pacienteId, UsuarioId()));

    private Guid UsuarioId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;
}
