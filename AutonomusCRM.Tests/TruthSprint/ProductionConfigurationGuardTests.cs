using AutonomusCRM.Infrastructure.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class ProductionConfigurationGuardTests
{
    private static IHostEnvironment Env(string name) =>
        Mock.Of<IHostEnvironment>(e => e.EnvironmentName == name);

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Development_skips_validation()
    {
        var ex = Record.Exception(() =>
            ProductionConfigurationGuard.Validate(Env("Development"), Config(new())));
        Assert.Null(ex);
    }

    [Fact]
    public void Staging_fails_without_database()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationGuard.Validate(Env("Staging"), Config(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('x', 32),
                ["IntegrationEncryption:Key"] = Convert.ToBase64String(new byte[32]),
                ["RabbitMQ:HostName"] = "rabbit",
                ["EventBus:Provider"] = "RabbitMQ"
            })));
        Assert.Contains("DefaultConnection", ex.Message);
    }

    [Fact]
    public void Production_fails_on_inmemory_eventbus()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationGuard.Validate(Env("Production"), Config(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost",
                ["Jwt:Key"] = new string('x', 32),
                ["IntegrationEncryption:Key"] = Convert.ToBase64String(new byte[32]),
                ["EventBus:Provider"] = "InMemory",
                ["RabbitMQ:HostName"] = "rabbit",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })));
        Assert.Contains("InMemory", ex.Message);
    }

    [Fact]
    public void Production_fails_log_email_when_simulation_disabled()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationGuard.Validate(Env("Production"), Config(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Key"] = new string('x', 32),
                ["IntegrationEncryption:Key"] = Convert.ToBase64String(new byte[32]),
                ["EventBus:Provider"] = "RabbitMQ",
                ["RabbitMQ:HostName"] = "rabbit",
                ["Communications:AllowSimulation"] = "false",
                ["Communications:EmailProvider"] = "Log"
            })));
        Assert.Contains("EmailProvider=Log", ex.Message);
    }

    [Fact]
    public void Staging_passes_with_minimal_valid_config()
    {
        var ex = Record.Exception(() =>
            ProductionConfigurationGuard.Validate(Env("Staging"), Config(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost",
                ["Jwt:Key"] = new string('x', 32),
                ["IntegrationEncryption:Key"] = Convert.ToBase64String(new byte[32]),
                ["EventBus:Provider"] = "RabbitMQ",
                ["RabbitMQ:HostName"] = "rabbit"
            })));
        Assert.Null(ex);
    }
}
