namespace nutriclinica_backend.Features.Nutricionistas.DTOs;

public class ReasignarCitasDto
{
    public Guid NutricionistaDestinoId { get; set; }

    /// <summary>Opcional: solo las citas de esos dias (hora de la clinica), p. ej. una ausencia.</summary>
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
}

public class CitaPendienteDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class ResultadoReasignacionDto
{
    public int Reasignadas { get; set; }

    /// <summary>Citas que chocan con la agenda o el horario del destino: se quedan sin mover.</summary>
    public List<CitaPendienteDto> Conflictos { get; set; } = [];
}
