namespace nutriclinica_backend.Core.Utils;

public static class CalculadoraClinica
{
    public static int? CalcularEdad(DateOnly? fechaNacimiento)
    {
        if (!fechaNacimiento.HasValue) return null;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var edad = hoy.Year - fechaNacimiento.Value.Year;
        if (fechaNacimiento.Value.AddYears(edad) > hoy) edad--;

        return edad < 0 ? null : edad;
    }

    public static decimal CalcularImc(decimal pesoKg, decimal estaturaCm)
    {
        if (estaturaCm <= 0) return 0m;

        var estaturaMetros = estaturaCm / 100m;
        return Math.Round(pesoKg / (estaturaMetros * estaturaMetros), 2);
    }
}
