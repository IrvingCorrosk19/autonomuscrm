# DATABASE_ENGINEERING_REPORT

## Esquema Fase 4

- Tabla nueva: `FailedEventMessages` (poison queue persistida)
- `Deals.Version` concurrency token (Fase 3)
- `WorkflowTasks` (Fase 3)

## Índices tenant (existentes)

Composite indexes en: Customers, Leads, Deals, Users, Workflows, Policies, DomainEvents, TimeSeriesMetrics, WorkflowTasks.

## Global query filters

Entidades filtradas: Customer, Lead, Deal, User, Workflow, Policy, DomainEventRecord, TimeSeriesMetric, WorkflowTask, FailedEventMessage.

**Exentas:** Tenant, Snapshot (agregado cross-tenant controlado).

## N+1 / slow queries

- OpenTelemetry EF habilitado — revisar spans >100ms en Tempo
- Dashboard queries: múltiples handlers por página (roadmap proyecciones)

## Retention (roadmap)

| Tabla | Política sugerida |
|-------|-------------------|
| DomainEvents | Archivar >90 días |
| FailedEventMessages | 30 días alerta |
| TimeSeriesMetrics | Rollup |

## Backup

PostgreSQL volume en `docker-compose.yml` — backup operacional fuera de app.
