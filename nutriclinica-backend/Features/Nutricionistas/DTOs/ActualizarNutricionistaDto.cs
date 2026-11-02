using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Nutricionistas.DTOs;

public class ActualizarNutricionistaDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? NumeroColegiatura { get; set; }
    public string? Especialidad { get; set; }

    /// <summary>Solo un administrador puede cambiarlo, y nunca el suyo. Null lo deja igual.</summary>
    public RolUsuario? Rol { get; set; }
}
