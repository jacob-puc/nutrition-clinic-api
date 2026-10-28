using FluentValidation;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.Transacciones.DTOs.Validators;

public class CrearFacturaValidator : AbstractValidator<CrearFacturaDto>
{
    private static readonly HashSet<RegimenFiscal> RegimenesMorales =
    [
        RegimenFiscal.GeneralDeLeyPersonasMorales,
        RegimenFiscal.PersonasMoralesConFinesNoLucrativos,
        RegimenFiscal.ActividadesAgricolasGanaderas,
        RegimenFiscal.OpcionalParaGruposDeSociedades,
        RegimenFiscal.Coordinados
    ];

    private static readonly HashSet<RegimenFiscal> RegimenesFisicas =
    [
        RegimenFiscal.SueldosYSalarios,
        RegimenFiscal.Arrendamiento,
        RegimenFiscal.EnajenacionOAdquisicionDeBienes,
        RegimenFiscal.DemasIngresos,
        RegimenFiscal.IngresosPorDividendos,
        RegimenFiscal.ActividadesEmpresarialesYProfesionales,
        RegimenFiscal.IngresosPorIntereses,
        RegimenFiscal.IngresosPorObtencionDePremios,
        RegimenFiscal.SinObligacionesFiscales,
        RegimenFiscal.IncorporacionFiscal,
        RegimenFiscal.PlataformasTecnologicas
    ];

    private static readonly HashSet<RegimenFiscal> RegimenesAmbas =
    [
        RegimenFiscal.ResidentesEnElExtranjero,
        RegimenFiscal.RegimenSimplificadoDeConfianza
    ];

    public CrearFacturaValidator()
    {
        RuleFor(x => x.PagoId)
            .NotEmpty().WithMessage("El pago es obligatorio.");

        RuleFor(x => x.RFC)
            .NotEmpty().WithMessage("El RFC es obligatorio.")
            .Must(rfc => System.Text.RegularExpressions.Regex.IsMatch(
                rfc?.Trim().ToUpperInvariant() ?? string.Empty,
                "^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$"))
            .WithMessage("El RFC no tiene un formato válido (12 caracteres para persona moral, 13 para física).")
            .Must((dto, rfc) => (rfc?.Trim().Length ?? 0) == (dto.TipoPersona == TipoPersona.Moral ? 12 : 13))
            .WithMessage("El RFC no coincide con el tipo de persona seleccionado.");

        RuleFor(x => x.RazonSocial)
            .NotEmpty().WithMessage("El nombre o razón social es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre o razón social no puede superar 200 caracteres.");

        RuleFor(x => x.RegimenFiscal)
            .IsInEnum().WithMessage("El régimen fiscal no es válido.")
            .Must((dto, regimen) => EsRegimenCompatible(dto.TipoPersona, regimen))
            .WithMessage("El régimen fiscal no corresponde al tipo de persona seleccionado.");

        RuleFor(x => x.UsoCfdi)
            .IsInEnum().WithMessage("El uso de CFDI no es válido.");

        RuleFor(x => x.CodigoPostal)
            .NotEmpty().WithMessage("El código postal es obligatorio.")
            .Matches("^[0-9]{5}$").WithMessage("El código postal debe tener 5 dígitos.");
    }

    private static bool EsRegimenCompatible(TipoPersona tipoPersona, RegimenFiscal regimen)
    {
        if (RegimenesAmbas.Contains(regimen)) return true;

        return tipoPersona == TipoPersona.Moral
            ? RegimenesMorales.Contains(regimen)
            : RegimenesFisicas.Contains(regimen);
    }
}
