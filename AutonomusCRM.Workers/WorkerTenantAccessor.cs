using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.Workers;

public sealed class WorkerTenantAccessor : ICurrentTenantAccessor
{
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
    public bool BypassTenantFilter { get; set; }
}
