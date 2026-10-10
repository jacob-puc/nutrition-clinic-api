namespace nutriclinica_backend.Features.Menus.DTOs;

public class DetallePlatoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid AlimentoId { get; set; }
    public string AlimentoNombre { get; set; } = string.Empty;
    public decimal RacionGramos { get; set; }
    public int Orden { get; set; }

    public decimal Calorias { get; set; }
    public decimal Proteinas { get; set; }
    public decimal Carbohidratos { get; set; }
    public decimal Grasas { get; set; }
}
