using AutonomusCRM.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AutonomusCRM.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>When set (e.g. by Testcontainers), integration tests use isolated PostgreSQL.</summary>
    public static string? PostgresConnectionString { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    PostgresConnectionString
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=localhost;Port=5432;Database=autonomuscrm_test;Username=postgres;Password=Panama2020$",
                ["Seed:Enabled"] = PostgresConnectionString != null ? "false" : "true",
                ["Jwt:Key"] = "Test-SuperSecretKey-AtLeast32Characters-Long!",
                ["EventBus:Provider"] = "InMemory",
                ["Seed:Enabled"] = "true",
                ["Seed:AdminPassword"] = "Admin123!"
            });
        });
    }
}
