using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Infrastructure.Policies;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class PolicyEngineTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task EvaluatePolicyAsync_denies_when_expression_denies()
    {
        var repo = new Mock<IPolicyRepository>();
        repo.Setup(r => r.GetActiveByTenantAndNameAsync(_tenantId, "autonomous", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Policy.Create(_tenantId, "autonomous", "deny") });

        var engine = new PolicyEngine(repo.Object, NullLogger<PolicyEngine>.Instance);
        var evt = new CustomerCreatedEvent(Guid.NewGuid(), _tenantId, "Acme");
        var result = await engine.EvaluatePolicyAsync("autonomous", evt);

        Assert.False(result.IsAllowed);
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public async Task IsActionAllowedAsync_blocks_high_risk()
    {
        var repo = new Mock<IPolicyRepository>();
        repo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Policy.Create(_tenantId, "risk", "maxRiskScore:60") });

        var engine = new PolicyEngine(repo.Object, NullLogger<PolicyEngine>.Instance);
        var allowed = await engine.IsActionAllowedAsync("executeDeal", _tenantId, new Dictionary<string, object>
        {
            ["riskScore"] = 90
        });

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsActionAllowedAsync_allows_when_policies_pass()
    {
        var repo = new Mock<IPolicyRepository>();
        repo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Policy.Create(_tenantId, "default", "allow; maxRiskScore:95") });

        var engine = new PolicyEngine(repo.Object, NullLogger<PolicyEngine>.Instance);
        var allowed = await engine.IsActionAllowedAsync("executeDeal", _tenantId, new Dictionary<string, object>
        {
            ["riskScore"] = 40,
            ["confidence"] = 0.9
        });

        Assert.True(allowed);
    }
}
