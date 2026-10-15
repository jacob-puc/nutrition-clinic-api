using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Menus.DTOs;

public class MenuAsignadoFiltroDto
{
    public Guid? PacienteId { get; set; }
    public MenuAsignadoEstado? Estado { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool? SoloActivos { get; set; } = true;
    public string? OrdenarPor { get; set; }
    public bool Descendente { get; set; }
}