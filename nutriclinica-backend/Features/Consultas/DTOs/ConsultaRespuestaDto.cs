using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Consultas.DTOs;

public class ConsultaRespuestaDto
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;

    public Guid? CitaId { get; set; }

    public Guid NutricionistaId { get; set; }
    public string NutricionistaNombre { get; set; } = string.Empty;

    public TipoConsulta TipoConsulta { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public string? NotasClinicas { get; set; }
    public DateTime FechaCreacion { get; set; }
}
