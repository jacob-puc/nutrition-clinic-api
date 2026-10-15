using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class ActualizarMenuAsignadoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public MenuAsignadoEstado Estado { get; set; }
    public List<ActualizarDetallesMenuAsignadoDto> Detalles { get; set; }
}