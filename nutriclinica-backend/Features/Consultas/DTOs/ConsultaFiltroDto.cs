using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Consultas.DTOs;

public class ConsultaFiltroDto
{
    public Guid? PacienteId { get; set; }
    public Guid? NutricionistaId { get; set; }
    public Guid? CitaId { get; set; }
    public TipoConsulta? TipoConsulta { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public bool? SoloSinCierre { get; set; }
}
