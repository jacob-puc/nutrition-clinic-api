using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs;

public class FacturaFiltroDto
{
    public EstatusFactura? Estatus { get; set; }
    /// <summary>Facturas de los pagos de un paciente (para precargar sus datos fiscales).</summary>
    public Guid? PacienteId { get; set; }
    public string? RFC { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
