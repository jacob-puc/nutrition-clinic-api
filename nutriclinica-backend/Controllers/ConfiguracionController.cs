using nutriclinica_backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Transacciones.DTOs;
using nutriclinica_backend.Features.Transacciones.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/configuracion/datos-fiscales")]
public class ConfiguracionController : ControllerBase
{
    private readonly IDatosFiscalesClinicaService _datosFiscalesService;

    public ConfiguracionController(IDatosFiscalesClinicaService datosFiscalesService)
    {
        _datosFiscalesService = datosFiscalesService;
    }

    [HttpGet]
    public async Task<ActionResult<DatosFiscalesClinicaRespuestaDto>> ObtenerDatosFiscales()
    {
        return Ok(await _datosFiscalesService.ObtenerAsync());
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPut]
    public async Task<ActionResult<DatosFiscalesClinicaRespuestaDto>> ActualizarDatosFiscales(
        [FromBody] ActualizarDatosFiscalesClinicaDto dto)
    {
        return Ok(await _datosFiscalesService.ActualizarAsync(dto));
    }
}
