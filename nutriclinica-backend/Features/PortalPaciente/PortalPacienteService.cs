using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Citas.DTOs;
using nutriclinica_backend.Features.Citas.Interfaces;
using nutriclinica_backend.Features.Menus.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.PortalPaciente;

// ── DTOs ─────────────────────────────────────────────────────────────

public record MiPerfilDto(
    Guid Id,
    string NombreCompleto,
    string CorreoElectronico,
    string Telefono,
    DateOnly? FechaNacimiento,
    int? Edad,
    string Sexo,
    string? TituloObjetivo,
    decimal? PesoObjetivo,
    bool DatosPorCompletar,
    MiHistorialDto? Historial,
    MiClinicaDto Clinica,
    bool TieneContrasena,
    bool ConGoogle);

public record MiHistorialDto(
    List<string> Enfermedades,
    List<string> Alergias,
    List<string> AlimentosFavoritos,
    List<string> AlimentosNoFavoritos);

public record MiClinicaDto(string Nombre, string? Direccion, string? Telefono, string? Correo);

public record MiCitaDto(
    Guid Id,
    DateTime FechaInicio,
    DateTime FechaFin,
    string Estado,
    string NutricionistaNombre,
    string? Motivo,
    bool PuedeConfirmar,
    bool PuedeCancelar);

public record CancelarMiCitaDto(string? Motivo);

public record MiPlanDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    decimal? ObjetivoCalorico,
    DateOnly FechaInicio,
    List<MiDiaDto> Dias);

public record MiDiaDto(string Dia, decimal Calorias, List<MiComidaDto> Comidas);

public record MiComidaDto(string Tiempo, decimal Calorias, List<MiPlatoDto> Platos);

public record MiPlatoDto(
    string Nombre,
    decimal Porciones,
    decimal Calorias,
    decimal Proteinas,
    decimal Carbohidratos,
    decimal Grasas,
    string? Indicaciones,
    List<string> Ingredientes);

public record MiMedidaDto(
    DateTime Fecha,
    decimal Peso,
    decimal Imc,
    decimal Estatura,
    decimal? PorcentajeGrasa,
    decimal? PorcentajeMasaMuscular,
    decimal? Cintura,
    decimal? Cadera);

public record MiProgresoDto(decimal? PesoObjetivo, string? TituloObjetivo, List<MiMedidaDto> Medidas);

// ── Servicio ─────────────────────────────────────────────────────────

/// <summary>
/// Lo que ve el paciente en su app. Todo se filtra por el expediente del
/// token: un paciente nunca puede pedir datos de otro.
/// </summary>
public class PortalPacienteService
{
    private static readonly EstadoCita[] Vigentes = [EstadoCita.Programada, EstadoCita.Confirmada];

    private readonly ApplicationDbContext _context;
    private readonly ICitaService _citas;
    private readonly IMenuAsignadoService _menus;

    public PortalPacienteService(ApplicationDbContext context, ICitaService citas, IMenuAsignadoService menus)
    {
        _context = context;
        _citas = citas;
        _menus = menus;
    }

    public async Task<MiPerfilDto> PerfilAsync(Guid pacienteId, Guid cuentaId)
    {
        var p = await _context.Pacientes
            .AsNoTracking()
            .Include(x => x.HistorialClinico)
            .FirstOrDefaultAsync(x => x.Id == pacienteId)
            ?? throw new KeyNotFoundException("No encontramos tu expediente.");
        var cuenta = await _context.CuentasPaciente.AsNoTracking().FirstAsync(c => c.Id == cuentaId);
        var clinica = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();

        int? edad = null;
        if (p.FechaNacimiento is { } nac)
        {
            var hoy = DateOnly.FromDateTime(ZonaClinica.Ahora());
            edad = hoy.Year - nac.Year - (hoy < nac.AddYears(hoy.Year - nac.Year) ? 1 : 0);
        }

        var h = p.HistorialClinico;
        return new MiPerfilDto(
            p.Id, p.NombreCompleto, p.CorreoElectronico, p.Telefono, p.FechaNacimiento, edad, p.Sexo.ToString(),
            p.TituloObjetivo, p.PesoObjetivo, p.DatosPorCompletar,
            h is null ? null : new MiHistorialDto(h.Enfermedades, h.Alergias, h.AlimentosFavoritos, h.AlimentosNoFavoritos),
            new MiClinicaDto(
                string.IsNullOrWhiteSpace(clinica?.RazonSocial) ? "NutriClínica" : clinica.RazonSocial,
                string.IsNullOrWhiteSpace(clinica?.Direccion) ? null : clinica.Direccion,
                clinica?.Telefono,
                clinica?.Correo),
            cuenta.PasswordHash is not null,
            cuenta.GoogleId is not null);
    }

    public async Task<List<MiCitaDto>> CitasAsync(Guid pacienteId)
    {
        var ahora = DateTime.UtcNow;
        var citas = await _context.Citas
            .AsNoTracking()
            .Include(c => c.Nutricionista)
            .Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.FechaInicio)
            .Take(60)
            .ToListAsync();

        return citas.Select(c => new MiCitaDto(
            c.Id, c.FechaInicio, c.FechaFin, c.Estado.ToString(), c.Nutricionista.NombreCompleto, c.Motivo,
            PuedeConfirmar: c.Estado == EstadoCita.Programada && c.FechaInicio > ahora,
            PuedeCancelar: Vigentes.Contains(c.Estado) && c.FechaInicio > ahora)).ToList();
    }

    public async Task<MiCitaDto> ConfirmarCitaAsync(Guid pacienteId, Guid citaId)
    {
        var cita = await CitaPropiaAsync(pacienteId, citaId);
        if (cita.Estado == EstadoCita.Confirmada) return (await CitasAsync(pacienteId)).First(c => c.Id == citaId);
        if (cita.Estado != EstadoCita.Programada || cita.FechaInicio <= DateTime.UtcNow)
            throw new ConflictException("Esta cita ya no se puede confirmar.");

        await _citas.CambiarEstadoAsync(citaId, new CambiarEstadoCitaDto { Estado = EstadoCita.Confirmada });
        return (await CitasAsync(pacienteId)).First(c => c.Id == citaId);
    }

    public async Task<MiCitaDto> CancelarCitaAsync(Guid pacienteId, Guid citaId, string? motivo)
    {
        var cita = await CitaPropiaAsync(pacienteId, citaId);
        if (!Vigentes.Contains(cita.Estado) || cita.FechaInicio <= DateTime.UtcNow)
            throw new ConflictException("Esta cita ya no se puede cancelar.");

        var texto = string.IsNullOrWhiteSpace(motivo)
            ? "El paciente canceló desde la app."
            : $"El paciente canceló desde la app: {motivo.Trim()}";
        // CambiarEstado avisa por correo a paciente y nutriologo, y quita la cita del calendario.
        await _citas.CambiarEstadoAsync(citaId, new CambiarEstadoCitaDto
        {
            Estado = EstadoCita.Cancelada,
            Motivo = texto.Length > 500 ? texto[..500] : texto,
        });
        return (await CitasAsync(pacienteId)).First(c => c.Id == citaId);
    }

    /// <summary>Plan activo del paciente, con ingredientes en medidas caseras.</summary>
    public async Task<MiPlanDto?> PlanAsync(Guid pacienteId)
    {
        var id = await _context.MenusAsignados
            .AsNoTracking()
            .Where(m => m.PacienteId == pacienteId && m.IsActive && m.Estado == MenuAsignadoEstado.Activo)
            .OrderByDescending(m => m.FechaInicio)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync();
        if (id is null) return null;

        var menu = await _menus.ObtenerMenuAsignadoPorIdAsync(id.Value);

        var platoIds = menu.Detalles.Select(d => d.PlatoId).Distinct().ToList();
        var ingredientes = (await _context.DetallesPlato
                .AsNoTracking()
                .Where(d => platoIds.Contains(d.PlatoId))
                .OrderBy(d => d.Orden)
                .Select(d => new { d.PlatoId, Alimento = d.Alimento.Nombre, d.RacionGramos, d.MedidaCantidad, d.MedidaNombre })
                .ToListAsync())
            .GroupBy(i => i.PlatoId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dias = menu.Detalles
            .GroupBy(d => d.DiaSemana)
            .OrderBy(g => g.Key)
            .Select(dia => new MiDiaDto(
                dia.Key.ToString(),
                dia.Sum(d => d.Calorias),
                dia.GroupBy(d => d.TiempoComida)
                    .OrderBy(g => g.Key)
                    .Select(t => new MiComidaDto(
                        t.Key.ToString(),
                        t.Sum(d => d.Calorias),
                        t.OrderBy(d => d.Orden).Select(d => new MiPlatoDto(
                            d.PlatoNombre, d.Cantidad, d.Calorias, d.Proteinas, d.Carbohidratos, d.Grasas,
                            d.Indicaciones,
                            ingredientes.TryGetValue(d.PlatoId, out var lista)
                                ? lista.Select(i =>
                                {
                                    var medida = TextoMedida.Escalada(i.MedidaCantidad, i.MedidaNombre, d.Cantidad);
                                    var gramos = Num(i.RacionGramos * d.Cantidad);
                                    return medida is null ? $"{i.Alimento} · {gramos} g" : $"{i.Alimento} · {medida} ({gramos} g)";
                                }).ToList()
                                : [])).ToList()))
                    .ToList()))
            .ToList();

        return new MiPlanDto(menu.Id, menu.Nombre, menu.Descripcion, menu.ObjetivoCalorico, menu.FechaInicio, dias);
    }

    public async Task<MiProgresoDto> ProgresoAsync(Guid pacienteId)
    {
        var paciente = await _context.Pacientes.AsNoTracking().FirstAsync(p => p.Id == pacienteId);
        var medidas = await _context.MedidasAntropometricas
            .AsNoTracking()
            .Where(m => m.PacienteId == pacienteId)
            .OrderBy(m => m.FechaMedicion)
            .Select(m => new MiMedidaDto(m.FechaMedicion, m.Peso, m.Imc, m.Estatura,
                m.PorcentajeGrasa, m.PorcentajeMasaMuscular, m.MedidaCintura, m.MedidaCadera))
            .ToListAsync();
        return new MiProgresoDto(paciente.PesoObjetivo, paciente.TituloObjetivo, medidas);
    }

    private async Task<Core.Entities.Cita> CitaPropiaAsync(Guid pacienteId, Guid citaId) =>
        await _context.Citas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == citaId && c.PacienteId == pacienteId)
        ?? throw new KeyNotFoundException("No encontramos esa cita.");

    private static string Num(decimal valor) => valor.ToString("0.#", ZonaClinica.Cultura);
}
