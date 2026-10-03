using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Pacientes.DTOs;

public class PacienteRespuestaDto
{
    public Guid Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public DateOnly? FechaNacimiento { get; set; }
    public int? Edad { get; set; }

    public Sexo Sexo { get; set; }

    public string? TituloObjetivo { get; set; }
    public decimal? PesoObjetivo { get; set; }

    public DateTime FechaRegistro { get; set; }
}