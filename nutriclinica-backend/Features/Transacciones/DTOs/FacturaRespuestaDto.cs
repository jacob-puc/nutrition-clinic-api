using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs;

public record FacturaRespuestaDto(
    Guid Id,
    Guid PagoId,
    TipoPersona TipoPersona,
    string RFC,
    string NombreRazonSocial,
    RegimenFiscal RegimenFiscal,
    UsoCfdi UsoCfdi,
    string CodigoPostal,
    string Serie,
    int Folio,
    DateTime FechaEmision,
    string? UUID,
    decimal Subtotal,
    decimal TarifaIva,
    decimal Iva,
    decimal Total,
    EstatusFactura Estatus,
    DateTime FechaCreacion);
