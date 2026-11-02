using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Agenda.Services;
using nutriclinica_backend.Features.Auth.Services;
using nutriclinica_backend.Features.Nutricionistas.DTOs;
using nutriclinica_backend.Features.Nutricionistas.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Nutricionistas.Services;

public class NutricionistaService : INutricionistaService
{
    /// <summary>Citas que todavia se van a atender y se pueden mover a otra persona.</summary>
    private static readonly EstadoCita[] EstadosPendientes =
    [
        EstadoCita.Programada,
        EstadoCita.Confirmada
    ];

    private static readonly EstadoCita[] EstadosQueOcupanAgenda =
    [
        EstadoCita.Programada,
        EstadoCita.Confirmada,
        EstadoCita.EnCurso
    ];

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearNutricionistaDto> _crearNutricionistaValidator;
    private readonly IValidator<ActualizarNutricionistaDto> _actualizarNutricionistaValidator;
    private readonly IAgendaService _agenda;

    public NutricionistaService(
        ApplicationDbContext context,
        IValidator<CrearNutricionistaDto> crearNutricionistaValidator,
        IValidator<ActualizarNutricionistaDto> actualizarNutricionistaValidator,
        IAgendaService agenda)
    {
        _context = context;
        _agenda = agenda;
        _crearNutricionistaValidator = crearNutricionistaValidator;
        _actualizarNutricionistaValidator = actualizarNutricionistaValidator;
    }

    public async Task<NutricionistaRespuestaDto> CrearNutricionistaAsync(CrearNutricionistaDto dto)
    {
        await _crearNutricionistaValidator.ValidateAndThrowAsync(dto);
        ValidarContrasena(dto.Contrasena);

        var correo = NormalizarCorreo(dto.CorreoElectronico);
        await ValidarCorreoDisponibleAsync(correo, null);
        await ValidarColegiaturaDisponibleAsync(dto.NumeroColegiatura, null);

        var nutricionista = new Nutricionista
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            CorreoElectronico = correo,
            Telefono = Limpiar(dto.Telefono),
            NumeroColegiatura = Limpiar(dto.NumeroColegiatura),
            Especialidad = Limpiar(dto.Especialidad),
            Rol = dto.Rol,
            IsActive = true,
            DebeCambiarContrasena = true
        };
        nutricionista.PasswordHash = AuthService.HashPassword(nutricionista, dto.Contrasena);

        _context.Nutricionistas.Add(nutricionista);
        await _context.SaveChangesAsync();

        return MapToDto(nutricionista);
    }

    public async Task<NutricionistaRespuestaDto> ActualizarNutricionistaAsync(
        Guid id,
        ActualizarNutricionistaDto dto,
        Guid actorId,
        bool actorEsAdministrador)
    {
        await _actualizarNutricionistaValidator.ValidateAndThrowAsync(dto);

        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró una cuenta activa con ID: {id}");

        var correo = NormalizarCorreo(dto.CorreoElectronico);
        await ValidarCorreoDisponibleAsync(correo, id);
        await ValidarColegiaturaDisponibleAsync(dto.NumeroColegiatura, id);

        if (dto.Rol.HasValue && dto.Rol.Value != nutricionista.Rol)
        {
            if (!actorEsAdministrador)
                throw Falla("Solo un administrador puede cambiar el rol.");

            /* Al no poder quitarse el rol a si mismo, siempre queda al menos
               un administrador activo en la clinica. */
            if (id == actorId)
                throw Falla("No puedes cambiar tu propio rol. Pídeselo a otro administrador.");

            if (dto.Rol.Value == RolUsuario.Recepcion)
                await ValidarSinCitasPendientesAsync(id,
                    "Recepción no atiende pacientes: reasigna antes sus citas pendientes.");

            nutricionista.Rol = dto.Rol.Value;
        }

        nutricionista.NombreCompleto = dto.NombreCompleto.Trim();
        nutricionista.CorreoElectronico = correo;
        nutricionista.Telefono = Limpiar(dto.Telefono);
        nutricionista.NumeroColegiatura = Limpiar(dto.NumeroColegiatura);
        nutricionista.Especialidad = Limpiar(dto.Especialidad);

        await _context.SaveChangesAsync();

        return MapToDto(nutricionista);
    }

    public async Task EstablecerContrasenaAsync(Guid id, string contrasena, bool temporal)
    {
        ValidarContrasena(contrasena);

        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró una cuenta activa con ID: {id}");

        nutricionista.PasswordHash = AuthService.HashPassword(nutricionista, contrasena);
        nutricionista.DebeCambiarContrasena = temporal;
        /* Cierra las sesiones abiertas: con la contraseña nueva hay que volver a entrar. */
        nutricionista.RefreshToken = null;
        nutricionista.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync();
    }

    /// <summary>Reglas de contraseña compartidas por el alta, el restablecimiento y el cambio propio.</summary>
    public static void ValidarContrasena(string contrasena)
    {
        if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8)
            throw Falla("La contraseña debe tener al menos 8 caracteres.", "Contrasena");

        if (!contrasena.Any(char.IsUpper))
            throw Falla("La contraseña debe incluir al menos una letra mayúscula.", "Contrasena");

        if (!contrasena.Any(char.IsDigit))
            throw Falla("La contraseña debe incluir al menos un número.", "Contrasena");
    }

    public async Task<NutricionistaRespuestaDto> ObtenerNutricionistaPorIdAsync(Guid id)
    {
        var nutricionista = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException($"No se encontró la cuenta con ID: {id}");

        return MapToDto(nutricionista);
    }

    public async Task<IEnumerable<NutricionistaRespuestaDto>> ObtenerNutricionistasAsync(bool incluirInactivos)
    {
        var query = _context.Nutricionistas.AsNoTracking();

        if (!incluirInactivos)
        {
            query = query.Where(n => n.IsActive);
        }

        var nutricionistas = await query.OrderBy(n => n.NombreCompleto).ToListAsync();

        return nutricionistas.Select(MapToDto).ToList();
    }

    public async Task EliminarNutricionistaAsync(Guid id, Guid actorId)
    {
        if (id == actorId)
            throw Falla("No puedes desactivar tu propia cuenta.");

        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró una cuenta activa con ID: {id}");

        var enCurso = await _context.Citas
            .AnyAsync(c => c.NutricionistaId == id && c.Estado == EstadoCita.EnCurso);
        if (enCurso)
            throw Falla("Tiene una consulta en curso. Ciérrala antes de desactivar la cuenta.");

        await ValidarSinCitasPendientesAsync(id,
            "Tiene citas pendientes. Reasígnalas a otra persona antes de desactivar la cuenta.");

        nutricionista.IsActive = false;
        nutricionista.RefreshToken = null;
        nutricionista.RefreshTokenExpiresAt = null;
        await _context.SaveChangesAsync();
    }

    public async Task<NutricionistaRespuestaDto> ReactivarNutricionistaAsync(Guid id)
    {
        var nutricionista = await _context.Nutricionistas
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró una cuenta inactiva con ID: {id}");

        await ValidarCorreoDisponibleAsync(nutricionista.CorreoElectronico, id);

        nutricionista.IsActive = true;
        await _context.SaveChangesAsync();

        return MapToDto(nutricionista);
    }

    public async Task<IEnumerable<CitaPendienteDto>> ObtenerCitasPendientesAsync(Guid id)
    {
        var ahora = DateTime.UtcNow;

        return await (
            from c in _context.Citas.AsNoTracking()
            join p in _context.Pacientes.AsNoTracking() on c.PacienteId equals p.Id
            where c.NutricionistaId == id
                  && EstadosPendientes.Contains(c.Estado)
                  && c.FechaFin > ahora
            orderby c.FechaInicio
            select new CitaPendienteDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                PacienteNombre = p.NombreCompleto,
                FechaInicio = c.FechaInicio,
                FechaFin = c.FechaFin,
                Estado = c.Estado.ToString()
            })
            .ToListAsync();
    }

    public async Task<ResultadoReasignacionDto> ReasignarCitasAsync(
        Guid id, Guid destinoId, DateOnly? desde = null, DateOnly? hasta = null)
    {
        if (id == destinoId)
            throw Falla("Elige a otra persona para recibir las citas.");

        var destino = await _context.Nutricionistas
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == destinoId && n.IsActive)
            ?? throw new KeyNotFoundException($"No se encontró una cuenta activa con ID: {destinoId}");

        if (destino.Rol == RolUsuario.Recepcion)
            throw Falla("Recepción no atiende pacientes: elige a un nutricionista.");

        var ahora = DateTime.UtcNow;
        var citas = await _context.Citas
            .Include(c => c.Paciente)
            .Where(c => c.NutricionistaId == id
                        && EstadosPendientes.Contains(c.Estado)
                        && c.FechaFin > ahora)
            .OrderBy(c => c.FechaInicio)
            .ToListAsync();

        if (desde.HasValue || hasta.HasValue)
        {
            citas = citas.Where(c =>
            {
                var dia = DateOnly.FromDateTime(ZonaClinica.ALocal(c.FechaInicio));
                return (!desde.HasValue || dia >= desde.Value) && (!hasta.HasValue || dia <= hasta.Value);
            }).ToList();
        }

        if (citas.Count == 0)
            return new ResultadoReasignacionDto();

        var primera = citas.Min(c => c.FechaInicio);
        var agendaDestino = await _context.Citas
            .AsNoTracking()
            .Where(c => c.NutricionistaId == destinoId
                        && EstadosQueOcupanAgenda.Contains(c.Estado)
                        && c.FechaFin > primera)
            .Select(c => new FranjaOcupada(c.FechaInicio, c.FechaFin))
            .ToListAsync();

        var resultado = new ResultadoReasignacionDto();

        foreach (var cita in citas)
        {
            var choca = agendaDestino.Any(o => o.Inicio < cita.FechaFin && cita.FechaInicio < o.Fin)
                        || await _agenda.MotivoNoDisponibleAsync(destinoId, cita.FechaInicio, cita.FechaFin) is not null;
            if (choca)
            {
                resultado.Conflictos.Add(new CitaPendienteDto
                {
                    Id = cita.Id,
                    PacienteId = cita.PacienteId,
                    PacienteNombre = cita.Paciente.NombreCompleto,
                    FechaInicio = cita.FechaInicio,
                    FechaFin = cita.FechaFin,
                    Estado = cita.Estado.ToString()
                });
                continue;
            }

            cita.NutricionistaId = destinoId;
            agendaDestino.Add(new FranjaOcupada(cita.FechaInicio, cita.FechaFin));
            resultado.Reasignadas++;
        }

        await _context.SaveChangesAsync();

        return resultado;
    }

    private sealed record FranjaOcupada(DateTime Inicio, DateTime Fin);

    private async Task ValidarSinCitasPendientesAsync(Guid id, string mensaje)
    {
        var ahora = DateTime.UtcNow;
        var pendientes = await _context.Citas
            .AnyAsync(c => c.NutricionistaId == id
                           && EstadosPendientes.Contains(c.Estado)
                           && c.FechaFin > ahora);

        if (pendientes) throw Falla(mensaje);
    }

    private async Task ValidarCorreoDisponibleAsync(string correo, Guid? excluirId)
    {
        var yaExiste = await _context.Nutricionistas
            .AnyAsync(n => n.IsActive
                           && n.CorreoElectronico.ToLower() == correo
                           && (!excluirId.HasValue || n.Id != excluirId.Value));

        if (yaExiste)
            throw Falla("Ya existe una cuenta activa con ese correo electrónico.", "CorreoElectronico");
    }

    private async Task ValidarColegiaturaDisponibleAsync(string? numeroColegiatura, Guid? excluirId)
    {
        if (string.IsNullOrWhiteSpace(numeroColegiatura)) return;

        var cedula = numeroColegiatura.Trim();
        var yaExiste = await _context.Nutricionistas
            .AnyAsync(n => n.NumeroColegiatura == cedula
                           && (!excluirId.HasValue || n.Id != excluirId.Value));

        if (yaExiste)
            throw Falla("Ya existe una cuenta registrada con esa cédula profesional.", "NumeroColegiatura");
    }

    /// <summary>
    /// Error de regla de negocio con el mensaje en el detalle, que es lo que
    /// muestra el cliente (un ValidationException solo con mensaje llega vacio).
    /// </summary>
    private static ValidationException Falla(string mensaje, string campo = "") =>
        new([new ValidationFailure(campo, mensaje)]);

    private static string NormalizarCorreo(string correo) => correo.Trim().ToLower();

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static NutricionistaRespuestaDto MapToDto(Nutricionista nutricionista) => new()
    {
        Id = nutricionista.Id,
        NombreCompleto = nutricionista.NombreCompleto,
        CorreoElectronico = nutricionista.CorreoElectronico,
        Telefono = nutricionista.Telefono,
        NumeroColegiatura = nutricionista.NumeroColegiatura,
        Especialidad = nutricionista.Especialidad,
        Rol = nutricionista.Rol,
        DebeCambiarContrasena = nutricionista.DebeCambiarContrasena,
        IsActive = nutricionista.IsActive,
        FechaRegistro = nutricionista.FechaRegistro
    };
}
