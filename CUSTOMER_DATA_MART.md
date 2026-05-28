# CUSTOMER DATA MART

Entidad: `CustomerAnalyticsSnapshot`

## Campos históricos diarios
Health, ChurnRisk, NPS, CSAT, Revenue, ExpansionScore, Segment, Engagement, Adoption, ActiveUsers

## Servicio
`ICustomerDataMartService` → `CustomerDataMartService`

- `BuildDailySnapshotsAsync` — idempotente por día/cliente
- `GetTrendsAsync` — series 90d por cliente o tenant

## Índice único
`(TenantId, CustomerId, SnapshotDate)`

Alimenta Churn V2 y tendencias ejecutivas.
