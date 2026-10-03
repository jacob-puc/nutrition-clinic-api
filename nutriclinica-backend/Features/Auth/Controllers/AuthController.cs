using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Features.Auth.DTOs;
using nutriclinica_backend.Features.Auth.Interfaces;
using nutriclinica_backend.Features.Nutricionistas.DTOs;
using nutriclinica_backend.Features.Nutricionistas.Interfaces;

namespace nutriclinica_backend.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly INutricionistaService _nutricionistaService;

    public AuthController(IAuthService authService, INutricionistaService nutricionistaService)
    {
        _authService = authService;
        _nutricionistaService = nutricionistaService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenRespuestaDto>> Login(LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);
        return Ok(token);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenRespuestaDto>> Refresh(RefreshTokenDto dto)
    {
        var token = await _authService.RefreshAsync(dto);
        return Ok(token);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _authService.LogoutAsync(id);
        return NoContent();
    }

    [HttpGet("yo")]
    [Authorize]
    public async Task<ActionResult<NutricionistaRespuestaDto>> Yo()
    {
        var id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var nutricionista = await _nutricionistaService.ObtenerNutricionistaPorIdAsync(id);
        return Ok(nutricionista);
    }
}
