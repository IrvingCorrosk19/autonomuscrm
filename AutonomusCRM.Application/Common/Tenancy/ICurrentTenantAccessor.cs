namespace AutonomusCRM.Application.Common.Tenancy;

/// <summary>
/// Contexto de tenant actual (HTTP, worker o seed). Usado por filtros globales EF y cache.
/// </summary>
public interface ICurrentTenantAccessor
{
  Guid? TenantId { get; set; }
  /// <summary>Desactiva filtros globales (migraciones, seed, operaciones de sistema).</summary>
  bool BypassTenantFilter { get; set; }
  string? CorrelationId { get; set; }
}
