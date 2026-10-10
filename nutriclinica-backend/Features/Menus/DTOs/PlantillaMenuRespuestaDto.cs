using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class ResumenTiempoComidaDto
{
    public DiaSemana DiaSemana { get; set; }
    public TiempoComida TiempoComida { get; set; }
    public decimal Calorias { get; set; }
    public decimal Proteinas { get; set; }
    public decimal Carbohidratos { get; set; }
    public decimal Grasas { get; set; }
}


public class PlantillaMenuRespuestaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }
    public Guid? NutricionistaId { get; set; }
    public string? NutricionistaNombre { get; set; }

    public List<DetallePlantillaMenuRespuestaDto> Detalles { get; set; } = [];

    public decimal CaloriasTotales { get; set; }
    public decimal ProteinasTotales { get; set; }
    public decimal CarbohidratosTotales { get; set; }
    public decimal GrasasTotales { get; set; }

    public decimal? DesviacionCalorica { get; set; }
    public List<ResumenTiempoComidaDto> ResumenPorTiempoComida { get; set; } = [];

    public DateTime FechaCreacion { get; set; }
    public bool IsActive { get; set; }
}
