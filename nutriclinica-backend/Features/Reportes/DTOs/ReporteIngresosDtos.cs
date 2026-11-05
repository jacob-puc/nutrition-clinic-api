using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Reportes.DTOs;

public class ReporteIngresosFiltroDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public Guid? NutricionistaId { get; set; }
}

public enum AgrupacionReporte
{
    Dia,
    Semana,
    Mes
}

/// <param name="Clave">Primer dia del tramo en hora de la clinica (yyyy-MM-dd).</param>
public record PuntoIngresosDto(string Clave, decimal Total, int Cobros);

public record IngresoPorMetodoDto(MetodoPago Metodo, decimal Total, int Cobros);

public record IngresoPorNutricionistaDto(Guid NutricionistaId, string Nombre, decimal Total, int Cobros);

public record IngresoPorTipoDto(TipoConsulta Tipo, decimal Total, int Cobros);

/// <summary>
/// Ingresos de un periodo. Solo suman los pagos vigentes; los anulados se
/// reportan aparte para que el total cuadre con el corte de cobros.
/// </summary>
public record ReporteIngresosDto(
    DateTime Desde,
    DateTime Hasta,
    AgrupacionReporte Agrupacion,
    decimal Total,
    int Cobros,
    decimal TicketPromedio,
    int Anulados,
    decimal MontoAnulado,
    int ConsultasAtendidas,
    int ConsultasPorCobrar,
    /* Periodo anterior de la misma duracion, para comparar. */
    decimal TotalAnterior,
    int CobrosAnterior,
    List<PuntoIngresosDto> Serie,
    List<IngresoPorMetodoDto> PorMetodo,
    List<IngresoPorNutricionistaDto> PorNutricionista,
    List<IngresoPorTipoDto> PorTipoConsulta);
