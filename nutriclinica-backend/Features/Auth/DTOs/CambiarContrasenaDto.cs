namespace nutriclinica_backend.Features.Auth.DTOs;

public class CambiarContrasenaDto
{
    public string ContrasenaActual { get; set; } = string.Empty;
    public string ContrasenaNueva { get; set; } = string.Empty;
}
