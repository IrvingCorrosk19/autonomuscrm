# CUSTOMER HEALTH ENGINE

## Servicio
`ICustomerHealthEngine` → `CustomerHealthEngine`

## Scores (0–100)
| Componente | Peso | Fuente de datos |
|------------|------|-----------------|
| Adoption | 20% | Tareas `Onboarding_*` completadas vs abiertas |
| Engagement | 25% | `LastContactAt` (recencia) |
| Support | 15% | Tareas abiertas/vencidas (proxy tickets) |
| Revenue | 20% | LTV + deals ClosedWon |
| Risk component | 20% | Inverso de `Customer.RiskScore` |

**Health Score** = media ponderada → clasificación:
- **Healthy** ≥ 70
- **Warning** 40–69
- **Critical** &lt; 40

## Persistencia
Metadata del cliente: `HealthScore`, `HealthClassification`, `AdoptionScore`, `EngagementScore`, `SupportScore`, `HealthCalculatedAt`.

## API
- `GET /api/customer/health?tenantId=`
- `GET /api/customer/health/{customerId}?tenantId=`

## Automatización
Scan periódico persiste health; Critical dispara playbook Rescue vía `RetentionAutomationEngine`.
