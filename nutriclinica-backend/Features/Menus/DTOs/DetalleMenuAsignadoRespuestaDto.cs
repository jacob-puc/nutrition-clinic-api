using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class DetalleMenuAsignadoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid PlatoId { get; set; }
    public string PlatoNombre { get; set; } = string.Empty;

    public DiaSemana DiaSemana { get; set; }
    public TiempoComida TiempoComida { get; set; }
    public decimal Cantidad { get; set; }
    public string? Indicaciones { get; set; }
    public int Orden { get; set; }

    public decimal Calorias { get; set; }
    public decimal Proteinas { get; set; }
    public decimal Carbohidratos { get; set; }
    public decimal Grasas { get; set; }
}
