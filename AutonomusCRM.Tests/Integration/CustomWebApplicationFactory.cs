using AutonomusCRM.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AutonomusCRM.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public static string? PostgresConnectionString { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development evita UseHttpsRedirection (Testing devuelve 307 y rompe asserts HTTP en WebApplicationFactory).
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var conn = PostgresConnectionString
                ?? IntegrationTestEnvironment.ResolvePostgresConnectionString()
                ?? IntegrationTestEnvironment.DefaultCiConnectionString;

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = conn,
                ["Database:AutoMigrate"] = "true",
                ["Seed:Enabled"] = "true",
                ["Seed:AdminPassword"] = "Admin123!",
                ["Jwt:Key"] = "Test-SuperSecretKey-AtLeast32Characters-Long!",
                ["IntegrationEncryption:Key"] = "Q0ktSW50ZWdyYXRpb25FbmNyeXB0aW9uS2V5MzJCeXRlc01pbg==",
                ["EventBus:Provider"] = "InMemory",
                ["AI:Enabled"] = "true"
            });
        });
    }
}
