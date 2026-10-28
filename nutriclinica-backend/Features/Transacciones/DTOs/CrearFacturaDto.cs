using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs;

public class CrearFacturaDto
{
    public Guid PagoId { get; set; }
    public TipoPersona TipoPersona { get; set; }
    public string RFC { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public RegimenFiscal RegimenFiscal { get; set; }
    public UsoCfdi UsoCfdi { get; set; }
    public string CodigoPostal { get; set; } = string.Empty;
}
