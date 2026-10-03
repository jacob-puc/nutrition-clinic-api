namespace nutriclinica_backend.Features.Nutricionistas.DTOs;

public class CrearNutricionistaDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? NumeroColegiatura { get; set; }
    public string? Especialidad { get; set; }
}
