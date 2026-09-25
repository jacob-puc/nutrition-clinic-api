namespace nutriclinica_backend.Features.HistorialesClinicos.DTOs;

public class CrearHistorialClinicoDto
{
    public List<string> Alergias { get; set; } = new();
    public List<string> AlimentosFavoritos { get; set; } = new();
    public List<string> AlimentosNoFavoritos { get; set; } = new();
}