# REVENUE_OPERATIONS_FOUNDATION

## Fase 12 — Revenue Operations Foundation

## Estado
AutonomusFlow evoluciona de **CRM operacional** a **Revenue Engine** (capa de servicios + API, sin UI).

## Componentes entregados
| Motor | Servicio | API |
|-------|----------|-----|
| Forecast | `IRevenueForecastEngine` | `GET /api/revenue/forecast` |
| Performance | `ISalesPerformanceEngine` | `GET /api/revenue/leaderboard` |
| Coverage | `IPipelineCoverageService` | `GET /api/revenue/pipeline-coverage` |
| Win/Loss | `IWinLossAnalyticsService` | `GET /api/revenue/win-loss` |
| Productivity | `ISalesProductivityService` | `GET /api/revenue/productivity` |
| SLA | `ICommercialSlaEngine` | `GET /api/revenue/sla-breaches` |
| KPIs | `IRevenueKpiService` | `GET /api/revenue/kpis` |
| Executive | `IExecutiveSalesDashboardService` | `GET /api/revenue/dashboard` |
| Cuotas | `UpsertSalesQuotaCommand` | `POST /api/revenue/quotas` |
| Automations | `IRevenueAutomationEngine` | eventos + scan 15min |
| Intelligence | `ISalesIntelligenceService` | vía DealStrategyAgent |
| Data Quality | `IDataQualityRevenueService` | `POST /api/revenue/scan` |

## Entidad nueva
`SalesQuota` — metas Monthly/Quarterly/Yearly por vendedor.

## Integración eventos
`DomainEventDispatcher`: workflows → operational → **revenue** → bus.

## Build
OK. Migración `Phase12_SalesQuotas`.
