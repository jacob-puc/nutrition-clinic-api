namespace nutriclinica_backend.Core.Entities;

public class HistorialClinico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;

    public List<string> Alergias { get; set; } = new();
    public List<string> AlimentosFavoritos { get; set; } = new();
    public List<string> AlimentosNoFavoritos { get; set; } = new();
}