using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Nutricionistas.Services;
using nutriclinica_backend.Infrastructure.Correo;
using nutriclinica_backend.Infrastructure.Persistence;
using nutriclinica_backend.Infrastructure.Security;

namespace nutriclinica_backend.Features.PortalPaciente;

public class GoogleConfig
{
    public const string SectionName = "Google";

    /// <summary>
    /// Client IDs de OAuth aceptados como audiencia del ID token (el "Web
    /// client" que usa la app como serverClientId, y los de Android/iOS).
    /// </summary>
    public string[] ClientIds { get; set; } = [];
}

/// <summary>
/// Cuentas de paciente para la app movil: registro con codigo por correo,
/// inicio de sesion con contraseña o Google, renovacion de sesion y
/// recuperacion de contraseña. La cuenta se vincula a su expediente solo
/// con el correo verificado (nunca por nombre).
/// </summary>
public class CuentaPacienteService
{
    private const int MinutosCodigo = 15;
    private const int IntentosCodigo = 5;
    private const int SegundosEntreCodigos = 60;
    private const string ParaVerificar = "verificar";
    private const string ParaRecuperar = "recuperar";

    private readonly ApplicationDbContext _context;
    private readonly JwtConfig _jwt;
    private readonly GoogleConfig _google;
    private readonly IColaCorreos _cola;
    private readonly CorreoConfig _correo;
    private readonly ILogger<CuentaPacienteService> _logger;
    private readonly PasswordHasher<CuentaPaciente> _hasher = new();

    public CuentaPacienteService(
        ApplicationDbContext context,
        JwtConfig jwt,
        GoogleConfig google,
        IColaCorreos cola,
        CorreoConfig correo,
        ILogger<CuentaPacienteService> logger)
    {
        _context = context;
        _jwt = jwt;
        _google = google;
        _cola = cola;
        _correo = correo;
        _logger = logger;
    }

    // ── Registro con correo ──────────────────────────────────────────

    public async Task<VerificacionPendienteDto> RegistrarAsync(RegistroPacienteDto dto)
    {
        var correo = NormalizarCorreo(dto.CorreoElectronico);
        var nombre = dto.NombreCompleto?.Trim() ?? "";
        if (nombre.Length < 3) throw Falla("NombreCompleto", "Escribe tu nombre completo.");
        if (nombre.Length > 150) throw Falla("NombreCompleto", "El nombre es demasiado largo.");
        if (!Enum.IsDefined(dto.Sexo)) throw Falla("Sexo", "Elige una opción.");
        var telefono = dto.Telefono?.Trim() ?? "";
        if (telefono.Count(char.IsDigit) < 10) throw Falla("Telefono", "Escribe un teléfono de 10 dígitos.");
        NutricionistaService.ValidarContrasena(dto.Contrasena);

        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo);
        if (cuenta is { CorreoVerificado: true })
            throw new ConflictException("Ya existe una cuenta con este correo. Inicia sesión o recupera tu contraseña.");

        if (cuenta is null)
        {
            cuenta = new CuentaPaciente { Correo = correo };
            _context.CuentasPaciente.Add(cuenta);
        }

        // Un registro sin verificar se puede rehacer (p. ej. si se equivoco de datos).
        cuenta.NombreRegistro = nombre;
        cuenta.TelefonoRegistro = telefono;
        cuenta.SexoRegistro = dto.Sexo;
        cuenta.PasswordHash = _hasher.HashPassword(cuenta, dto.Contrasena);

        EnviarCodigo(cuenta, ParaVerificar, forzar: true);
        await _context.SaveChangesAsync();

        return new VerificacionPendienteDto(correo, $"Te enviamos un código de 6 dígitos a {correo}.");
    }

    public async Task<TokenPacienteDto> VerificarAsync(VerificarCodigoDto dto)
    {
        var correo = NormalizarCorreo(dto.CorreoElectronico);
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo)
                     ?? throw Falla("Codigo", "El código no es válido.");

        ValidarCodigo(cuenta, ParaVerificar, dto.Codigo);
        cuenta.CorreoVerificado = true;
        await VincularExpedienteAsync(cuenta);

        return await EmitirTokensAsync(cuenta);
    }

    public async Task ReenviarCodigoAsync(CorreoDto dto)
    {
        var correo = NormalizarCorreo(dto.CorreoElectronico);
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo);
        // Sin decir si existe o no: no se debe poder averiguar quien tiene cuenta.
        if (cuenta is null || cuenta.CorreoVerificado) return;

        EnviarCodigo(cuenta, ParaVerificar, forzar: false);
        await _context.SaveChangesAsync();
    }

    // ── Inicio de sesion ─────────────────────────────────────────────

    public async Task<TokenPacienteDto> LoginAsync(LoginPacienteDto dto)
    {
        var correo = NormalizarCorreo(dto.CorreoElectronico);
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo);

        if (cuenta?.PasswordHash is null || !VerificarContrasena(cuenta, dto.Contrasena ?? ""))
        {
            throw new UnauthorizedAccessException(
                cuenta is { PasswordHash: null, GoogleId: not null }
                    ? "Esta cuenta entra con Google. Usa «Continuar con Google»."
                    : "Correo o contraseña incorrectos.");
        }
        if (!cuenta.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta está desactivada. Comunícate con la clínica.");

        if (!cuenta.CorreoVerificado)
        {
            EnviarCodigo(cuenta, ParaVerificar, forzar: false);
            await _context.SaveChangesAsync();
            throw new CorreoNoVerificadoException(correo);
        }

        await VincularExpedienteAsync(cuenta);
        return await EmitirTokensAsync(cuenta);
    }

    public async Task<TokenPacienteDto> GoogleAsync(GooglePacienteDto dto)
    {
        if (_google.ClientIds.Length == 0)
            throw new ConflictException("El inicio de sesión con Google aún no está configurado en el servidor.");

        GoogleJsonWebSignature.Payload datos;
        try
        {
            datos = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = _google.ClientIds });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("ID token de Google rechazado: {Motivo}", ex.Message);
            throw new UnauthorizedAccessException("No pudimos verificar tu cuenta de Google. Intenta de nuevo.");
        }

        if (!datos.EmailVerified || string.IsNullOrWhiteSpace(datos.Email))
            throw new UnauthorizedAccessException("Tu cuenta de Google no tiene un correo verificado.");

        var correo = NormalizarCorreo(datos.Email);
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.GoogleId == datos.Subject)
                     ?? await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo);

        if (cuenta is null)
        {
            cuenta = new CuentaPaciente
            {
                Correo = correo,
                NombreRegistro = string.IsNullOrWhiteSpace(datos.Name) ? correo : datos.Name.Trim(),
            };
            _context.CuentasPaciente.Add(cuenta);
        }
        if (!cuenta.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta está desactivada. Comunícate con la clínica.");

        // Google ya verifico el correo: vale como verificacion.
        cuenta.GoogleId = datos.Subject;
        cuenta.CorreoVerificado = true;
        await VincularExpedienteAsync(cuenta);
        return await EmitirTokensAsync(cuenta);
    }

    public async Task<TokenPacienteDto?> RefreshAsync(string refreshToken)
    {
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.RefreshToken == refreshToken);
        if (cuenta is null) return null;

        if (cuenta.RefreshTokenExpiresAt is null || DateTime.UtcNow > cuenta.RefreshTokenExpiresAt || !cuenta.IsActive)
        {
            cuenta.RefreshToken = null;
            cuenta.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Tu sesión terminó. Inicia sesión de nuevo.");
        }

        return await EmitirTokensAsync(cuenta);
    }

    public async Task LogoutAsync(Guid cuentaId)
    {
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta is null) return;
        cuenta.RefreshToken = null;
        cuenta.RefreshTokenExpiresAt = null;
        await _context.SaveChangesAsync();
    }

    // ── Recuperar contraseña ─────────────────────────────────────────

    public async Task OlvideContrasenaAsync(CorreoDto dto)
    {
        var correo = NormalizarCorreo(dto.CorreoElectronico);
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo && c.IsActive);
        if (cuenta is null) return; // misma respuesta exista o no

        EnviarCodigo(cuenta, ParaRecuperar, forzar: false);
        await _context.SaveChangesAsync();
    }

    public async Task<TokenPacienteDto> RestablecerAsync(RestablecerContrasenaDto dto)
    {
        var correo = NormalizarCorreo(dto.CorreoElectronico);
        var cuenta = await _context.CuentasPaciente.FirstOrDefaultAsync(c => c.Correo == correo && c.IsActive)
                     ?? throw Falla("Codigo", "El código no es válido.");

        NutricionistaService.ValidarContrasena(dto.ContrasenaNueva);
        ValidarCodigo(cuenta, ParaRecuperar, dto.Codigo);

        cuenta.PasswordHash = _hasher.HashPassword(cuenta, dto.ContrasenaNueva);
        // Recibio el codigo en su correo: tambien queda verificado.
        cuenta.CorreoVerificado = true;
        await VincularExpedienteAsync(cuenta);
        return await EmitirTokensAsync(cuenta);
    }

    // ── Internos ─────────────────────────────────────────────────────

    /// <summary>
    /// Vincula la cuenta con el expediente que tenga su mismo correo; si no
    /// hay, crea uno nuevo marcado "por completar" (como la reserva en linea).
    /// </summary>
    private async Task VincularExpedienteAsync(CuentaPaciente cuenta)
    {
        if (cuenta.PacienteId is not null || !cuenta.CorreoVerificado) return;

        var vinculados = _context.CuentasPaciente.Where(c => c.PacienteId != null).Select(c => c.PacienteId);
        var paciente = await _context.Pacientes
            .Where(p => p.IsActive
                        && p.CorreoElectronico.ToLower() == cuenta.Correo
                        && !vinculados.Contains(p.Id))
            .OrderByDescending(p => p.Citas.Max(c => (DateTime?)c.FechaInicio) ?? p.FechaRegistro)
            .FirstOrDefaultAsync();

        if (paciente is null)
        {
            paciente = new Paciente
            {
                NombreCompleto = cuenta.NombreRegistro,
                CorreoElectronico = cuenta.Correo,
                Telefono = cuenta.TelefonoRegistro ?? "",
                Sexo = cuenta.SexoRegistro ?? Sexo.Otro,
                IsActive = true,
                DatosPorCompletar = true,
            };
            _context.Pacientes.Add(paciente);
            _logger.LogInformation("Expediente nuevo creado desde la app para {Correo}.", cuenta.Correo);
        }

        cuenta.PacienteId = paciente.Id;
        cuenta.Paciente = paciente;
    }

    private void EnviarCodigo(CuentaPaciente cuenta, string proposito, bool forzar)
    {
        if (!forzar && cuenta.CodigoEnviadoEn is { } enviado
            && cuenta.CodigoProposito == proposito
            && (DateTime.UtcNow - enviado).TotalSeconds < SegundosEntreCodigos)
        {
            return; // evita llenar el correo de codigos si tocan "reenviar" seguido
        }

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        cuenta.CodigoHash = HashCodigo(cuenta.Id, codigo);
        cuenta.CodigoProposito = proposito;
        cuenta.CodigoExpira = DateTime.UtcNow.AddMinutes(MinutosCodigo);
        cuenta.CodigoIntentos = 0;
        cuenta.CodigoEnviadoEn = DateTime.UtcNow;

        var recuperar = proposito == ParaRecuperar;
        var contenido = new PlantillaCorreo.Contenido
        {
            Titulo = recuperar ? "Recupera tu *contraseña*" : $"Hola, *{PrimerNombre(cuenta.NombreRegistro)}*",
            Introduccion = recuperar
                ? $"Usa este código en la app para crear una contraseña nueva. Vence en {MinutosCodigo} minutos."
                : $"Escribe este código en la app para activar tu cuenta. Vence en {MinutosCodigo} minutos.",
            Codigo = $"{codigo[..3]} {codigo[3..]}",
            Nota = "Si no fuiste tú, ignora este correo: nadie podrá entrar sin el código.",
            Pie = "NutriClínica\nRecibes este correo porque se usó tu dirección en la app de NutriClínica.",
            LogoUrl = Uri.TryCreate(_correo.UrlSitio, UriKind.Absolute, out var u)
                ? $"{u.ToString().TrimEnd('/')}/logo-correo.png"
                : null,
        };
        _cola.Encolar(new MensajeCorreo
        {
            Para = cuenta.Correo,
            ParaNombre = cuenta.NombreRegistro,
            Asunto = recuperar ? $"{codigo} es tu código para recuperar tu contraseña" : $"{codigo} es tu código de NutriClínica",
            Html = PlantillaCorreo.Html(contenido),
            Texto = PlantillaCorreo.TextoPlano(contenido),
        }, recuperar ? "CuentaRecuperar" : "CuentaVerificar", cuenta.Id);
    }

    private void ValidarCodigo(CuentaPaciente cuenta, string proposito, string? codigo)
    {
        var limpio = new string((codigo ?? "").Where(char.IsDigit).ToArray());
        if (cuenta.CodigoHash is null || cuenta.CodigoProposito != proposito
            || cuenta.CodigoExpira is null || DateTime.UtcNow > cuenta.CodigoExpira)
            throw Falla("Codigo", "El código venció. Pide uno nuevo.");

        if (cuenta.CodigoIntentos >= IntentosCodigo)
            throw Falla("Codigo", "Demasiados intentos. Pide un código nuevo.");

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(HashCodigo(cuenta.Id, limpio)),
                Encoding.ASCII.GetBytes(cuenta.CodigoHash)))
        {
            cuenta.CodigoIntentos++;
            _context.SaveChanges();
            throw Falla("Codigo", "El código no es correcto.");
        }

        cuenta.CodigoHash = null;
        cuenta.CodigoProposito = null;
        cuenta.CodigoExpira = null;
        cuenta.CodigoIntentos = 0;
    }

    private string HashCodigo(Guid cuentaId, string codigo) =>
        Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes($"codigo-paciente:{_jwt.Secret}"),
            Encoding.UTF8.GetBytes($"{cuentaId:N}:{codigo}")));

    private bool VerificarContrasena(CuentaPaciente cuenta, string contrasena)
    {
        var resultado = _hasher.VerifyHashedPassword(cuenta, cuenta.PasswordHash!, contrasena);
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            cuenta.PasswordHash = _hasher.HashPassword(cuenta, contrasena);
        return resultado != PasswordVerificationResult.Failed;
    }

    private async Task<TokenPacienteDto> EmitirTokensAsync(CuentaPaciente cuenta)
    {
        var paciente = cuenta.Paciente
                       ?? await _context.Pacientes.FirstAsync(p => p.Id == cuenta.PacienteId);
        var ahora = DateTime.UtcNow;
        var expiracion = ahora.AddHours(_jwt.ExpirationHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, cuenta.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, cuenta.Correo),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, cuenta.Id.ToString()),
            new(ClaimTypes.Role, Roles.Paciente),
            new(Roles.ClaimPacienteId, paciente.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: ahora,
            expires: expiracion,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)), SecurityAlgorithms.HmacSha256));

        cuenta.RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        cuenta.RefreshTokenExpiresAt = ahora.AddDays(_jwt.RefreshExpirationDays);
        cuenta.UltimoAcceso = ahora;
        await _context.SaveChangesAsync();

        return new TokenPacienteDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = cuenta.RefreshToken,
            ExpiraEnSegundos = (int)(expiracion - ahora).TotalSeconds,
            PacienteId = paciente.Id,
            NombreCompleto = paciente.NombreCompleto,
            CorreoElectronico = cuenta.Correo,
        };
    }

    private static string NormalizarCorreo(string? correo)
    {
        var limpio = (correo ?? "").Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(limpio, out var m) || m.Address != limpio || !limpio.Contains('.'))
            throw Falla("CorreoElectronico", "Escribe un correo válido.");
        return limpio;
    }

    private static string PrimerNombre(string nombre) =>
        nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;

    private static ValidationException Falla(string campo, string mensaje) => new([new FluentValidation.Results.ValidationFailure(campo, mensaje)]);
}

/// <summary>Contraseña correcta pero correo sin verificar: la app pide el codigo.</summary>
public class CorreoNoVerificadoException(string correo)
    : Exception("Verifica tu correo: te enviamos un código de 6 dígitos.")
{
    public string Correo { get; } = correo;
}
