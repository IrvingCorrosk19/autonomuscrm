using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.Infrastructure.Tenancy;

/// <summary>
/// AsyncLocal permite propagar tenant en HTTP, workers y bus sin leak entre hilos.
/// </summary>
public sealed class CurrentTenantAccessor : ICurrentTenantAccessor
{
    private sealed class State
    {
        public Guid? TenantId;
        public bool BypassTenantFilter;
        public string? CorrelationId;
    }

    private static readonly AsyncLocal<State?> Ambient = new();

    private static State Current => Ambient.Value ??= new State();

    public Guid? TenantId
    {
        get => Current.TenantId;
        set => Current.TenantId = value;
    }

    public bool BypassTenantFilter
    {
        get => Current.BypassTenantFilter;
        set => Current.BypassTenantFilter = value;
    }

    public string? CorrelationId
    {
        get => Current.CorrelationId;
        set => Current.CorrelationId = value;
    }
}
