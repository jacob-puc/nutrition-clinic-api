namespace nutriclinica_backend.Features.Menus.DTOs;

public class PlatoFiltroDto
{
    public string? Buscar { get; set; }
    public Guid? NutricionistaId { get; set; }
    public bool? SoloCompuestos { get; set; }
    public bool? SoloActivos { get; set; } = true;
    public string? OrdenarPor { get; set; }
    public bool Descendente { get; set; }
}
