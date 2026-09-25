using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class DocumentoPaciente
{
    public  Guid Id { get; set; }
    
    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;
    
    public Guid? CitaId { get; set; }
    
    public string NombreDocumento { get; set; } = string.Empty;
    public string UrlDocumento { get; set; } = string.Empty;
    public TipoDocumento Tipo { get; set; } = TipoDocumento.AnalisisLaboratorio;
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }
    
}