using AutonomusCRM.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AutonomusCRM.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=localhost;Port=5432;Database=autonomuscrm_test;Username=postgres;Password=Panama2020$",
                ["Jwt:Key"] = "Test-SuperSecretKey-AtLeast32Characters-Long!",
                ["EventBus:Provider"] = "InMemory",
                ["Seed:Enabled"] = "true",
                ["Seed:AdminPassword"] = "Admin123!"
            });
        });
    }
}
