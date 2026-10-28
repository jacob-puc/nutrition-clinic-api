using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Features.Transacciones.DTOs;
using nutriclinica_backend.Features.Transacciones.DTOs.Validators;
using nutriclinica_backend.Features.Transacciones.Interfaces;
using nutriclinica_backend.Features.Transacciones.Pdf;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Transacciones.Services;

public class PagoService : IPagoService
{
    private const string PrefijoRecibo = "REC";

    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearPagoDto> _crearValidator;
    private readonly IValidator<PagoFiltroDto> _filtroValidator;

    public PagoService(
        ApplicationDbContext context,
        IValidator<CrearPagoDto> crearValidator,
        IValidator<PagoFiltroDto> filtroValidator)
    {
        _context = context;
        _crearValidator = crearValidator;
        _filtroValidator = filtroValidator;
    }

    public async Task<PagoRespuestaDto> CrearPagoAsync(CrearPagoDto dto)
    {
        await _crearValidator.ValidateAndThrowAsync(dto);

        var consulta = await _context.Consultas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.ConsultaId)
            ?? throw new KeyNotFoundException($"No se encontró la consulta con ID: {dto.ConsultaId}");

        if (consulta.FechaFin is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure("ConsultaId",
                    "Solo se puede cobrar una consulta que ya fue cerrada.")
            ]);
        }

        var yaPagada = await _context.Pagos
            .AsNoTracking()
            .AnyAsync(p => p.ConsultaId == dto.ConsultaId && p.Estado == EstadoPago.Pagado);

        if (yaPagada)
        {
            throw new ConflictException("La consulta ya tiene un pago vigente registrado.");
        }

        var pago = new Pago
        {
            Id = Guid.NewGuid(),
            ConsultaId = consulta.Id,
            PacienteId = consulta.PacienteId,
            Monto = dto.Monto,
            Moneda = "MXN",
            MetodoPago = dto.MetodoPago,
            Estado = EstadoPago.Pagado,
            NumeroRecibo = await GenerarNumeroReciboAsync(),
            FechaPago = DateTime.UtcNow,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        return await ObtenerPagoPorIdAsync(pago.Id);
    }

    public async Task<RespuestaPaginadaDto<PagoRespuestaDto>> ObtenerPagosAsync(PagoFiltroDto filtro)
    {
        await _filtroValidator.ValidateAndThrowAsync(filtro);

        var query = _context.Pagos.AsNoTracking().AsQueryable();

        if (filtro.PacienteId.HasValue)
        {
            query = query.Where(p => p.PacienteId == filtro.PacienteId.Value);
        }

        if (filtro.ConsultaId.HasValue)
        {
            query = query.Where(p => p.ConsultaId == filtro.ConsultaId.Value);
        }

        if (filtro.Estado.HasValue)
        {
            query = query.Where(p => p.Estado == filtro.Estado.Value);
        }

        if (filtro.MetodoPago.HasValue)
        {
            query = query.Where(p => p.MetodoPago == filtro.MetodoPago.Value);
        }

        if (filtro.FechaInicio.HasValue)
        {
            query = query.Where(p => p.FechaPago >= filtro.FechaInicio.Value);
        }

        if (filtro.FechaFin.HasValue)
        {
            query = query.Where(p => p.FechaPago <= filtro.FechaFin.Value);
        }

        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, 100);
        var totalRegistros = await query.CountAsync();

        var proyeccion = await (
            from p in query
            join paciente in _context.Pacientes.AsNoTracking() on p.PacienteId equals paciente.Id
            select new
            {
                Pago = p,
                PacienteNombre = paciente.NombreCompleto
            })
            .OrderByDescending(x => x.Pago.FechaPago)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new RespuestaPaginadaDto<PagoRespuestaDto>
        {
            TotalRegistros = totalRegistros,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanoPagina),
            Datos = proyeccion
                .Select(x => MapToDto(x.Pago, x.PacienteNombre))
                .ToList()
        };
    }

    public async Task<PagoRespuestaDto> ObtenerPagoPorIdAsync(Guid pagoId)
    {
        var proyeccion = await (
            from p in _context.Pagos.AsNoTracking()
            join paciente in _context.Pacientes.AsNoTracking() on p.PacienteId equals paciente.Id
            where p.Id == pagoId
            select new
            {
                Pago = p,
                PacienteNombre = paciente.NombreCompleto
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No se encontró el pago con ID: {pagoId}");

        return MapToDto(proyeccion.Pago, proyeccion.PacienteNombre);
    }

    public async Task<PagoRespuestaDto> AnularPagoAsync(Guid pagoId, AnularPagoDto dto)
    {
        var pago = await _context.Pagos
            .FirstOrDefaultAsync(p => p.Id == pagoId)
            ?? throw new KeyNotFoundException($"No se encontró el pago con ID: {pagoId}");

        if (pago.Estado == EstadoPago.Anulado)
        {
            throw new ConflictException("El pago ya está anulado.");
        }

        var tieneFactura = await _context.Facturas
            .AsNoTracking()
            .AnyAsync(f => f.PagoId == pagoId && f.Estatus != EstatusFactura.Anulada);

        if (tieneFactura)
        {
            throw new ConflictException(
                "El pago tiene una factura emitida y no puede anularse.");
        }

        pago.Estado = EstadoPago.Anulado;

        if (!string.IsNullOrWhiteSpace(dto.Motivo))
        {
            var motivo = dto.Motivo.Trim();
            pago.Notas = string.IsNullOrWhiteSpace(pago.Notas)
                ? $"Anulado: {motivo}"
                : $"{pago.Notas} | Anulado: {motivo}";
        }

        await _context.SaveChangesAsync();

        return await ObtenerPagoPorIdAsync(pago.Id);
    }

    public async Task<byte[]> GenerarComprobantePdfAsync(Guid pagoId)
    {
        var pago = await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Paciente)
            .Include(p => p.Consulta)
            .FirstOrDefaultAsync(p => p.Id == pagoId)
            ?? throw new KeyNotFoundException($"No se encontró el pago con ID: {pagoId}");

        var clinica = await _context.DatosFiscalesClinica
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return TransaccionesPdf.ComprobantePago(
            pago,
            pago.Paciente.NombreCompleto,
            ConstruirConcepto(pago.Consulta),
            clinica);
    }

    private async Task<string> GenerarNumeroReciboAsync()
    {
        var prefijo = $"{PrefijoRecibo}-{DateTime.UtcNow.Year}-";

        var recibos = await _context.Pagos
            .AsNoTracking()
            .Where(p => p.NumeroRecibo.StartsWith(prefijo))
            .Select(p => p.NumeroRecibo)
            .ToListAsync();

        var consecutivo = recibos
            .Select(r => int.TryParse(r[prefijo.Length..], out var numero) ? numero : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefijo}{consecutivo:D4}";
    }

    private static string ConstruirConcepto(Consulta? consulta)
    {
        if (consulta is null)
        {
            return "Servicio de nutrición";
        }

        return $"Consulta nutricional {consulta.TipoConsulta} del " +
               $"{consulta.FechaInicio:dd/MM/yyyy}";
    }

    private static PagoRespuestaDto MapToDto(Pago pago, string pacienteNombre) => new(
        pago.Id,
        pago.ConsultaId,
        pago.PacienteId,
        pacienteNombre,
        pago.Monto,
        pago.Moneda,
        pago.MetodoPago,
        pago.Estado,
        pago.NumeroRecibo,
        pago.FechaPago,
        pago.Notas,
        pago.FechaCreacion);
}
