# RENEWAL ENGINE

## Servicio
`IRenewalEngine` → `RenewalEngine`

## Contratos
`CustomerContract` creado en `Deal.ClosedWon` (12 meses, ARR = amount × 12).

## Ventanas
| Ventana | TaskType | SLA prioridad |
|---------|----------|---------------|
| 90 días | `Renewal_90d` | High |
| 60 días | `Renewal_60d` | High |
| 30 días | `Renewal_30d` | Urgent |

Playbook **Renewal** al entrar ventana 90d.

## Forecast
`RenewalForecastDto`: ARR esperado, contratos en ventana, ARR at-risk (≤30d).

## API
- `GET /api/customer/renewals?tenantId=`
- `GET /api/customer/renewal-forecast?tenantId=&horizonDays=90`

## Agente
`RenewalAgent` — scan periódico `EnforceRenewalWindowsAsync`.
