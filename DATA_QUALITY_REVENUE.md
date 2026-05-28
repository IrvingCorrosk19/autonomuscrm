# DATA_QUALITY_REVENUE

## `IDataQualityRevenueService`
Detecta y crea tareas:
| Issue | TaskType |
|-------|----------|
| Deal sin owner | DQ_NoOwner |
| Deal sin fecha cierre | DQ_NoCloseDate |
| Lead abandonado &gt;7d | DQ_AbandonedLead |
| Email duplicado | DQ_DuplicateEmail |
| Deal huérfano | DQ_OrphanDeal |

## Ejecución
Worker cada 15 min + `POST /api/revenue/scan`.

## Impacto revenue
Datos limpios → forecast y asignación confiables.
