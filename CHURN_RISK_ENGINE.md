# CHURN RISK ENGINE

## Servicio
`IChurnRiskEngine` → `ChurnRiskEngine`

## Señales detectadas
| SignalType | Severidad | Condición |
|------------|-----------|-----------|
| LowHealth | High | Health Critical |
| Inactivity | High | Sin contacto &gt; 60 días |
| IncompleteOnboarding | Medium | Adoption &lt; 40 |
| OverdueTasks | Medium | Tareas vencidas |
| LowUsage | Medium | Engagement &lt; 30 |
| OpenSupport | Low | Tareas tipo soporte abiertas |

## Acciones
- Tarea `ChurnRisk_Alert` (Urgent, 48h SLA)
- Playbook **Rescue** automático en clientes High severity

## API
`GET /api/customer/churn-signals?tenantId=`

## Agente
`ChurnRiskAgent` — reacciona a `Customer.RiskScoreUpdated` (risk ≥ 60) vía `ICustomerSuccessIntelligenceService`.
