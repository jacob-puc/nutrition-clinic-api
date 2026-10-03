using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Consulta
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;

    public Guid? CitaId { get; set; }
    public virtual Cita? Cita { get; set; }

    public Guid NutricionistaId { get; set; }
    public virtual Nutricionista Nutricionista { get; set; } = null!;

    public TipoConsulta TipoConsulta { get; set; } = TipoConsulta.Inicial;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public string? NotasClinicas { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
