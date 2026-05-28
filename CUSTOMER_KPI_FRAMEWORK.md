# CUSTOMER KPI FRAMEWORK

## Servicio
`ICustomerKpiService` → `CustomerKpiService`

## 10 KPIs (`CustomerKpiSnapshotDto`)
1. **Health Score** — promedio health tenant
2. **Churn Risk** — % clientes no Healthy
3. **Renewal Rate** — proxy contratos activos/renovados
4. **Retention Rate** — activos vs churned
5. **Expansion Revenue** — deals metadata ExpansionDeal
6. **Upsell Revenue** — proxy oportunidades Upsell
7. **Cross-Sell Revenue** — proxy CrossSell
8. **Customer Lifetime Value** — suma LTV
9. **Adoption Score** — promedio componente adopción
10. **Engagement Score** — promedio componente engagement

## API
`GET /api/customer/kpis?tenantId=`

Incluido en dashboard ejecutivo.
