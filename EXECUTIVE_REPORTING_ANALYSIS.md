# EXECUTIVE_REPORTING_ANALYSIS

## Pregunta de negocio
**¿Un CEO puede dirigir la empresa usando estos datos?**

**Respuesta: No de forma fiable.** Puede ver **snapshot operativo** en `/Index` si hay datos, pero le faltan tendencias, forecast consolidado, retención, y una sola fuente de verdad sin mocks.

---

## Fuentes de reporting

| Fuente | ¿Usar para decisiones? |
|--------|------------------------|
| `/` Index.cshtml | **Sí** (KPIs DB) |
| `/Deals` | **Sí** (pipeline + forecast 30d) |
| `/Customers` stats | **Sí** (conteos LTV/risk) |
| `/Audit` | **Sí** (compliance operativo) |
| `/Dashboard` | **No** — datos inventados |
| Sidebars Agents/Workflows/Customers | **No** — ficticios |
| API Metrics/TimeSeries | **Vacío** — sin writers |

---

## KPIs disponibles (reales)

| KPI | Cálculo | Limitación |
|-----|---------|------------|
| Leads nuevos 24h | Count | Sin tendencia |
| Tasa conversión | Qualified/Total | No es win rate |
| Deals en riesgo | Prob < 50 | No usa agent metadata |
| Ingresos estimados | Suma deals abiertos | No ponderado |
| Pipeline por etapa | Suma montos | Sin histórico |
| Forecast 30d | Ponderado | Solo en Deals |
| Clientes alto riesgo | Risk > 70 | Sin evolución |

---

## KPIs que un CEO necesita y faltan

| KPI | Importancia |
|-----|-------------|
| ARR / MRR | Crítico |
| Revenue closed (mes/trimestre) | Crítico |
| Win rate | Crítico |
| Average deal size | Alto |
| Sales cycle days | Alto |
| CAC / LTV ratio | Alto |
| Churn rate | Crítico SaaS |
| NRR / GRR | Crítico SaaS |
| Pipeline coverage vs quota | Crítico |
| Forecast accuracy | Alto |

---

## Forecast & revenue

- **Real:** forecast 30d ponderado en Deals.
- **Mock:** panel 30/60/90 en Deals.
- **Index EstimatedRevenue:** bruto, no probabilístico — **sobreestima** para CEO.

**Riesgo de decisión:** CEO ve número optimista sin peso de probabilidad.

---

## Productividad comercial

No hay:
- Actividades por rep
- Tareas completadas / vencidas
- Tiempo en etapa
- Leaderboard

---

## Exportación y reportes

- Audit: export JSON eventos ✓
- PDF / Excel ejecutivo: **no**
- Reportes programados: **no**
- Dashboard CEO móvil: depende UI responsive (existe) pero datos incompletos

---

## Recomendación ejecutiva

1. **Unificar** en “Executive View” solo métricas DB; retirar o marcar `/Dashboard` como DEMO.
2. **P0:** Revenue closed + pipeline weighted + churn count (real).
3. **P1:** Poblar TimeSeriesMetrics desde eventos.
4. **P2:** PDF semanal automático a dirección.

---

## Madurez reporting

| Nivel | Estado |
|-------|--------|
| Operativo (rep) | 60% |
| Táctico (gerente) | 40% |
| Estratégico (CEO) | 20% |
