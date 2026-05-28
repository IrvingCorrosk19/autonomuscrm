# PRODUCT ANALYTICS ENGINE

`IProductAnalyticsEngine` → `ProductAnalyticsEngine`

## KPIs
- **DAU / WAU / MAU** — usuarios con login (LastLoginAt + eventos)
- **Stickiness** — DAU/MAU × 100
- **Avg session minutes** — eventos `session`
- **Usage by module** — Leads, Deals, Customers, Tasks, etc.

## Ingesta
- `POST /api/intelligence/usage?module=&eventType=`
- `SyncFromUserLoginsAsync` en scan periódico

## Eventos
`login`, `session`, `feature`
