using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Nutricionistas.DTOs;

public class NutricionistaRespuestaDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? NumeroColegiatura { get; set; }
    public string? Especialidad { get; set; }
    public RolUsuario Rol { get; set; }
    public bool DebeCambiarContrasena { get; set; }
    public bool IsActive { get; set; }
    public DateTime FechaRegistro { get; set; }
}
