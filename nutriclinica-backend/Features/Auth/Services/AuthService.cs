using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.Auth.DTOs;
using nutriclinica_backend.Features.Auth.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtConfig _jwtConfig;
    private readonly PasswordHasher<Nutricionista> _hasher = new();
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RefreshTokenDto> _refreshValidator;

    public AuthService(
        ApplicationDbContext context,
        JwtConfig jwtConfig,
        IValidator<LoginDto> loginValidator,
        IValidator<RefreshTokenDto> refreshValidator)
    {
        _context = context;
        _jwtConfig = jwtConfig;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    public static string HashPassword(Nutricionista nutricionista, string contrasena) =>
        new PasswordHasher<Nutricionista>().HashPassword(nutricionista, contrasena);

    public async Task<TokenRespuestaDto> LoginAsync(LoginDto dto)
    {
        await _loginValidator.ValidateAndThrowAsync(dto);

        var correo = dto.CorreoElectronico.Trim().ToLower();

        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.CorreoElectronico.ToLower() == correo);

        if (nutricionista is null || !VerificarContrasena(nutricionista, dto.Contrasena))
        {
            throw new UnauthorizedAccessException("Correo electrónico o contraseña incorrectos.");
        }

        if (!nutricionista.IsActive)
        {
            throw new UnauthorizedAccessException("La cuenta se encuentra inactiva.");
        }

        return await EmitirTokensAsync(nutricionista);
    }

    public async Task<TokenRespuestaDto> RefreshAsync(RefreshTokenDto dto)
    {
        await _refreshValidator.ValidateAndThrowAsync(dto);

        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.RefreshToken == dto.RefreshToken);

        if (nutricionista is null)
        {
            throw new UnauthorizedAccessException("El refresh token no es válido.");
        }

        if (nutricionista.RefreshTokenExpiresAt is null
            || DateTime.UtcNow > nutricionista.RefreshTokenExpiresAt.Value)
        {
            LimpiarRefreshToken(nutricionista);
            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("El refresh token expiró. Inicia sesión nuevamente.");
        }

        if (!nutricionista.IsActive)
        {
            throw new UnauthorizedAccessException("La cuenta se encuentra inactiva.");
        }

        return await EmitirTokensAsync(nutricionista);
    }

    public async Task LogoutAsync(Guid nutricionistaId)
    {
        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == nutricionistaId);

        if (nutricionista is null) return;

        LimpiarRefreshToken(nutricionista);
        await _context.SaveChangesAsync();
    }

    private bool VerificarContrasena(Nutricionista nutricionista, string contrasena)
    {
        if (string.IsNullOrEmpty(nutricionista.PasswordHash)) return false;

        var resultado = _hasher.VerifyHashedPassword(
            nutricionista, nutricionista.PasswordHash, contrasena);

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            nutricionista.PasswordHash = _hasher.HashPassword(nutricionista, contrasena);
        }

        return resultado != PasswordVerificationResult.Failed;
    }

    private async Task<TokenRespuestaDto> EmitirTokensAsync(Nutricionista nutricionista)
    {
        var ahora = DateTime.UtcNow;
        var expiracion = ahora.AddHours(_jwtConfig.ExpirationHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, nutricionista.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, nutricionista.CorreoElectronico),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, nutricionista.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            notBefore: ahora,
            expires: expiracion,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        nutricionista.RefreshToken = refreshToken;
        nutricionista.RefreshTokenExpiresAt = ahora.AddDays(_jwtConfig.RefreshExpirationDays);

        await _context.SaveChangesAsync();

        return new TokenRespuestaDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            ExpiraEnSegundos = (int)(expiracion - ahora).TotalSeconds,
            NutricionistaId = nutricionista.Id,
            NombreCompleto = nutricionista.NombreCompleto,
            CorreoElectronico = nutricionista.CorreoElectronico
        };
    }

    private static void LimpiarRefreshToken(Nutricionista nutricionista)
    {
        nutricionista.RefreshToken = null;
        nutricionista.RefreshTokenExpiresAt = null;
    }
}
