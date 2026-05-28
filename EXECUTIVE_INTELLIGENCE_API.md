# EXECUTIVE INTELLIGENCE API

## Endpoint principal
```
GET /api/intelligence/dashboard?tenantId={guid}
Authorization: Bearer / Cookie
```

## Respuesta `ExecutiveIntelligenceDashboardDto`
- ProductAnalytics (DAU/WAU/MAU/Stickiness)
- NPS + CSAT
- Health overview (top 20)
- Churn predictions V2 (top 15)
- Expansion trends (top 15)
- Segmentation (50 cuentas)
- Top insights (20)
- Feedback summary

## Endpoints adicionales
Ver `IntelligenceController`: product-analytics, nps, csat, insights, churn-predictions, expansion, segmentation, feedback, trends, POST nps/csat/usage/scan.

**100 % desde PostgreSQL.**
