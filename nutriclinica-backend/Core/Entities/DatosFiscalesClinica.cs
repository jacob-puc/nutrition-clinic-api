using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class DatosFiscalesClinica
{
    public Guid Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;
    public string RFC { get; set; } = string.Empty;
    public RegimenFiscal RegimenFiscal { get; set; }           
    public string CodigoPostal { get; set; } = string.Empty;   
    public string Direccion { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? LogoUrl { get; set; }

    public string Serie { get; set; } = "A";                   
    public decimal TarifaIva { get; set; } = 16m;              

    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

}