namespace nutriclinica_backend.Core.Entities;

public class DetallePlato
{
    public Guid Id { get; set; }
    public Guid PlatoId { get; set; }
    public Guid AlimentoId { get; set; }

    /// <summary>Siempre en gramos: de aqui salen calorias y macros.</summary>
    public decimal RacionGramos { get; set; }

    /* Como se capturo, si fue en medida casera ("2" + "pieza"). Es una copia
       y no una referencia: editar la medida del alimento no cambia platos ya
       armados. Null = capturado en gramos. */
    public decimal? MedidaCantidad { get; set; }
    public string? MedidaNombre { get; set; }
    public int Orden { get; set; }

    public virtual Plato Plato { get; set; } = null!;
    public virtual Alimento Alimento { get; set; } = null!;
}
