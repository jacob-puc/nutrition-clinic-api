using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs;

public class PagoFiltroDto
{
    public Guid? PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public EstadoPago? Estado { get; set; }
    public MetodoPago? MetodoPago { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
