using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Features.Consultas.Interfaces;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Menus.Interfaces;
using nutriclinica_backend.Features.Pacientes.Interfaces;
using nutriclinica_backend.Features.Reportes.Pdf;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Reportes.Services;

public interface IReporteService
{
    Task<(byte[] Pdf, string NombreArchivo)> MenuPacienteAsync(Guid menuId);
    Task<(byte[] Pdf, string NombreArchivo)> HistorialClinicoAsync(Guid pacienteId);
}

/// <summary>
/// Arma los PDF reutilizando los servicios de cada modulo, asi el documento
/// muestra exactamente lo mismo que la pantalla (mismos calculos de macros,
/// mismas reglas de visibilidad) y los 404 salen de donde ya salian.
/// </summary>
public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _context;
    private readonly IMenuAsignadoService _menus;
    private readonly IPacienteService _pacientes;
    private readonly IConsultaService _consultas;

    public ReporteService(
        ApplicationDbContext context,
        IMenuAsignadoService menus,
        IPacienteService pacientes,
        IConsultaService consultas)
    {
        _context = context;
        _menus = menus;
        _pacientes = pacientes;
        _consultas = consultas;
    }

    public async Task<(byte[] Pdf, string NombreArchivo)> MenuPacienteAsync(Guid menuId)
    {
        var menu = await _menus.ObtenerMenuAsignadoPorIdAsync(menuId);
        var clinica = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();

        /* El plan solo trae el nombre del plato; para el desglose se cargan sus
           ingredientes (gramos por porcion). Incluye alimentos eliminados: el
           plato que ya los usaba los sigue llevando. */
        var platoIds = menu.Detalles.Select(d => d.PlatoId).Distinct().ToList();
        var ingredientes = await _context.DetallesPlato
            .AsNoTracking()
            .Where(d => platoIds.Contains(d.PlatoId))
            .OrderBy(d => d.Orden)
            .Select(d => new { d.PlatoId, Alimento = d.Alimento.Nombre, d.RacionGramos, d.MedidaCantidad, d.MedidaNombre })
            .ToListAsync();
        var porPlato = ingredientes
            .GroupBy(i => i.PlatoId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<(string Alimento, decimal Gramos, decimal? MedidaCantidad, string? MedidaNombre)>)g
                    .Select(i => (i.Alimento, i.RacionGramos, i.MedidaCantidad, i.MedidaNombre))
                    .ToList());

        var pdf = ReportesPdf.MenuPaciente(menu, porPlato, clinica);
        return (pdf, $"menu-{Slug(menu.PacienteNombre)}.pdf");
    }

    public async Task<(byte[] Pdf, string NombreArchivo)> HistorialClinicoAsync(Guid pacienteId)
    {
        var paciente = await _pacientes.ObtenerPacientePorIdAsync(pacienteId);
        var expediente = await _pacientes.ObtenerExpedienteCompletoAsync(pacienteId);
        var consultas = (await _consultas.ObtenerConsultasPorPacienteAsync(pacienteId)).ToList();

        /* Plan vigente: el activo mas reciente; si no hay, el borrador. */
        var planes = (await _menus.ObtenerMenuAsignadoAsync(new MenuAsignadoFiltroDto { PacienteId = pacienteId }))
            .OrderByDescending(p => p.FechaInicio)
            .ToList();
        var vigente = planes.FirstOrDefault(p => p.Estado == MenuAsignadoEstado.Activo)
                      ?? planes.FirstOrDefault(p => p.Estado == MenuAsignadoEstado.Borrador);

        var clinica = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();

        /* El motivo vive en la cita que origino cada consulta. */
        var citaIds = consultas.Where(c => c.CitaId.HasValue).Select(c => c.CitaId!.Value).ToList();
        var motivos = await _context.Citas
            .AsNoTracking()
            .Where(c => citaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Motivo);

        var pdf = ReportesPdf.HistorialClinico(paciente, expediente, consultas, motivos, vigente, clinica);
        return (pdf, $"historial-clinico-{Slug(paciente.NombreCompleto)}.pdf");
    }

    /// <summary>Nombre de archivo seguro: "Ana López" -> "ana-lopez".</summary>
    private static string Slug(string texto)
    {
        var normalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
        var limpio = new string(normalizado
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray());
        return string.Join("-", limpio.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
