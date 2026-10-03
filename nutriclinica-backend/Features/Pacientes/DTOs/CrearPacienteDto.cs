using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Pacientes.DTOs;

public class CrearPacienteDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public DateOnly? FechaNacimiento { get; set; }
    public Sexo Sexo { get; set; }
}