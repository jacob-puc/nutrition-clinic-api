using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Reportes.DTOs;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Reportes.Services;

public interface IReporteIngresosService
{
    Task<ReporteIngresosDto> ObtenerAsync(ReporteIngresosFiltroDto filtro);
}

public class ReporteIngresosService : IReporteIngresosService
{
    private const int DiasMaximos = 366 * 3;
    private const int DiasPorDia = 31;
    private const int DiasPorSemana = 182;

    private readonly ApplicationDbContext _context;

    public ReporteIngresosService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReporteIngresosDto> ObtenerAsync(ReporteIngresosFiltroDto filtro)
    {
        var (desde, hasta) = Validar(filtro);

        /* Mismo largo justo antes del periodo pedido. */
        var hastaAnterior = desde.AddTicks(-1);
        var desdeAnterior = hastaAnterior - (hasta - desde);

        var pagosQuery = _context.Pagos.AsNoTracking()
            .Where(p => p.FechaPago >= desdeAnterior && p.FechaPago <= hasta);

        if (filtro.NutricionistaId.HasValue)
        {
            pagosQuery = pagosQuery.Where(p => p.Consulta.NutricionistaId == filtro.NutricionistaId.Value);
        }

        var pagos = await pagosQuery
            .Select(p => new
            {
                p.Monto,
                p.MetodoPago,
                p.Estado,
                p.FechaPago,
                p.Consulta.NutricionistaId,
                Nutricionista = p.Consulta.Nutricionista.NombreCompleto,
                p.Consulta.TipoConsulta,
            })
            .ToListAsync();

        var delPeriodo = pagos.Where(p => p.FechaPago >= desde).ToList();
        var vigentes = delPeriodo.Where(p => p.Estado == EstadoPago.Pagado).ToList();
        var anulados = delPeriodo.Where(p => p.Estado == EstadoPago.Anulado).ToList();
        var anteriores = pagos
            .Where(p => p.FechaPago < desde && p.Estado == EstadoPago.Pagado)
            .ToList();

        var consultasQuery = _context.Consultas.AsNoTracking()
            .Where(c => c.FechaFin >= desde && c.FechaFin <= hasta);

        if (filtro.NutricionistaId.HasValue)
        {
            consultasQuery = consultasQuery.Where(c => c.NutricionistaId == filtro.NutricionistaId.Value);
        }

        var consultasAtendidas = await consultasQuery.CountAsync();
        var consultasPorCobrar = await consultasQuery.CountAsync(c =>
            !_context.Pagos.Any(p => p.ConsultaId == c.Id && p.Estado == EstadoPago.Pagado));

        var total = vigentes.Sum(p => p.Monto);

        var primerDia = ZonaClinica.ALocal(desde).Date;
        var ultimoDia = ZonaClinica.ALocal(hasta).Date;
        var agrupacion = ElegirAgrupacion((ultimoDia - primerDia).Days + 1);

        /* Se rellenan los tramos sin cobros: un dia en cero tambien es un dato. */
        var tramos = new SortedDictionary<DateTime, (decimal Total, int Cobros)>();
        for (var tramo = InicioDeTramo(primerDia, agrupacion); tramo <= ultimoDia; tramo = Siguiente(tramo, agrupacion))
        {
            tramos[tramo] = (0, 0);
        }

        foreach (var pago in vigentes)
        {
            var tramo = InicioDeTramo(ZonaClinica.ALocal(pago.FechaPago).Date, agrupacion);
            var actual = tramos.GetValueOrDefault(tramo);
            tramos[tramo] = (actual.Total + pago.Monto, actual.Cobros + 1);
        }

        return new ReporteIngresosDto(
            desde,
            hasta,
            agrupacion,
            total,
            vigentes.Count,
            vigentes.Count == 0 ? 0 : Math.Round(total / vigentes.Count, 2),
            anulados.Count,
            anulados.Sum(p => p.Monto),
            consultasAtendidas,
            consultasPorCobrar,
            anteriores.Sum(p => p.Monto),
            anteriores.Count,
            tramos
                .Select(t => new PuntoIngresosDto(
                    t.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    t.Value.Total,
                    t.Value.Cobros))
                .ToList(),
            vigentes
                .GroupBy(p => p.MetodoPago)
                .Select(g => new IngresoPorMetodoDto(g.Key, g.Sum(p => p.Monto), g.Count()))
                .OrderByDescending(x => x.Total)
                .ToList(),
            vigentes
                .GroupBy(p => new { p.NutricionistaId, p.Nutricionista })
                .Select(g => new IngresoPorNutricionistaDto(
                    g.Key.NutricionistaId,
                    g.Key.Nutricionista,
                    g.Sum(p => p.Monto),
                    g.Count()))
                .OrderByDescending(x => x.Total)
                .ToList(),
            vigentes
                .GroupBy(p => p.TipoConsulta)
                .Select(g => new IngresoPorTipoDto(g.Key, g.Sum(p => p.Monto), g.Count()))
                .OrderByDescending(x => x.Total)
                .ToList());
    }

    private static (DateTime Desde, DateTime Hasta) Validar(ReporteIngresosFiltroDto filtro)
    {
        if (filtro.Desde is not { } desde || filtro.Hasta is not { } hasta)
        {
            throw Invalido("Desde", "Indica el periodo del reporte.");
        }

        if (desde > hasta)
        {
            throw Invalido("Desde", "La fecha inicial no puede superar la fecha final.");
        }

        if ((hasta - desde).TotalDays > DiasMaximos)
        {
            throw Invalido("Hasta", "El periodo no puede superar tres años.");
        }

        return (desde, hasta);
    }

    private static ValidationException Invalido(string campo, string mensaje) =>
        new([new ValidationFailure(campo, mensaje)]);

    private static AgrupacionReporte ElegirAgrupacion(int dias) => dias switch
    {
        <= DiasPorDia => AgrupacionReporte.Dia,
        <= DiasPorSemana => AgrupacionReporte.Semana,
        _ => AgrupacionReporte.Mes,
    };

    private static DateTime InicioDeTramo(DateTime dia, AgrupacionReporte agrupacion) => agrupacion switch
    {
        AgrupacionReporte.Semana => dia.AddDays(-(((int)dia.DayOfWeek + 6) % 7)),
        AgrupacionReporte.Mes => new DateTime(dia.Year, dia.Month, 1),
        _ => dia,
    };

    private static DateTime Siguiente(DateTime tramo, AgrupacionReporte agrupacion) => agrupacion switch
    {
        AgrupacionReporte.Semana => tramo.AddDays(7),
        AgrupacionReporte.Mes => tramo.AddMonths(1),
        _ => tramo.AddDays(1),
    };
}
