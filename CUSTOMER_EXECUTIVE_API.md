# CUSTOMER EXECUTIVE API

## Endpoint principal
```
GET /api/customer/dashboard?tenantId={guid}
Authorization: Bearer / Cookie
```

## Respuesta (`ExecutiveCustomerDashboardDto`)
- `Kpis` — 10 KPIs customer
- `HealthSummary` — top 25 cuentas con scores
- `TopChurnSignals` — 15 señales prioritarias
- `UpcomingRenewals` — 20 renovaciones
- `ExpansionOpportunities` — 15 oportunidades
- `JourneyMetrics` — embudo post-venta
- `RenewalForecast90` — ARR renovación 90d

## Endpoints adicionales
| Método | Ruta |
|--------|------|
| GET | `/api/customer/health` |
| GET | `/api/customer/churn-signals` |
| GET | `/api/customer/renewals` |
| GET | `/api/customer/expansion` |
| GET | `/api/customer/journey` |
| GET | `/api/customer/kpis` |
| POST | `/api/customer/scan` |
| POST | `/api/customer/playbooks/{type}` |
| POST | `/api/customer/intelligence/{agent}/{customerId}` |

**100 % datos desde PostgreSQL** — sin agregación UI.
