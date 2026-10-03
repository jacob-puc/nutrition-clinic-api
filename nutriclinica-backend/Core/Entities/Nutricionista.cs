namespace nutriclinica_backend.Core.Entities;

public class Nutricionista
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? NumeroColegiatura { get; set; }
    public string? Especialidad { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public string PasswordHash { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public virtual ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}
