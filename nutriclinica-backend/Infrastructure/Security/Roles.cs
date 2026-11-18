using System.Security.Claims;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Infrastructure.Security;

/// <summary>
/// Nombres de rol para [Authorize(Roles = ...)]. Coinciden con RolUsuario,
/// que es lo que viaja en el claim de rol del token.
/// </summary>
public static class Roles
{
    public const string Administrador = nameof(RolUsuario.Administrador);
    public const string Nutricionista = nameof(RolUsuario.Nutricionista);
    public const string Recepcion = nameof(RolUsuario.Recepcion);

    /// <summary>Quienes atienden pacientes y pueden ver informacion clinica.</summary>
    public const string Clinicos = Administrador + "," + Nutricionista;

    /// <summary>Todo el personal de la clinica (cualquier cuenta del equipo).</summary>
    public const string Personal = Administrador + "," + Nutricionista + "," + Recepcion;

    /// <summary>
    /// Cuenta de paciente (app movil). No es un RolUsuario: los pacientes no
    /// son personal y solo tienen acceso a sus propios datos en /api/mi.
    /// </summary>
    public const string Paciente = "Paciente";

    /// <summary>Claim con el Id del expediente del paciente en tokens de paciente.</summary>
    public const string ClaimPacienteId = "paciente_id";

    /// <summary>Claim que marca un token emitido con contraseña temporal.</summary>
    public const string ClaimCambiarContrasena = "cambiar_contrasena";
}

public static class UsuarioActualExtensions
{
    public static Guid ObtenerId(this ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public static bool EsAdministrador(this ClaimsPrincipal usuario) =>
        usuario.IsInRole(Roles.Administrador);

    public static bool EsRecepcion(this ClaimsPrincipal usuario) =>
        usuario.IsInRole(Roles.Recepcion);

    /// <summary>Expediente del paciente en sesion (solo tokens de paciente).</summary>
    public static Guid ObtenerPacienteId(this ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirst(Roles.ClaimPacienteId)!.Value);
}
