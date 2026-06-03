using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsProduction()))
            return;

        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
            logger.LogError(ex, "Database migration failed");
            throw;
        }
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Seed:Enabled", app.Environment.IsDevelopment()))
            return;

        try
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseInitialization");
            await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
            logger.LogWarning(ex, "Database initialization/seed skipped (DB may be unavailable).");
        }
    }
}
