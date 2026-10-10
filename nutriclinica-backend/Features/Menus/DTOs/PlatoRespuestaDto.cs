namespace nutriclinica_backend.Features.Menus.DTOs;

public class PlatoRespuestaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public Guid? NutricionistaId { get; set; }
    public string? NutricionistaNombre { get; set; }

    public bool EsCompuesto { get; set; }
    public List<DetallePlatoRespuestaDto> Detalles { get; set; } = [];

    public decimal CaloriasTotales { get; set; }
    public decimal ProteinasTotales { get; set; }
    public decimal CarbohidratosTotales { get; set; }
    public decimal GrasasTotales { get; set; }

    public DateTime FechaCreacion { get; set; }
    public bool IsActive { get; set; }
}
