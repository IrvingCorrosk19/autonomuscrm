# CHURN PREDICTION V2

`IChurnPredictionV2` → `ChurnPredictionV2Service`

## Mejoras sobre V1
- Señales `ChurnRiskEngine` + **histórico snapshots** (14d)
- Tendencias Health / Engagement (`HealthTrendDown`, `EngagementTrendDown`)
- Probabilidad churn 0–100 con factores y dirección (Declining/Stable/Improving)

## ChurnRiskEngine ampliado
Lee `CustomerAnalyticsSnapshots` para tendencias en detección de señales.

## API
`GET /api/intelligence/churn-predictions?tenantId=&customerId=`
