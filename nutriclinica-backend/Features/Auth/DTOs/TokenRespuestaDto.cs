namespace nutriclinica_backend.Features.Auth.DTOs;

public class TokenRespuestaDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiraEnSegundos { get; set; }
    public Guid NutricionistaId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
}
