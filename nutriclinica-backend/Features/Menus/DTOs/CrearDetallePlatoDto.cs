namespace nutriclinica_backend.Features.Menus.DTOs;

public class CrearDetallePlatoDto
{
    public Guid? Id { get; set; }
    public Guid AlimentoId { get; set; }
    /// <summary>Siempre en gramos, tambien si se capturo en medida casera.</summary>
    public decimal RacionGramos { get; set; }
    public decimal? MedidaCantidad { get; set; }
    public string? MedidaNombre { get; set; }
    public int Orden { get; set; }
}
