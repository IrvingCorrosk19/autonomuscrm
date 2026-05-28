# REVENUE_KPI_FRAMEWORK

## 10 KPIs (`RevenueKpiSnapshotDto`)
| # | KPI | Fuente |
|---|-----|--------|
| 1 | Revenue Closed | ClosedWon sum |
| 2 | Win Rate | won/(won+lost) |
| 3 | Average Deal Size | revenue/won count |
| 4 | Sales Cycle | avg days won |
| 5 | Forecast Accuracy Proxy | closed/forecast90 |
| 6 | Pipeline Coverage | team coverage % |
| 7 | Conversion Rate | qualified/total leads |
| 8 | Revenue per Rep | closed/active users |
| 9 | Lost Revenue | ClosedLost sum |
| 10 | Recovery Pipeline | open weighted |

## API
`GET /api/revenue/kpis?tenantId=`
