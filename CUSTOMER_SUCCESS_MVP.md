# CUSTOMER_SUCCESS_MVP

## Trigger
`Deal.Closed` con stage `ClosedWon` en `OperationalAutomationService`

## Playbook automático
| Día | Tarea |
|-----|-------|
| 0 | Onboarding CS — Día 1 (Urgent) |
| 7 | Onboarding CS — Día 7 |
| 30 | Onboarding CS — Día 30 |

## Características
- Tipo `Onboarding_D0`, `Onboarding_D7`, `Onboarding_D30`
- Asignado al owner del deal
- DueDate calculado desde cierre
- Sin duplicar si tarea mismo tipo ya abierta

## CS operativo
Cola visible en `/Tasks` filtro Open + prioridad Urgent para día 1.
