using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Citas.DTOs;

public class CitaFiltroDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public Guid? NutricionistaId { get; set; }
    public Guid? PacienteId { get; set; }
    public EstadoCita? Estado { get; set; }
}
