namespace nutriclinica_backend.Core.Entities;

public class Plato
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }

    public Guid? NutricionistaId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual Nutricionista? Nutricionista { get; set; }
    public virtual ICollection<DetallePlato> Detalles { get; set; } = new List<DetallePlato>();
}
