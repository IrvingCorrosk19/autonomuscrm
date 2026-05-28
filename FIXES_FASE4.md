# FIXES_FASE4

| Componente | Cambio |
|------------|--------|
| `ICurrentTenantAccessor` / `CurrentTenantAccessor` | AsyncLocal + scoped |
| `ApplicationDbContext` | Global query filters 10 entidades |
| `TenantScopeMiddleware` | Sync tenant post-auth |
| `TenantScopedCacheService` | Cache keys por tenant |
| `ResilientRabbitMQEventBus` | DLX, idempotencia, poison DB |
| `PlatformExtensions` | OpenTelemetry + Npgsql retry |
| `Worker.cs` | Scoped agents per message |
| `TenantProvisioningService` | SaaS provision API-ready |
| `FailedEventMessage` + migración | Phase4 |
| `LoginCommandHandler` | Tenant context login |
| `SecurityHeadersMiddleware` | CSP |
| `Program.cs` | OTel + login rate limit |
| `.github/workflows/platform-ci.yml` | CI |
| `tests/load/run-load-phase4.ps1` | Load smoke |
