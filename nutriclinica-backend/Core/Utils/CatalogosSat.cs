using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Utils;

/// <summary>
/// Claves del SAT con su descripcion, para imprimir "601 - General de Ley
/// Personas Morales" en lugar del codigo solo. Mismo texto que el catalogo
/// del front (features/cobros/lib/cobros.ts).
/// </summary>
public static class CatalogosSat
{
    private static readonly Dictionary<RegimenFiscal, string> Regimenes = new()
    {
        [RegimenFiscal.GeneralDeLeyPersonasMorales] = "General de Ley Personas Morales",
        [RegimenFiscal.PersonasMoralesConFinesNoLucrativos] = "Personas Morales con Fines no Lucrativos",
        [RegimenFiscal.SueldosYSalarios] = "Sueldos y Salarios e Ingresos Asimilados a Salarios",
        [RegimenFiscal.Arrendamiento] = "Arrendamiento",
        [RegimenFiscal.EnajenacionOAdquisicionDeBienes] = "Enajenación o Adquisición de Bienes",
        [RegimenFiscal.DemasIngresos] = "Demás ingresos",
        [RegimenFiscal.ResidentesEnElExtranjero] = "Residentes en el Extranjero sin Establecimiento Permanente en México",
        [RegimenFiscal.IngresosPorDividendos] = "Ingresos por Dividendos",
        [RegimenFiscal.ActividadesEmpresarialesYProfesionales] = "Personas Físicas con Actividades Empresariales y Profesionales",
        [RegimenFiscal.IngresosPorIntereses] = "Ingresos por intereses",
        [RegimenFiscal.IngresosPorObtencionDePremios] = "Régimen de los ingresos por obtención de premios",
        [RegimenFiscal.SinObligacionesFiscales] = "Sin obligaciones fiscales",
        [RegimenFiscal.IncorporacionFiscal] = "Incorporación Fiscal",
        [RegimenFiscal.ActividadesAgricolasGanaderas] = "Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras",
        [RegimenFiscal.OpcionalParaGruposDeSociedades] = "Opcional para Grupos de Sociedades",
        [RegimenFiscal.Coordinados] = "Coordinados",
        [RegimenFiscal.PlataformasTecnologicas] = "Actividades Empresariales con ingresos a través de Plataformas Tecnológicas",
        [RegimenFiscal.RegimenSimplificadoDeConfianza] = "Régimen Simplificado de Confianza",
    };

    private static readonly Dictionary<UsoCfdi, string> Usos = new()
    {
        [UsoCfdi.G01] = "Adquisición de mercancías",
        [UsoCfdi.G02] = "Devoluciones, descuentos o bonificaciones",
        [UsoCfdi.G03] = "Gastos en general",
        [UsoCfdi.I01] = "Construcciones",
        [UsoCfdi.I02] = "Mobiliario y equipo de oficina por inversiones",
        [UsoCfdi.I03] = "Equipo de transporte",
        [UsoCfdi.I04] = "Equipo de cómputo y accesorios",
        [UsoCfdi.I05] = "Dados, troqueles, moldes, matrices y herramental",
        [UsoCfdi.I06] = "Comunicaciones telefónicas",
        [UsoCfdi.I07] = "Comunicaciones satelitales",
        [UsoCfdi.I08] = "Otra maquinaria y equipo",
        [UsoCfdi.D01] = "Honorarios médicos, dentales y gastos hospitalarios",
        [UsoCfdi.D02] = "Gastos médicos por incapacidad o discapacidad",
        [UsoCfdi.D03] = "Gastos funerales",
        [UsoCfdi.D04] = "Donativos",
        [UsoCfdi.D06] = "Aportaciones voluntarias al SAR",
        [UsoCfdi.D07] = "Primas por seguros de gastos médicos",
        [UsoCfdi.D08] = "Gastos de transportación escolar obligatoria",
        [UsoCfdi.D09] = "Depósitos en cuentas para el ahorro, primas de pensiones",
        [UsoCfdi.D10] = "Pagos por servicios educativos (colegiaturas)",
        [UsoCfdi.S01] = "Sin efectos fiscales",
        [UsoCfdi.CP01] = "Pagos",
        [UsoCfdi.CN01] = "Nómina",
    };

    /// <summary>"601 - General de Ley Personas Morales".</summary>
    public static string Regimen(RegimenFiscal regimen) =>
        Regimenes.TryGetValue(regimen, out var texto) ? $"{(int)regimen} - {texto}" : ((int)regimen).ToString();

    /// <summary>"D01 - Honorarios médicos, dentales y gastos hospitalarios".</summary>
    public static string Uso(UsoCfdi uso) =>
        Usos.TryGetValue(uso, out var texto) ? $"{uso} - {texto}" : uso.ToString();

    public static string Persona(TipoPersona tipo) => tipo == TipoPersona.Moral ? "Persona moral" : "Persona física";
}
