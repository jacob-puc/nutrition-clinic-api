using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Consultas.DTOs;
using nutriclinica_backend.Features.Consultas.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/consultas")]
public class ConsultasPacienteController : ControllerBase
{
    private readonly IConsultaService _consultaService;

    public ConsultasPacienteController(IConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConsultaRespuestaDto>>> ObtenerConsultasDelPaciente(
        Guid pacienteId)
    {
        return Ok(await _consultaService.ObtenerConsultasPorPacienteAsync(pacienteId));
    }
}
