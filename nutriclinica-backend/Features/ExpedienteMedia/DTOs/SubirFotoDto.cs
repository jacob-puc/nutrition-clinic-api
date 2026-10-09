using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs;

public class SubirFotoDto
{
    public IFormFile Archivo { get; set; } = null!;
    public Guid? ConsultaId { get; set; }
    public TipoFoto Tipo { get; set; } = TipoFoto.Frente;
    public string? Notas { get; set; }
}
