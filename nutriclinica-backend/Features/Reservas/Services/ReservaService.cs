using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Agenda.DTOs;
using nutriclinica_backend.Features.Agenda.Services;
using nutriclinica_backend.Features.Citas.DTOs;
using nutriclinica_backend.Features.Citas.Interfaces;
using nutriclinica_backend.Features.Reservas.DTOs;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Reservas.Services;

public interface IReservaService
{
    Task<OpcionesReservaDto> ObtenerOpcionesAsync();
    Task<List<DiaPublicoDto>> ObtenerDisponibilidadAsync(Guid? nutricionistaId, DateOnly desde, DateOnly hasta);
    Task<ReservaConfirmadaDto> CrearAsync(CrearReservaDto dto);
}

/// <summary>
/// Reservacion en linea: el paciente elige hora de los huecos que calcula la
/// agenda y la cita entra como Programada (por confirmar) con canal EnLinea.
/// Todo se vuelve a validar al confirmar: el hueco pudo ocuparse mientras tanto.
/// </summary>
public class ReservaService : IReservaService
{
    public const int AnticipacionHoras = 2;
    public const int DiasMaximos = 60;
    private const int PasoMinutos = 30;

    private static readonly EstadoCita[] EstadosVigentes = [EstadoCita.Programada, EstadoCita.Confirmada];

    private readonly ApplicationDbContext _context;
    private readonly IAgendaService _agenda;
    private readonly ICitaService _citas;
    private readonly IValidator<CrearReservaDto> _validator;

    public ReservaService(
        ApplicationDbContext context,
        IAgendaService agenda,
        ICitaService citas,
        IValidator<CrearReservaDto> validator)
    {
        _context = context;
        _agenda = agenda;
        _citas = citas;
        _validator = validator;
    }

    public async Task<OpcionesReservaDto> ObtenerOpcionesAsync()
    {
        var profesionales = await _context.Nutricionistas
            .AsNoTracking()
            .Where(n => n.IsActive && n.Rol != RolUsuario.Recepcion)
            .OrderBy(n => n.NombreCompleto)
            .Select(n => new ProfesionalPublicoDto
            {
                Id = n.Id,
                Nombre = n.NombreCompleto,
                Especialidad = n.Especialidad,
                DuracionMinutos = n.DuracionConsultaMinutos
            })
            .ToListAsync();

        return new OpcionesReservaDto
        {
            Clinica = await ClinicaAsync(),
            Profesionales = profesionales,
            AnticipacionHoras = AnticipacionHoras,
            DiasMaximos = DiasMaximos
        };
    }

    public async Task<List<DiaPublicoDto>> ObtenerDisponibilidadAsync(Guid? nutricionistaId, DateOnly desde, DateOnly hasta)
    {
        var hoy = DateOnly.FromDateTime(ZonaClinica.Ahora());
        var limite = hoy.AddDays(DiasMaximos);
        if (desde < hoy) desde = hoy;
        if (hasta > limite) hasta = limite;
        if (hasta < desde) return [];

        var minimo = DateTime.UtcNow.AddHours(AnticipacionHoras);
        var disponibilidades = await DisponibilidadesAsync(nutricionistaId, desde, hasta);

        /* "Con quien sea": se juntan las horas de todos, sin repetir. */
        return disponibilidades
            .SelectMany(d => d.Dias)
            .GroupBy(d => d.Fecha)
            .OrderBy(g => g.Key)
            .Select(g => new DiaPublicoDto
            {
                Fecha = g.Key,
                Horas = g.SelectMany(d => d.Huecos)
                    .Where(h => h.Inicio >= minimo)
                    .GroupBy(h => h.Inicio)
                    .OrderBy(h => h.Key)
                    .Select(h => new HoraPublicaDto { Inicio = h.Key, Hora = h.First().Hora })
                    .ToList()
            })
            .ToList();
    }

    public async Task<ReservaConfirmadaDto> CrearAsync(CrearReservaDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        if (!string.IsNullOrWhiteSpace(dto.Sitio))
            throw Falla("No pudimos registrar la reservación.", "sitio");

        var inicio = DateTime.SpecifyKind(dto.Inicio.ToUniversalTime(), DateTimeKind.Utc);
        if (inicio < DateTime.UtcNow.AddHours(AnticipacionHoras))
            throw Falla($"Reserva con al menos {AnticipacionHoras} horas de anticipación.", "inicio");

        var fecha = DateOnly.FromDateTime(ZonaClinica.ALocal(inicio));
        if (fecha > DateOnly.FromDateTime(ZonaClinica.Ahora()).AddDays(DiasMaximos))
            throw Falla($"Solo se puede reservar con {DiasMaximos} días de anticipación como máximo.", "inicio");

        /* Quien tenga libre exactamente ese inicio (el primero, si es "con quien sea"). */
        var disponibles = await DisponibilidadesAsync(dto.NutricionistaId, fecha, fecha);
        var elegido = disponibles
            .Select(d => new { d.NutricionistaId, d.NutricionistaNombre, Hueco = d.Dias.SelectMany(x => x.Huecos).FirstOrDefault(h => h.Inicio == inicio) })
            .FirstOrDefault(x => x.Hueco is not null)
            ?? throw new ConflictException("Ese horario acaba de ocuparse. Elige otro, por favor.");

        var correo = dto.CorreoElectronico.Trim().ToLowerInvariant();
        var telefono = dto.Telefono.Trim();

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        var paciente = await BuscarPacienteAsync(correo, telefono, dto.NombreCompleto);
        if (paciente is null)
        {
            paciente = new Paciente
            {
                NombreCompleto = dto.NombreCompleto.Trim(),
                Telefono = telefono,
                CorreoElectronico = correo,
                Sexo = dto.Sexo,
                IsActive = true,
                DatosPorCompletar = true
            };
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
        }
        else
        {
            /* Evita que alguien llene la agenda con los datos de un paciente. */
            var tieneProxima = await _context.Citas.AnyAsync(c =>
                c.PacienteId == paciente.Id && EstadosVigentes.Contains(c.Estado) && c.FechaInicio > DateTime.UtcNow);
            if (tieneProxima)
                throw new ConflictException(
                    "Ya hay una cita próxima registrada con estos datos. Para cambiarla, comunícate con la clínica.");
        }

        var motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim();
        var cita = await _citas.CrearCitaAsync(new CrearCitaDto
        {
            PacienteId = paciente.Id,
            NutricionistaId = elegido.NutricionistaId,
            FechaInicio = elegido.Hueco!.Inicio,
            FechaFin = elegido.Hueco.Fin,
            Motivo = motivo,
            Canal = CanalCita.EnLinea
        });

        await transaccion.CommitAsync();

        return new ReservaConfirmadaDto
        {
            FechaInicio = cita.FechaInicio,
            FechaFin = cita.FechaFin,
            NutricionistaNombre = cita.NutricionistaNombre,
            Clinica = await ClinicaAsync()
        };
    }

    private async Task<List<DisponibilidadDto>> DisponibilidadesAsync(Guid? nutricionistaId, DateOnly desde, DateOnly hasta)
    {
        if (nutricionistaId is null)
            return (await _agenda.ObtenerDisponibilidadEquipoAsync(desde, hasta, null, PasoMinutos)).ToList();

        try
        {
            return [await _agenda.ObtenerDisponibilidadAsync(nutricionistaId.Value, desde, hasta, null, PasoMinutos)];
        }
        catch (ValidationException)
        {
            /* Recepcion o id que no atiende: para el publico simplemente no existe. */
            throw new KeyNotFoundException("No encontramos a ese profesional.");
        }
    }

    /// <summary>
    /// Mismo correo, o mismo telefono (ultimos 10 digitos) y mismo primer
    /// nombre: es el mismo paciente. Solo el telefono no basta, porque un
    /// digito mal escrito ligaria la cita al expediente de otra persona.
    /// </summary>
    private async Task<Paciente?> BuscarPacienteAsync(string correo, string telefono, string nombre)
    {
        var porCorreo = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.IsActive && p.CorreoElectronico.ToLower() == correo);
        if (porCorreo is not null) return porCorreo;

        var digitos = UltimosDigitos(telefono);
        if (digitos.Length < 10) return null;

        var candidatos = await _context.Pacientes
            .Where(p => p.IsActive && p.Telefono.EndsWith(digitos.Substring(digitos.Length - 4)))
            .ToListAsync();
        var primerNombre = PrimerNombre(nombre);
        return candidatos.FirstOrDefault(p =>
            UltimosDigitos(p.Telefono) == digitos && PrimerNombre(p.NombreCompleto) == primerNombre);
    }

    private static string UltimosDigitos(string telefono)
    {
        var digitos = new string(telefono.Where(char.IsDigit).ToArray());
        return digitos.Length > 10 ? digitos[^10..] : digitos;
    }

    /// <summary>Primera palabra sin acentos ni mayusculas ("José" == "jose").</summary>
    private static string PrimerNombre(string nombre)
    {
        var palabra = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var sinAcentos = palabra.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(sinAcentos.ToArray()).ToLowerInvariant();
    }

    private async Task<ClinicaPublicaDto> ClinicaAsync()
    {
        var datos = await _context.DatosFiscalesClinica.AsNoTracking().FirstOrDefaultAsync();
        return new ClinicaPublicaDto
        {
            Nombre = string.IsNullOrWhiteSpace(datos?.RazonSocial) ? "NutriClínica" : datos.RazonSocial,
            Direccion = string.IsNullOrWhiteSpace(datos?.Direccion) ? null : datos.Direccion,
            Telefono = datos?.Telefono,
            Correo = datos?.Correo
        };
    }

    private static ValidationException Falla(string mensaje, string campo) =>
        new([new ValidationFailure(campo, mensaje)]);
}
