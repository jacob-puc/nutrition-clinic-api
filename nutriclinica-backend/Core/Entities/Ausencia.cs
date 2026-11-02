namespace nutriclinica_backend.Core.Entities;

/// <summary>Dias completos en que alguien no atiende (vacaciones, congreso, incapacidad).</summary>
public class Ausencia
{
    public Guid Id { get; set; }
    public Guid NutricionistaId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public virtual Nutricionista Nutricionista { get; set; } = null!;
}
