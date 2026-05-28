# SALES_INTELLIGENCE_ANALYSIS

## `ISalesIntelligenceService`
Por deal:
1. Determina prioridad (Urgent/High/Normal) y acción recomendada por etapa/riesgo/LTV
2. **Crea WorkflowTask** (`Intel_*`) si no existe
3. Persiste `NextBestAction`, `ActionPriority` en deal metadata

## `DealStrategyAgent`
Delega a SalesIntelligence (no solo metadata decorativa).

## Valor
Vendedor recibe **tareas accionables** en `/Tasks`, no texto oculto en JSON.
