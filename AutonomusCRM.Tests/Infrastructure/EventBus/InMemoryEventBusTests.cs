using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Tenants.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutonomusCRM.Tests.Infrastructure.EventBus;

public class InMemoryEventBusTests
{
    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribedHandlers()
    {
        // Arrange
        var logger = NullLogger<InMemoryEventBus>.Instance;
        var eventBus = new InMemoryEventBus(logger);
        var handlerInvoked = false;

        await eventBus.SubscribeAsync<TenantCreatedEvent>(async (evt, ct) =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;
        });

        var tenantId = Guid.NewGuid();
        var domainEvent = new TenantCreatedEvent(tenantId, "Test Tenant");

        // Act
        await eventBus.PublishAsync(domainEvent);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotInvokeUnsubscribedHandlers()
    {
        // Arrange
        var logger = NullLogger<InMemoryEventBus>.Instance;
        var eventBus = new InMemoryEventBus(logger);
        var handlerInvoked = false;

        await eventBus.SubscribeAsync<TenantCreatedEvent>(async (evt, ct) =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;
        });

        var tenantId = Guid.NewGuid();
        var otherEvent = new TenantUpdatedEvent(tenantId, "Updated Tenant");

        // Act
        await eventBus.PublishAsync(otherEvent);

        // Assert
        Assert.False(handlerInvoked);
    }
}

