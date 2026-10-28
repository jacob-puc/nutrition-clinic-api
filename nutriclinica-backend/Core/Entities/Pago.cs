using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Pago
{
    public Guid Id { get; set; }

    public Guid ConsultaId { get; set; }
    public virtual Consulta Consulta { get; set; } = null!;

    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;

    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "MXN";
    public MetodoPago MetodoPago { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Pagado;
    public string NumeroRecibo { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
