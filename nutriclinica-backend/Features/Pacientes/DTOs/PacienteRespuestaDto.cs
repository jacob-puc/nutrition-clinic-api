namespace nutriclinica_backend.Features.Pacientes.DTOs;

public class PacienteRespuestaDto
{
    public Guid Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public int? Edad { get; set; }

    public string Sexo { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }
}