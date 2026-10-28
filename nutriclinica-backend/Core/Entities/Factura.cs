using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Factura
{
    public Guid Id { get; set; }

    public Guid PagoId { get; set; }
    public virtual Pago Pago { get; set; } = null!;

    public TipoPersona TipoPersona { get; set; }
    public string RFC { get; set; } = string.Empty;
    public string NombreRazonSocial { get; set; } = string.Empty;
    public RegimenFiscal RegimenFiscal { get; set; }
    public UsoCfdi UsoCfdi { get; set; }
    public string CodigoPostal { get; set; } = string.Empty;

    public string Serie { get; set; } = "A";
    public int Folio { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? UUID { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TarifaIva { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }

    public EstatusFactura Estatus { get; set; } = EstatusFactura.Emitida;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
