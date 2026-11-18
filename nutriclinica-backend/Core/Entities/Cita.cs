using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Cita
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;

    public Guid NutricionistaId { get; set; }
    public virtual Nutricionista Nutricionista { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public EstadoCita Estado { get; set; } = EstadoCita.Programada;

    public string? Motivo { get; set; }

    public CanalCita? Canal { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoCancelacion { get; set; }

    /// <summary>SEQUENCE de la invitacion de calendario: sube en cada cambio avisado.</summary>
    public int SecuenciaCalendario { get; set; }

    /// <summary>
    /// Inicio de la cita para el que ya se mando el recordatorio. Si la cita
    /// se mueve, deja de coincidir y se recuerda la nueva hora.
    /// </summary>
    public DateTime? RecordatorioEnviadoPara { get; set; }

    public virtual Consulta? Consulta { get; set; }
}
