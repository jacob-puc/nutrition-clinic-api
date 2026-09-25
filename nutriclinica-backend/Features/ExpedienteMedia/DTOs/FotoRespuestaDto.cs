using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs;

public class FotoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? CitaId { get; set; }
    public string UrlFoto { get; set; } = string.Empty;
    public TipoFoto Tipo { get; set; }
    public DateTime FechaSubida { get; set; }
    public string? Notas { get; set; }
}