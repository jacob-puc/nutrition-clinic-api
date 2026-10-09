namespace nutriclinica_backend.Features.Alimentos.DTOs;

public class AlimentoFiltroDto
{
    public string? Buscar { get; set; }
    public decimal? CaloriasMinimas { get; set; }
    public decimal? CaloriasMaximas { get; set; }
    public bool? SoloActivos { get; set; } = true;
    public string? OrdenarPor { get; set; }
    public bool Descendente { get; set; }
}
