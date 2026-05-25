using AutonomusCRM.Infrastructure.Persistence.Seed;

namespace AutonomusCRM.API.Extensions;

public static class WebApplicationExtensions
{
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
