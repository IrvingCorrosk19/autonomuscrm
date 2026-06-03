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
        builder.UseEnvironment("Testing");
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
                ["EventBus:Provider"] = "InMemory",
                ["AI:Enabled"] = "true"
            });
        });
    }
}
