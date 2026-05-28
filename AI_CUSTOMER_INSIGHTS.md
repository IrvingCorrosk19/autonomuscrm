# AI CUSTOMER INSIGHTS

## Agente
`CustomerInsightsAgent` → `ICustomerInsightsAgentService`

## Acciones (no decorativo)
| Detección | Acción |
|-----------|--------|
| Insight High/Medium actionable | Tarea `Intel_CustomerInsight` / `Intel_Anomaly` |
| Churn prob ≥ 75% | Playbook Rescue |
| Expansion Ready | Tarea `Intel_Recommendation` |

## Scan
Worker cada 15 min + `POST /api/intelligence/scan`

Config: `CustomerInsightsAgent` en tenant settings.
