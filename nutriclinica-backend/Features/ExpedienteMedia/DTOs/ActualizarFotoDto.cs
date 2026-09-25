using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.ExpedienteMedia.DTOs;

public class ActualizarFotoDto
{
    public string UrlFoto { get; set; } = string.Empty;
    public TipoFoto Tipo { get; set; } = TipoFoto.Frente;
    public string? Notas { get; set; }
}