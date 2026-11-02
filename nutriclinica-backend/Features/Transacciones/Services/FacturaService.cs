using FluentValidation;
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

public class FacturaService : IFacturaService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CrearFacturaDto> _crearValidator;
    private readonly IValidator<FacturaFiltroDto> _filtroValidator;

    public FacturaService(
        ApplicationDbContext context,
        IValidator<CrearFacturaDto> crearValidator,
        IValidator<FacturaFiltroDto> filtroValidator)
    {
        _context = context;
        _crearValidator = crearValidator;
        _filtroValidator = filtroValidator;
    }

    public async Task<FacturaRespuestaDto> EmitirFacturaAsync(CrearFacturaDto dto)
    {
        await _crearValidator.ValidateAndThrowAsync(dto);

        var pago = await _context.Pagos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.PagoId)
            ?? throw new KeyNotFoundException($"No se encontró el pago con ID: {dto.PagoId}");

        if (pago.Estado != EstadoPago.Pagado)
        {
            throw new ConflictException("No se puede facturar un pago anulado.");
        }

        var facturaExistente = await _context.Facturas
            .AsNoTracking()
            .AnyAsync(f => f.PagoId == dto.PagoId && f.Estatus != EstatusFactura.Anulada);

        if (facturaExistente)
        {
            throw new ConflictException("El pago ya tiene una factura emitida.");
        }

        var clinica = await _context.DatosFiscalesClinica
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new ConflictException(
                "No hay datos fiscales de la clínica configurados. Configúralos antes de facturar.");

        if (string.IsNullOrWhiteSpace(clinica.RFC) ||
            string.IsNullOrWhiteSpace(clinica.RazonSocial) ||
            string.IsNullOrWhiteSpace(clinica.CodigoPostal))
        {
            throw new ConflictException(
                "Los datos fiscales de la clínica están incompletos. Configúralos antes de facturar.");
        }

        var subtotal = Math.Round(pago.Monto / (1 + (clinica.TarifaIva / 100m)), 2,
            MidpointRounding.AwayFromZero);
        var iva = Math.Round(pago.Monto - subtotal, 2, MidpointRounding.AwayFromZero);

        var factura = new Factura
        {
            Id = Guid.NewGuid(),
            PagoId = pago.Id,
            TipoPersona = dto.TipoPersona,
            RFC = dto.RFC.Trim().ToUpperInvariant(),
            NombreRazonSocial = dto.RazonSocial.Trim(),
            RegimenFiscal = dto.RegimenFiscal,
            UsoCfdi = dto.UsoCfdi,
            CodigoPostal = dto.CodigoPostal.Trim(),
            Serie = clinica.Serie,
            Folio = await GenerarFolioAsync(clinica.Serie),
            FechaEmision = DateTime.UtcNow,
            UUID = null,
            Subtotal = subtotal,
            TarifaIva = clinica.TarifaIva,
            Iva = iva,
            Total = pago.Monto,
            Estatus = EstatusFactura.Emitida,
            FechaCreacion = DateTime.UtcNow,
            IsActive = true
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        return await ObtenerFacturaPorIdAsync(factura.Id);
    }

    public async Task<RespuestaPaginadaDto<FacturaRespuestaDto>> ObtenerFacturasAsync(
        FacturaFiltroDto filtro)
    {
        await _filtroValidator.ValidateAndThrowAsync(filtro);

        var query = _context.Facturas.AsNoTracking().AsQueryable();

        if (filtro.Estatus.HasValue)
        {
            query = query.Where(f => f.Estatus == filtro.Estatus.Value);
        }

        if (filtro.PacienteId.HasValue)
        {
            query = query.Where(f => f.Pago.PacienteId == filtro.PacienteId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.RFC))
        {
            var rfc = filtro.RFC.Trim().ToUpperInvariant();
            query = query.Where(f => f.RFC.Contains(rfc));
        }

        if (filtro.FechaInicio.HasValue)
        {
            query = query.Where(f => f.FechaEmision >= filtro.FechaInicio.Value);
        }

        if (filtro.FechaFin.HasValue)
        {
            query = query.Where(f => f.FechaEmision <= filtro.FechaFin.Value);
        }

        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, 100);
        var totalRegistros = await query.CountAsync();

        var datos = await query
            .OrderByDescending(f => f.FechaEmision)
            .ThenByDescending(f => f.Folio)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new RespuestaPaginadaDto<FacturaRespuestaDto>
        {
            TotalRegistros = totalRegistros,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanoPagina),
            Datos = datos.Select(MapToDto).ToList()
        };
    }

    public async Task<FacturaRespuestaDto> ObtenerFacturaPorIdAsync(Guid facturaId)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == facturaId)
            ?? throw new KeyNotFoundException($"No se encontró la factura con ID: {facturaId}");

        return MapToDto(factura);
    }

    public async Task<byte[]> GenerarFacturaPdfAsync(Guid facturaId)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .Include(f => f.Pago)
                .ThenInclude(p => p.Paciente)
            .Include(f => f.Pago)
                .ThenInclude(p => p.Consulta)
            .FirstOrDefaultAsync(f => f.Id == facturaId)
            ?? throw new KeyNotFoundException($"No se encontró la factura con ID: {facturaId}");

        var clinica = await _context.DatosFiscalesClinica
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return TransaccionesPdf.FacturaPdf(
            factura,
            factura.Pago,
            factura.Pago.Paciente.NombreCompleto,
            ConstruirConcepto(factura.Pago.Consulta),
            clinica);
    }

    private async Task<int> GenerarFolioAsync(string serie)
    {
        var folios = await _context.Facturas
            .AsNoTracking()
            .Where(f => f.Serie == serie)
            .Select(f => f.Folio)
            .ToListAsync();

        return folios.DefaultIfEmpty(0).Max() + 1;
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

    private static FacturaRespuestaDto MapToDto(Factura factura) => new(
        factura.Id,
        factura.PagoId,
        factura.TipoPersona,
        factura.RFC,
        factura.NombreRazonSocial,
        factura.RegimenFiscal,
        factura.UsoCfdi,
        factura.CodigoPostal,
        factura.Serie,
        factura.Folio,
        factura.FechaEmision,
        factura.UUID,
        factura.Subtotal,
        factura.TarifaIva,
        factura.Iva,
        factura.Total,
        factura.Estatus,
        factura.FechaCreacion);
}
