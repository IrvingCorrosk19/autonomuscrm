using AutonomusCRM.API;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Application.DatabaseIntelligence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                ["Seed:EnsureRoleUsers"] = "true",
                ["Jwt:Key"] = "DevOnly-SuperSecretKey-AtLeast32Characters-Long!",
                ["Jwt:Issuer"] = "AutonomusCRM",
                ["Jwt:Audience"] = "AutonomusCRM",
                ["IntegrationEncryption:Key"] = "Q0ktSW50ZWdyYXRpb25FbmNyeXB0aW9uS2V5MzJCeXRlc01pbg==",
                ["EventBus:Provider"] = "InMemory",
                ["AI:Enabled"] = "true",
                ["DataHub:Security:EncryptStorage"] = "true",
                ["DataHub:Security:ActiveEncryptionKeyId"] = "v1",
                ["DataHub:Security:EncryptionKeys:v1"] = "QXV0b25vbXVzQ1JNLURhdGFIdWItQUVTMjU2LUtleSEh",
                ["DataHub:Security:RequireMalwareScan"] = "true",
                ["DataHub:Security:MaxImportsPerHour"] = "100000",
                ["DataHub:Security:MaxExportsPerHour"] = "100000",
                ["DataHub:Security:MaxConcurrentJobs"] = "100",
                ["DatabaseIntelligence:Security:ActiveEncryptionKeyId"] = "v1",
                ["DatabaseIntelligence:Security:EncryptionKeys:v1"] = "QXV0b25vbXVzQ1JNLURhdGFIdWItQUVTMjU2LUtleSEh",
                ["DatabaseIntelligence:Security:ConnectionTimeoutSeconds"] = "15",
                ["DatabaseIntelligence:Security:MaxConnectionsPerTenant"] = "1000"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<DbIntelligenceSecurityOptions>(o => o.MaxConnectionsPerTenant = 1000);
            services.PostConfigure<DataHubSecurityOptions>(o =>
            {
                o.MaxImportsPerHour = 100_000;
                o.MaxExportsPerHour = 100_000;
                o.MaxConcurrentJobs = 100;
            });
        });
    }
}

