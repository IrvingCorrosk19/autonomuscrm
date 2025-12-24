using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Tenants.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Xunit;

namespace AutonomusCRM.Tests.Infrastructure.EventBus;

public class InMemoryEventBusTests
{
    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribedHandlers()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var handlerInvoked = false;

        await eventBus.SubscribeAsync<TenantCreatedEvent>(async (evt, ct) =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;
        });

        var domainEvent = new TenantCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Test Tenant");

        // Act
        await eventBus.PublishAsync(domainEvent);

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotInvokeUnsubscribedHandlers()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var handlerInvoked = false;

        await eventBus.SubscribeAsync<TenantCreatedEvent>(async (evt, ct) =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;
        });

        var otherEvent = new TenantUpdatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Updated Tenant");

        // Act
        await eventBus.PublishAsync(otherEvent);

        // Assert
        Assert.False(handlerInvoked);
    }
}

