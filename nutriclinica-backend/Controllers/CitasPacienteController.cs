using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Citas.DTOs;
using nutriclinica_backend.Features.Citas.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/citas")]
public class CitasPacienteController : ControllerBase
{
    private readonly ICitaService _citaService;

    public CitasPacienteController(ICitaService citaService)
    {
        _citaService = citaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaRespuestaDto>>> ObtenerCitasPorPaciente(Guid pacienteId)
    {
        return Ok(await _citaService.ObtenerCitasPorPacienteAsync(pacienteId));
    }
}
