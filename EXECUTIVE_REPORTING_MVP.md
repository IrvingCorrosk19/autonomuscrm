# EXECUTIVE_REPORTING_MVP

## Ubicación
`/Index` — barra reporting ejecutivo (datos BD)

## KPIs MVP
| KPI | Cálculo |
|-----|---------|
| Revenue closed | Σ deals ClosedWon |
| Pipeline abierto | Σ amount deals Open |
| Pipeline ponderado | Σ amount×probability/100 |
| Win rate | won/(won+lost) |
| Conversion rate | qualified leads / total leads |
| Tareas vencidas | open tasks `IsOverdue` |

## Deals
Forecast 30/60/90 ponderado por `ExpectedCloseDate`  
Win rate + revenue closed en sidebar forecast

## CEO
Puede ver snapshot real en un solo dashboard; tendencias históricas siguen en P1 (TimeSeries).
