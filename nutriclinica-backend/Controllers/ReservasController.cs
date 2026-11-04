using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using nutriclinica_backend.Features.Reservas.DTOs;
using nutriclinica_backend.Features.Reservas.Services;

namespace nutriclinica_backend.Controllers;

/// <summary>
/// Reservacion en linea, sin sesion. Expone solo lo minimo (nombres, horas
/// libres) y limita peticiones por IP para frenar abusos.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/publico/reserva")]
[EnableRateLimiting(LimitesReserva.Consulta)]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservas;

    public ReservasController(IReservaService reservas)
    {
        _reservas = reservas;
    }

    [HttpGet]
    public async Task<ActionResult<OpcionesReservaDto>> Opciones()
    {
        return Ok(await _reservas.ObtenerOpcionesAsync());
    }

    /// <summary>Horas libres por dia; sin nutricionistaId, las de todo el equipo.</summary>
    [HttpGet("disponibilidad")]
    public async Task<ActionResult<List<DiaPublicoDto>>> Disponibilidad(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] Guid? nutricionistaId)
    {
        var fin = hasta ?? desde.AddDays(6);
        if (fin.DayNumber - desde.DayNumber > 13) fin = desde.AddDays(13);
        return Ok(await _reservas.ObtenerDisponibilidadAsync(nutricionistaId, desde, fin));
    }

    [HttpPost]
    [EnableRateLimiting(LimitesReserva.Crear)]
    public async Task<ActionResult<ReservaConfirmadaDto>> Crear([FromBody] CrearReservaDto dto)
    {
        return Ok(await _reservas.CrearAsync(dto));
    }
}

public static class LimitesReserva
{
    public const string Consulta = "reserva-consulta";
    public const string Crear = "reserva-crear";
}
