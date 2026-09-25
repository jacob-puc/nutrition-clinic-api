namespace nutriclinica_backend.Features.HistorialesClinicos.DTOs;

public class HistorialClinicoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public List<string> Alergias { get; set; } = new();
    public List<string> AlimentosFavoritos { get; set; } = new();
    public List<string> AlimentosNoFavoritos { get; set; } = new();

}