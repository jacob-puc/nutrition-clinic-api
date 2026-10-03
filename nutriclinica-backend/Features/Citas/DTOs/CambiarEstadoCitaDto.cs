using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Citas.DTOs;

public class CambiarEstadoCitaDto
{
    public EstadoCita Estado { get; set; }
    public string? Motivo { get; set; }
}
