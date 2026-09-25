using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.HistorialesClinicos.DTOs;
using nutriclinica_backend.Features.HistorialesClinicos.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Route("api/pacientes/{pacienteId:guid}/historial-clinico")]
public class HistorialesClinicosController : ControllerBase
{
    private readonly IHistorialClinicoService _historialService;

    public HistorialesClinicosController(IHistorialClinicoService historialService)
    {
        _historialService = historialService;
    }

    [HttpGet]
    public async Task<ActionResult<HistorialClinicoRespuestaDto>> ObtenerHistorialClinico(Guid pacienteId)
    {
        return Ok(await _historialService.ObtenerHistorialClinicoPorPacienteIdAsync(pacienteId));
    }

    [HttpPut]
    public async Task<ActionResult<HistorialClinicoRespuestaDto>> GuardarHistorialClinico(
        Guid pacienteId,
        [FromBody] CrearHistorialClinicoDto dto)
    {
        return Ok(await _historialService.UpsertHistorialClinicoAsync(pacienteId, dto));
    }
}