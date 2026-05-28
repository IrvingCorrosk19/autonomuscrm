# RETENTION AUTOMATIONS

## Motor
`IRetentionAutomationEngine` → `RetentionAutomationEngine`

## Reglas
| Trigger | Acción |
|---------|--------|
| Health ↓ Critical | Playbook Rescue + email risk |
| Renewal 90/60/30d | Tareas + playbook Renewal |
| Cliente inactivo 45d | Playbook ReEngagement + WhatsApp recovery |
| Deal ClosedWon | Contrato + LTV + onboarding email |
| Customer.Created | Playbook Onboarding + journey metadata |
| Risk ≥ 70 | Playbook Rescue |

## Pipeline eventos
`DomainEventDispatcher` invoca retention tras revenue.

## Scan periódico (Worker, 15 min)
`RunPeriodicRetentionScanAsync` por tenant.

## API
`POST /api/customer/scan?tenantId=`
