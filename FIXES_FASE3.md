# FIXES_FASE3

| Fecha | Componente | Cambio |
|-------|------------|--------|
| 2026-05-27 | `TenantIds`, `ITenantContext`, `TenantContext` | Contexto tenant enterprise |
| 2026-05-27 | `QaTenantSeeder` | Tenant QA-B + admin + datos exclusivos |
| 2026-05-27 | `Tenant.CreateWithId` | IDs estables seed |
| 2026-05-27 | `GetLeadByIdQuery` + `LeadsController` | IDOR fix API leads |
| 2026-05-27 | `WorkflowEngine` | Assign, UpdateStatus, CreateTask |
| 2026-05-27 | `WorkflowTask` + migración | Tareas operativas workflow |
| 2026-05-27 | `Deal.Version` | Concurrencia optimista |
| 2026-05-27 | `ImportGuard` + Leads Import | Límites importación |
| 2026-05-27 | `CorrelationIdMiddleware` | Observabilidad |
| 2026-05-27 | `DomainEventRouting` + `RabbitMQEventBus` | Fix event bus |
| 2026-05-27 | UX Audit/Customers/Deals | Botones disabled vs alert falso |
| 2026-05-27 | `tests/e2e/run-phase3-qa.ps1` | Batería Fase 3 |
| 2026-05-27 | `tests/qa-data/*` | Fixtures import |

Sin hacks temporales ni bypass de seguridad.
