# REAL_DASHBOARD_VALIDATION

## Cambios
- `/Dashboard` redirige a `/Index` (única fuente operativa)
- Index KPIs 100% desde `GetLeadsByTenant` / `GetDealsByTenant` / repositorio deals
- **Eliminado** forecast mock $310K/$185K/$142K en `Deals.cshtml`
- Forecast 30/60/90 calculado en `DealsModel` desde BD
- Index: pipeline ponderado, revenue closed, win rate, conversión, tareas vencidas

## Métricas verificables
| Métrica | Fuente |
|---------|--------|
| Leads 24h | `LeadDto.CreatedAt` |
| Conversión | Qualified / Total |
| Deals riesgo | metadata `AtRisk` o prob&lt;50 |
| Pipeline ponderado | Σ amount×prob/100 |
| Win rate | ClosedWon / (Won+Lost) |

## No usar
`Dashboard.cshtml` estático para decisiones (redirige).
