# DEAL_AT_RISK_AUTOMATION

## Trigger
`DealStrategyAgent` tras `Deal.Created` / `Deal.StageChanged` (si agente habilitado en Settings).

## Detección `isAtRisk`
- Probabilidad &lt; umbral y días &gt; `RiskDaysThreshold` (config)
- Cliente `RiskScore` &gt; 70
- `ExpectedCloseDate` vencida

## Acciones
1. `deal.Metadata["AtRisk"] = "true"`
2. `deal.Metadata["StrategySummary"]` texto operativo
3. **WorkflowTask** Urgent, vencimiento +1 día, tipo `AtRisk` (sin duplicar si ya existe)

## Visibilidad vendedor
- Dashboard: contador deals en riesgo (metadata + fallback prob.)
- Enlace a `/Tasks` para tareas vencidas
