# SALES_PERFORMANCE_ENGINE

## Objetivo
Cuotas, cumplimiento, ranking y cobertura por vendedor.

## Modelo `SalesQuota`
- `PeriodType`: Monthly, Quarterly, Yearly
- `TargetAmount`, `PeriodStart`, `PeriodEnd`, `UserId`

## `RepPerformanceDto`
- RevenueClosed (mes actual)
- AttainmentPercent = closed / quota
- PipelineCoveragePercent = openWeighted / quota
- Rank en leaderboard

## API
`GET /api/revenue/leaderboard?tenantId=`  
`POST /api/revenue/quotas` — definir metas

## Uso gerencial
Identifica quién vende más, quién está atrasado (attainment &lt; 100%), quién necesita pipeline (coverage &lt; 300%).
