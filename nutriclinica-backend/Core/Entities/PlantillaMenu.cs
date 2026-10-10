namespace nutriclinica_backend.Core.Entities;

public class PlantillaMenu
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }

    public Guid? NutricionistaId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual Nutricionista? Nutricionista { get; set; }
    public virtual ICollection<DetallePlantillaMenu> Detalles { get; set; } = new List<DetallePlantillaMenu>();
}
