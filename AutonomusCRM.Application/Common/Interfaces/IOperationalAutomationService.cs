using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface IOperationalAutomationService
{
    Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
