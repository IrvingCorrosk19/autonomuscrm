using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.API.Infrastructure;

public static class TenantGuard
{
    public const string RequiredKey = "Api_Error_TenantRequired";

    public static Guid Require(ITenantContext tenant) =>
        tenant.TenantId ?? throw new InvalidOperationException(RequiredKey);
}
