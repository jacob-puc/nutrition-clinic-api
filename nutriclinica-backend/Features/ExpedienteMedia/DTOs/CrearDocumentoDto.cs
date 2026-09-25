using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs;

public class CrearDocumentoDto
{
    public Guid? CitaId { get; set; }
    public string NombreDocumento { get; set; } = string.Empty;
    public string UrlDocumento { get; set; } = string.Empty;
    public TipoDocumento Tipo { get; set; } = TipoDocumento.AnalisisLaboratorio;
    public string? Observaciones { get; set; }
}