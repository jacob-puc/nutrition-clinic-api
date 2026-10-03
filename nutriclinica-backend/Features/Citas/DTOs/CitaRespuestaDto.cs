using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Citas.DTOs;

public class CitaRespuestaDto
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;

    public Guid NutricionistaId { get; set; }
    public string NutricionistaNombre { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public EstadoCita Estado { get; set; }

    public string? Motivo { get; set; }

    public Guid? ConsultaId { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoCancelacion { get; set; }
}
