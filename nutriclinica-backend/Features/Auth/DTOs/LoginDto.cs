namespace nutriclinica_backend.Features.Auth.DTOs;

public class LoginDto
{
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}
