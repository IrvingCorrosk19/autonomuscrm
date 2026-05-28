using AutonomusCRM.Domain.Events;
using AutonomusCRM.Infrastructure.Persistence.EventStore;

namespace AutonomusCRM.Infrastructure.Events;

internal static class DomainEventRouting
{
  private static readonly Lazy<Dictionary<Type, string>> ClrToEventType = new(() =>
      DomainEventTypeRegistry.GetRegisteredTypes()
          .ToDictionary(x => x.Value, x => x.Key));

    public static string GetRoutingKey<T>() where T : IDomainEvent
        => GetRoutingKey(typeof(T));

    public static string GetRoutingKey(Type eventClrType)
    {
        if (ClrToEventType.Value.TryGetValue(eventClrType, out var eventType))
            return eventType;

        return eventClrType.Name;
    }
}
