using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs;

public class SubirDocumentoDto
{
    public IFormFile Archivo { get; set; } = null!;
    public Guid? ConsultaId { get; set; }
    public TipoDocumento Tipo { get; set; } = TipoDocumento.AnalisisLaboratorio;
    public string? Observaciones { get; set; }
}
