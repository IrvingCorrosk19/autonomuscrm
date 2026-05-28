# FIXES_APLICADOS — Fase 2 QA

| Fecha | Componente | Cambio |
|-------|------------|--------|
| 2026-05-27 | `EventStore.cs` | Deserialización + `CountByTenantAsync` |
| 2026-05-27 | `DomainEventTypeRegistry.cs` | Mapa tipos evento |
| 2026-05-27 | `PersistedDomainEvent.cs` | Envelope fallback |
| 2026-05-27 | `IEventStore.cs` | Contrato count |
| 2026-05-27 | `Audit.cshtml` / `Audit.cshtml.cs` | Métricas reales, sin demo |
| 2026-05-27 | `SameTenantHandler.cs` | Comparación tenant query |
| 2026-05-27 | `CommercialWriteAuthorizationMiddleware.cs` | GET Create/Edit bloqueados Viewer/Support |
| 2026-05-27 | `ApiTenantValidationMiddleware.cs` | **Nuevo** — aislamiento API |
| 2026-05-27 | `Program.cs` | `AddHttpContextAccessor`, middleware API tenant |
| 2026-05-27 | `AutonomusCRM.Application.csproj` | `FrameworkReference` AspNetCore.App |
| 2026-05-27 | `tests/e2e/run-p0-qa.ps1` | **Nuevo** — batería P0 + evidencia |

Sin hacks temporales ni hardcodes de negocio.
