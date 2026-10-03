using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.Auth.Services;

namespace nutriclinica_backend.Infrastructure.Persistence;

public static class Seeder
{
    public static async Task SeedAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var correo = configuration["Seed:AdminCorreo"];
        var contrasena = configuration["Seed:AdminContrasena"];

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning(
                "Seed omitido: no se definio Seed__AdminCorreo o Seed__AdminContrasena.");
            return;
        }

        if (contrasena.Length < 8)
        {
            logger.LogError("Seed omitido: Seed__AdminContrasena debe tener al menos 8 caracteres.");
            return;
        }

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var correoNormalizado = correo.Trim().ToLower();
        var existe = await context.Nutricionistas
            .AnyAsync(n => n.CorreoElectronico.ToLower() == correoNormalizado);

        if (existe) return;

        var admin = new Nutricionista
        {
            NombreCompleto = configuration["Seed:AdminNombre"] ?? "Nutricionista Administrador",
            CorreoElectronico = correoNormalizado,
            Especialidad = configuration["Seed:AdminEspecialidad"],
            IsActive = true,
            PasswordHash = AuthService.HashPassword(new Nutricionista(), contrasena)
        };

        context.Nutricionistas.Add(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario administrador creado: {Correo}", admin.CorreoElectronico);
    }
}
