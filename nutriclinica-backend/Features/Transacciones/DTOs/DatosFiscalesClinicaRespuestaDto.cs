using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs;

public record DatosFiscalesClinicaRespuestaDto(
    Guid Id,
    string RazonSocial,
    string RFC,
    RegimenFiscal RegimenFiscal,
    string CodigoPostal,
    string Direccion,
    string? Telefono,
    string? Correo,
    string? LogoUrl,
    string Serie,
    decimal TarifaIva,
    DateTime FechaActualizacion);
