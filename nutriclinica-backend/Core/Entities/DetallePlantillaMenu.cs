using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class DetallePlantillaMenu
{
    public Guid Id { get; set; }
    public Guid PlantillaMenuId { get; set; }
    public Guid PlatoId { get; set; }

    public DiaSemana DiaSemana { get; set; }
    public TiempoComida TiempoComida { get; set; }
    public decimal Cantidad { get; set; } = 1;
    public string? Indicaciones { get; set; }
    public int Orden { get; set; }

    public virtual PlantillaMenu PlantillaMenu { get; set; } = null!;
    public virtual Plato Plato { get; set; } = null!;
}
