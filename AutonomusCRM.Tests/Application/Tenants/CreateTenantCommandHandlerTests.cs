using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Tenants.Commands;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Tenants;
using Moq;
using Xunit;

namespace AutonomusCRM.Tests.Application.Tenants;

public class CreateTenantCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateTenantAndReturnId()
    {
        // Arrange
        var tenantRepository = new Mock<ITenantRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var eventDispatcher = new Mock<IDomainEventDispatcher>();

        var handler = new CreateTenantCommandHandler(
            tenantRepository.Object,
            unitOfWork.Object,
            eventDispatcher.Object);

        var command = new CreateTenantCommand("Test Tenant", "Test Description");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        tenantRepository.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Verificar que se dispatchea al menos un evento (puede ser uno o varios)
        eventDispatcher.Verify(e => e.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}

