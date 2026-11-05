using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Features.Auth.Services;

namespace nutriclinica_backend.Infrastructure.Persistence;

public static class Seeder
{
    public static async Task SeedAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await SeedDatosFiscalesAsync(context, logger);
        await MedidasCaserasSeeder.SeedAsync(context, logger);

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

        var correoNormalizado = correo.Trim().ToLower();
        var existe = await context.Nutricionistas
            .AnyAsync(n => n.CorreoElectronico.ToLower() == correoNormalizado);

        if (existe) return;

        var admin = new Nutricionista
        {
            NombreCompleto = configuration["Seed:AdminNombre"] ?? "Nutricionista Administrador",
            CorreoElectronico = correoNormalizado,
            Especialidad = configuration["Seed:AdminEspecialidad"],
            Rol = RolUsuario.Administrador,
            IsActive = true,
            PasswordHash = AuthService.HashPassword(new Nutricionista(), contrasena)
        };

        context.Nutricionistas.Add(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario administrador creado: {Correo}", admin.CorreoElectronico);
    }

    private static async Task SeedDatosFiscalesAsync(
        ApplicationDbContext context,
        ILogger logger)
    {
        if (await context.DatosFiscalesClinica.AnyAsync()) return;

        context.DatosFiscalesClinica.Add(new DatosFiscalesClinica
        {
            Id = Guid.NewGuid(),
            RazonSocial = "NutriClínica S.A. de C.V.",
            RFC = "NUT250101AB1",
            RegimenFiscal = RegimenFiscal.GeneralDeLeyPersonasMorales,
            CodigoPostal = "64000",
            Direccion = "Av. Constitución 1000, Centro, Monterrey, N.L.",
            Telefono = "8180000000",
            Correo = "contacto@nutriclinica.cl",
            Serie = "A",
            TarifaIva = 16m,
            FechaActualizacion = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Datos fiscales de la clinica creados con valores por defecto.");
    }
}
