using nutriclinica_backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Reportes.DTOs;
using nutriclinica_backend.Features.Reportes.Services;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Documentos imprimibles. Las rutas cuelgan del recurso que imprimen (el
/// plan, el paciente) para que se lean igual que el resto de la API, sin
/// tocar los controllers de esos recursos.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Clinicos)]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reportes;
    private readonly IReporteIngresosService _ingresos;

    public ReportesController(IReporteService reportes, IReporteIngresosService ingresos)
    {
        _reportes = reportes;
        _ingresos = ingresos;
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet("api/reportes/ingresos")]
    public async Task<ActionResult<ReporteIngresosDto>> Ingresos([FromQuery] ReporteIngresosFiltroDto filtro)
    {
        return Ok(await _ingresos.ObtenerAsync(filtro));
    }

    [HttpGet("api/menu-asignados/{menuId:guid}/pdf")]
    public async Task<IActionResult> MenuPaciente(Guid menuId)
    {
        var (pdf, nombre) = await _reportes.MenuPacienteAsync(menuId);
        return File(pdf, "application/pdf", nombre);
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/historial-clinico/pdf")]
    public async Task<IActionResult> HistorialClinico(Guid pacienteId)
    {
        var (pdf, nombre) = await _reportes.HistorialClinicoAsync(pacienteId);
        return File(pdf, "application/pdf", nombre);
    }
}
