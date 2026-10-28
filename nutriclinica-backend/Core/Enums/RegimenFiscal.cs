namespace nutriclinica_backend.Core.Enums;

public enum RegimenFiscal
{
    
    // Persona Moral: 601, 603, 622, 623, 624
    // Persona Física: 605, 606, 607, 608, 611, 612, 614, 615, 616, 621, 625
    // Ambas:          610, 626

    /// <summary>601 - General de Ley Personas Morales (Moral)</summary>
    GeneralDeLeyPersonasMorales = 601,
    /// <summary>603 - Personas Morales con Fines no Lucrativos (Moral)</summary>
    PersonasMoralesConFinesNoLucrativos = 603,
    /// <summary>605 - Sueldos y Salarios e Ingresos Asimilados a Salarios (Física)</summary>
    SueldosYSalarios = 605,
    /// <summary>606 - Arrendamiento (Física)</summary>
    Arrendamiento = 606,
    /// <summary>607 - Enajenación o Adquisición de Bienes (Física)</summary>
    EnajenacionOAdquisicionDeBienes = 607,
    /// <summary>608 - Demás Ingresos (Física)</summary>
    DemasIngresos = 608,
    /// <summary>610 - Residentes en el Extranjero sin Establecimiento Permanente (ambas)</summary>
    ResidentesEnElExtranjero = 610,
    /// <summary>611 - Ingresos por Dividendos (Física)</summary>
    IngresosPorDividendos = 611,
    /// <summary>612 - Personas Físicas con Actividades Empresariales y Profesionales (Física)</summary>
    ActividadesEmpresarialesYProfesionales = 612,
    /// <summary>614 - Ingresos por Intereses (Física)</summary>
    IngresosPorIntereses = 614,
    /// <summary>615 - Ingresos por Obtención de Premios (Física)</summary>
    IngresosPorObtencionDePremios = 615,
    /// <summary>616 - Sin Obligaciones Fiscales (Física)</summary>
    SinObligacionesFiscales = 616,
    /// <summary>621 - Incorporación Fiscal (Física)</summary>
    IncorporacionFiscal = 621,
    /// <summary>622 - Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras (Moral)</summary>
    ActividadesAgricolasGanaderas = 622,
    /// <summary>623 - Opcional para Grupos de Sociedades (Moral)</summary>
    OpcionalParaGruposDeSociedades = 623,
    /// <summary>624 - Coordinados (Moral)</summary>
    Coordinados = 624,
    /// <summary>625 - Actividades Empresariales con ingresos por Plataformas Tecnológicas (Física)</summary>
    PlataformasTecnologicas = 625,
    /// <summary>626 - Régimen Simplificado de Confianza (RESICO, ambas)</summary>
    RegimenSimplificadoDeConfianza = 626

}