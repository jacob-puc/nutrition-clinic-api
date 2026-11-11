namespace nutriclinica_backend.Core.Entities;

public class HistorialClinico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;

    /// <summary>Condiciones de salud a considerar en el plan (diabetes, hipertension...).</summary>
    public List<string> Enfermedades { get; set; } = new();
    public List<string> Alergias { get; set; } = new();
    public List<string> AlimentosFavoritos { get; set; } = new();
    public List<string> AlimentosNoFavoritos { get; set; } = new();
}