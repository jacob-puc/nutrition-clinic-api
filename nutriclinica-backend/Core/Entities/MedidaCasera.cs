namespace nutriclinica_backend.Core.Entities;

/// <summary>
/// Medida casera de un alimento: "pieza" = 50 g. Va por alimento porque una
/// taza de arroz no pesa lo mismo que una de leche.
/// </summary>
public class MedidaCasera
{
    public Guid Id { get; set; }
    public Guid AlimentoId { get; set; }

    /// <summary>En singular: "pieza", "taza", "cucharada".</summary>
    public string Nombre { get; set; } = string.Empty;
    public decimal Gramos { get; set; }
    public int Orden { get; set; }

    public virtual Alimento Alimento { get; set; } = null!;
}
