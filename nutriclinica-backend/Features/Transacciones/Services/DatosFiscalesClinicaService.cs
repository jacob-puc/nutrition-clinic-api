using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.Transacciones.DTOs;
using nutriclinica_backend.Features.Transacciones.Interfaces;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Transacciones.Services;

public class DatosFiscalesClinicaService : IDatosFiscalesClinicaService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<ActualizarDatosFiscalesClinicaDto> _validator;

    public DatosFiscalesClinicaService(
        ApplicationDbContext context,
        IValidator<ActualizarDatosFiscalesClinicaDto> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<DatosFiscalesClinicaRespuestaDto> ObtenerAsync()
    {
        var datos = await _context.DatosFiscalesClinica
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException(
                "Todavía no se han configurado los datos fiscales de la clínica.");

        return MapToDto(datos);
    }

    public async Task<DatosFiscalesClinicaRespuestaDto> ActualizarAsync(
        ActualizarDatosFiscalesClinicaDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        var datos = await _context.DatosFiscalesClinica.FirstOrDefaultAsync();

        if (datos is null)
        {
            datos = new DatosFiscalesClinica { Id = Guid.NewGuid() };
            _context.DatosFiscalesClinica.Add(datos);
        }

        datos.RazonSocial = dto.RazonSocial.Trim();
        datos.RFC = dto.RFC.Trim().ToUpperInvariant();
        datos.RegimenFiscal = dto.RegimenFiscal;
        datos.CodigoPostal = dto.CodigoPostal.Trim();
        datos.Direccion = dto.Direccion.Trim();
        datos.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        datos.Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim();
        datos.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();
        datos.Serie = dto.Serie.Trim().ToUpperInvariant();
        datos.TarifaIva = dto.TarifaIva;
        datos.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(datos);
    }

    private static DatosFiscalesClinicaRespuestaDto MapToDto(DatosFiscalesClinica datos) => new(
        datos.Id,
        datos.RazonSocial,
        datos.RFC,
        datos.RegimenFiscal,
        datos.CodigoPostal,
        datos.Direccion,
        datos.Telefono,
        datos.Correo,
        datos.LogoUrl,
        datos.Serie,
        datos.TarifaIva,
        datos.FechaActualizacion);
}
