using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class PlantillaMenuFiltroDto
{
    public string? Buscar { get; set; }
    public Guid? NutricionistaId { get; set; }
    public decimal? ObjetivoCaloricoMinimo { get; set; }
    public decimal? ObjetivoCaloricoMaximo { get; set; }
    public DiaSemana? DiaSemana { get; set; }
    public TiempoComida? TiempoComida { get; set; }
    public bool? SoloActivos { get; set; } = true;
    public string? OrdenarPor { get; set; }
    public bool Descendente { get; set; }
}
