# CUSTOMER SUCCESS PLAYBOOKS

## Servicio
`ICustomerPlaybookService` → `CustomerPlaybookService`

## Playbooks
| Tipo | Tareas ejemplo | SLA típico |
|------|----------------|------------|
| Onboarding | Kick-off, Config, Training | 1–7 días |
| Adoption | Usage review, QBR ligero | 14–30 días |
| Rescue | Llamada rescate, Plan, Seguimiento | 1–7 días Urgent |
| Renewal | Review, Propuesta, Cierre | 14–28 días |
| Expansion | Discover, Oportunidad expansión | 7–14 días |
| ReEngagement | Outreach, Seguimiento | 1–5 días |

Cada tarea: `RelatedEntityType=Customer`, `TaskType` único (idempotente).

## API
`POST /api/customer/playbooks/{playbookType}?tenantId=&customerId=`

## Disparadores automáticos
- Critical health → Rescue
- Inactividad 45d → ReEngagement
- Renewal 90d → Renewal
- Churn high → Rescue
