using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class FotoSeguimiento
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    
    public Guid? ConsultaId { get; set; }
    public virtual Consulta? Consulta { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;
    
    public string? UrlFoto { get; set; }
    public TipoFoto Tipo { get; set; } = TipoFoto.Frente;
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    public string? Notas { get; set; }
    
}