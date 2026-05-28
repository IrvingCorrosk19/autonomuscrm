# COMMERCIAL_SLA_ENGINE

## SLAs implementados
| Evento | SLA | TaskType |
|--------|-----|----------|
| Lead Created | 24h contacto | SLA_LeadContact24h |
| Lead Qualified | 48h seguimiento | SLA_QualifiedFollowUp |
| Deal At Risk | rescate 24h | AtRisk (operational) |
| Deal Won | onboarding D1/D7/D30 | Onboarding_D* |

## Detección
`DetectBreachesAsync` — tareas SLA vencidas + leads New &gt;24h + deals AtRisk sin rescate.

## Enforcement
`EnforceLeadCreatedSlaAsync` en `Lead.Created` vía `IRevenueAutomationEngine`.

## API
`GET /api/revenue/sla-breaches?tenantId=`
