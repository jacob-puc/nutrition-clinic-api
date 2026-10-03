using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Consultas.DTOs;

public class CrearConsultaDto
{
    public Guid PacienteId { get; set; }

    public Guid? CitaId { get; set; }

    public Guid? NutricionistaId { get; set; }

    public TipoConsulta TipoConsulta { get; set; } = TipoConsulta.Inicial;

    public DateTime? FechaInicio { get; set; }

    public string? NotasClinicas { get; set; }
}
