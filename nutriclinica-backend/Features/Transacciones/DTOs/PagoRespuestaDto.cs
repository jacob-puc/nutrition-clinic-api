using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs;

public record PagoRespuestaDto(
    Guid Id,
    Guid ConsultaId,
    Guid PacienteId,
    string PacienteNombre,
    decimal Monto,
    string Moneda,
    MetodoPago MetodoPago,
    EstadoPago Estado,
    string NumeroRecibo,
    DateTime FechaPago,
    string? Notas,
    DateTime FechaCreacion);
