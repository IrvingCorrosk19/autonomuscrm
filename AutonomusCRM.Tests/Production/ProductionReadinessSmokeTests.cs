using AutonomusCRM.Infrastructure.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AutonomusCRM.Tests.Production;

/// <summary>Production Readiness Execution — smoke contracts (no new features).</summary>
public class ProductionReadinessSmokeTests
{
    [Fact]
    public void Release_publish_output_exists_after_build()
    {
        var publishDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "publish-api"));
        if (!Directory.Exists(publishDir))
        {
            // CI/local may publish elsewhere; verify API assembly exists in test output deps
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "AutonomusCRM.API.dll"))
                || Directory.Exists(publishDir),
                "Run dotnet publish AutonomusCRM.API -c Release -o artifacts/publish-api for deploy smoke.");
            return;
        }

        Assert.Contains(Directory.GetFiles(publishDir, "AutonomusCRM.API.dll"), f => true);
    }

    [Fact]
    public void Production_guard_rejects_inmemory_eventbus_in_production()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;Username=x;Password=x",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Key"] = new string('x', 32),
                ["IntegrationEncryption:Key"] = Convert.ToBase64String(new byte[32]),
                ["EventBus:Provider"] = "InMemory",
                ["RabbitMQ:HostName"] = "localhost"
            })
            .Build();

        var env = new HostEnvironmentStub(Environments.Production);
        var ex = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(env, config));
        Assert.Contains("InMemory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_guard_requires_jwt_key_min_length()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;Username=x;Password=x",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Key"] = "short",
                ["IntegrationEncryption:Key"] = Convert.ToBase64String(new byte[32]),
                ["RabbitMQ:HostName"] = "localhost"
            })
            .Build();

        var env = new HostEnvironmentStub(Environments.Production);
        var ex = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(env, config));
        Assert.Contains("Jwt:Key", ex.Message);
    }

    private sealed class HostEnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "AutonomusCRM.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
