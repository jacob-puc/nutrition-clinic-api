using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class CrearMenuAsignadoDto
{
    public Guid PacienteId { get; set; }
    public Guid PlantillaMenuId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }
    public DateOnly FechaInicio { get; set; }
    public MenuAsignadoEstado Estado { get; set; } = MenuAsignadoEstado.Borrador;
}
