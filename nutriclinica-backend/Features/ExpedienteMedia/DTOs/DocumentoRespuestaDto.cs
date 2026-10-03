using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs;

public class DocumentoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public string NombreDocumento { get; set; } = string.Empty;
    public string UrlDocumento { get; set; } = string.Empty;
    public TipoDocumento Tipo { get; set; }
    public DateTime FechaSubida { get; set; }
    public string? Observaciones { get; set; }
}