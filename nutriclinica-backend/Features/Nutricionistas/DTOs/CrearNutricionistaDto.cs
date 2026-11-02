using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Nutricionistas.DTOs;

public class CrearNutricionistaDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? NumeroColegiatura { get; set; }
    public string? Especialidad { get; set; }
    public RolUsuario Rol { get; set; } = RolUsuario.Nutricionista;

    /// <summary>Contraseña temporal: la cuenta debe cambiarla en su primer inicio de sesion.</summary>
    public string Contrasena { get; set; } = string.Empty;
}
