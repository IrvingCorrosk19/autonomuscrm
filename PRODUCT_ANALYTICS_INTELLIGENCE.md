# PRODUCT ANALYTICS & CUSTOMER INTELLIGENCE — Fase 14

## Visión
AutonomusFlow evoluciona de **Customer Retention Engine** a **Customer Intelligence Platform**: aprende de actividad, feedback y snapshots para predecir churn, detectar expansión y automatizar decisiones.

## Alcance
| Bloque | Servicio | API |
|--------|----------|-----|
| Product Analytics | `IProductAnalyticsEngine` | `/api/intelligence/product-analytics` |
| NPS | `INpsEngine` | `/api/intelligence/nps` |
| CSAT | `ICsatEngine` | `/api/intelligence/csat` |
| Insights | `ICustomerInsightsEngine` | `/api/intelligence/insights` |
| Usage Intelligence | `IProductUsageIntelligence` | (dashboard) |
| Churn V2 | `IChurnPredictionV2` | `/api/intelligence/churn-predictions` |
| Expansion Intel | `IExpansionIntelligence` | `/api/intelligence/expansion` |
| Segmentation | `ICustomerSegmentationEngine` | `/api/intelligence/segmentation` |
| Feedback | `IFeedbackEngine` | `/api/intelligence/feedback` |
| Data Mart | `ICustomerDataMartService` | `/api/intelligence/trends` |
| Executive | `IExecutiveIntelligenceDashboardService` | **`GET /api/intelligence/dashboard`** |
| AI Agent | `CustomerInsightsAgent` | scan 15 min |

## Tablas nuevas
`ProductUsageEvents`, `CustomerFeedbacks`, `CustomerAnalyticsSnapshots`

## Migración
`Phase14_ProductAnalytics`

## Sin UI
100 % API + Worker + BD.
