namespace nutriclinica_backend.Infrastructure.Security;

public class JwtConfig
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 1;
    public int RefreshExpirationDays { get; set; } = 7;

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Secret) || Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret no configurado o menor a 32 caracteres. "
                + "Defina Jwt__Secret como variable de entorno.");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer y Jwt:Audience son obligatorios. "
                + "Defina Jwt__Issuer y Jwt__Audience como variables de entorno.");
        }
    }
}
