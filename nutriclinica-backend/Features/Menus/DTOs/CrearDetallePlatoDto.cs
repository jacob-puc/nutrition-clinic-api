namespace nutriclinica_backend.Features.Menus.DTOs;

public class CrearDetallePlatoDto
{
    public Guid? Id { get; set; }
    public Guid AlimentoId { get; set; }
    public decimal RacionGramos { get; set; }
    public int Orden { get; set; }
}
