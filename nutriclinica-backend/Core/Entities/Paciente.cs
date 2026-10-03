using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Paciente
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;

    public DateOnly? FechaNacimiento { get; set; }
    public Sexo Sexo { get; set; }
    public string? TituloObjetivo { get; set; }
    public decimal? PesoObjetivo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public virtual HistorialClinico? HistorialClinico { get; set; }
    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public virtual ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}