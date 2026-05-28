# CUSTOMER ENGAGEMENT & RETENTION — Fase 13

## Visión
AutonomusFlow evoluciona de **Revenue Engine** a **Customer Retention & Expansion Engine**: el ciclo post-venta (onboarding → adopción → engagement → renovación → expansión) queda automatizado, medible y accionable desde base de datos y API.

## Alcance implementado
| Bloque | Componente | Estado |
|--------|------------|--------|
| A | CustomerHealthEngine | ✓ |
| B | ChurnRiskEngine | ✓ |
| C | RenewalEngine | ✓ |
| D | CustomerPlaybookService | ✓ |
| E | EmailAutomationEngine | ✓ |
| F | WhatsAppAutomationEngine | ✓ |
| G | CustomerJourneyEngine | ✓ |
| H | ExpansionRevenueEngine | ✓ |
| I | Agentes CS (Health, Churn, Renewal, Expansion) | ✓ |
| J | RetentionAutomationEngine | ✓ |
| K | CustomerKpiService (10 KPIs) | ✓ |
| L | GET `/api/customer/dashboard` | ✓ |

## Integración con Revenue Ops (Fase 12)
- `Deal.ClosedWon` → contrato `CustomerContract`, LTV, onboarding operativo + playbooks CS.
- Tareas vía `IOperationalTaskService` / `WorkflowTasks` (misma capa que SLA e inteligencia comercial).
- `DomainEventDispatcher`: workflows → operacional → revenue → **retention** → bus.

## Entidades nuevas
- `CustomerContracts` — fechas de renovación, ARR, estado.
- `CustomerCommunicationLogs` — email/WhatsApp con tracking.

## API principal
`GET /api/customer/dashboard?tenantId={id}` — KPIs, health, churn, renewals, expansion, journey (100 % BD).

## Operación
1. Migración: `Phase13_CustomerRetention`
2. Worker activo (scan 15 min: retention + renewal + expansion)
3. Sin cambios UI/CSS (congelado)

## Referencias base
REVENUE_OPERATIONS_FOUNDATION.md, REVENUE_KPI_FRAMEWORK.md, GO_NO_GO_REVENUE_FOUNDATION.md
