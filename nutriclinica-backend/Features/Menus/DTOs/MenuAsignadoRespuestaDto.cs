using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class MenuAsignadoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;

    public Guid? PlantillaMenuId { get; set; }
    public string? PlantillaMenuNombre { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }

    public DateOnly FechaInicio { get; set; }
    public MenuAsignadoEstado Estado { get; set; }

    public List<DetalleMenuAsignadoRespuestaDto> Detalles { get; set; } = [];

    public decimal CaloriasTotales { get; set; }
    public decimal ProteinasTotales { get; set; }
    public decimal CarbohidratosTotales { get; set; }
    public decimal GrasasTotales { get; set; }

    public decimal? DesviacionCalorica { get; set; }
    public List<ResumenTiempoComidaDto> ResumenPorTiempoComida { get; set; } = [];

    public DateTime FechaCreacion { get; set; }
    public bool IsActive { get; set; }
}
