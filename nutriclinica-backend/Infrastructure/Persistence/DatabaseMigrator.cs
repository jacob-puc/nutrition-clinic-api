using Microsoft.EntityFrameworkCore;

namespace nutriclinica_backend.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static async Task MigrateAsync(
        this IServiceProvider services,
        ILogger logger,
        int maxAttempts = 5)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

                if (pending.Count == 0)
                {
                    logger.LogInformation("Base de datos al dia.");
                    return;
                }

                logger.LogInformation(
                    "Aplicando {Count} migracion(es): {Migrations}",
                    pending.Count,
                    string.Join(", ", pending));

                await db.Database.MigrateAsync();

                logger.LogInformation("Migraciones aplicadas.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Intento {Attempt}/{MaxAttempts} fallo. Reintentando en {Delay}s.",
                    attempt,
                    maxAttempts,
                    attempt * 2);

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }
    }
}
