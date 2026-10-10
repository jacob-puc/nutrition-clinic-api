namespace nutriclinica_backend.Core.Entities;

public class DetallePlato
{
    public Guid Id { get; set; }
    public Guid PlatoId { get; set; }
    public Guid AlimentoId { get; set; }

    public decimal RacionGramos { get; set; }
    public int Orden { get; set; }

    public virtual Plato Plato { get; set; } = null!;
    public virtual Alimento Alimento { get; set; } = null!;
}
