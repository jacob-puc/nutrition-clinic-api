using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Citas.DTOs;

public class CrearCitaDto
{
    public Guid PacienteId { get; set; }
    public Guid NutricionistaId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Motivo { get; set; }
}
