using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Nutricionista
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? NumeroColegiatura { get; set; }
    public string? Especialidad { get; set; }
    public RolUsuario Rol { get; set; } = RolUsuario.Nutricionista;
    public bool IsActive { get; set; } = true;

    /// <summary>False: atiende en el horario general de la clinica.</summary>
    public bool HorarioPropio { get; set; }
    /// <summary>Duracion que se propone al agendarle una cita.</summary>
    public int DuracionConsultaMinutos { get; set; } = 60;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>La contraseña la puso un administrador: se pide cambiarla al entrar.</summary>
    public bool DebeCambiarContrasena { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public virtual ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
    public virtual ICollection<HorarioAtencion> Horarios { get; set; } = new List<HorarioAtencion>();
    public virtual ICollection<Ausencia> Ausencias { get; set; } = new List<Ausencia>();
}
