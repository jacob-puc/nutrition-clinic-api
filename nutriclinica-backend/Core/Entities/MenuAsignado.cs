using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class MenuAsignado
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? PlantillaMenuId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }

    public DateOnly FechaInicio { get; set; }
    public MenuAsignadoEstado Estado { get; set; } = MenuAsignadoEstado.Borrador;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual Paciente Paciente { get; set; } = null!;
    public virtual PlantillaMenu? PlantillaMenu { get; set; }
    public virtual ICollection<DetalleMenuAsignado> Detalles { get; set; } = new List<DetalleMenuAsignado>();
}
