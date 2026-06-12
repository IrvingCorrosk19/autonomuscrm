# PHASE2_DATABASE_AUDIT — AutonomusCRM

**Fecha:** 2026-06-12  
**Fase:** 2 — Optimización avanzada enterprise  
**Motor:** PostgreSQL @ `autonomuscrm` (local)

---

## 1. Resumen ejecutivo

| Área | Estado Fase 1 | Estado Fase 2 |
|------|---------------|---------------|
| Carga masiva `GetByTenantIdAsync().ToList()` en engines analytics | Riesgo medio | **Eliminado** en 6 engines objetivo + `CustomerHealthEngine` |
| Agregación en PostgreSQL | Parcial | **Completa** en hot paths revenue/CS |
| Round-trips por request analytics | Alto (3–5 + N usuarios) | **2–5** consultas acotadas |
| Índices ILike / búsqueda | Sin trigram | **GIN pg_trgm** en Leads, Customers, Deals, Users |
| Particionamiento DomainEvents | No evaluado | **Evaluado** (ver §5) |
| Migración aplicada | `QueryPathCompositeIndexes` | `Phase2AdvancedDatabaseOptimization` |

---

## 2. Engines refactorizados (sin carga tenant completa)

| Engine | Antes | Después |
|--------|-------|---------|
| `RevenueForecastEngine` | Todos los deals en memoria | `GetWinRateCountsAsync` + `GetForecastHorizonsAsync` |
| `SalesPerformanceEngine` | Deals + users + N×quota | `GetRepPerformanceAggregatesAsync` + `GetActiveUserSummariesAsync` + quotas batch |
| `CustomerJourneyEngine` | Leads + deals + customers + `CalculateAllAsync` | `CountByTenantAsync` + `GetJourneyCustomerCountsAsync` + `GetJourneyDealMetricsAsync` + `GetAverageHealthScoreAsync` |
| `SmartAssignmentEngine` | Users + leads + deals completos | Proyecciones activas + `GetActiveAssignmentLoadByUserAsync` + `GetOpenAssignmentLoadByUserAsync` |
| `RevenuePredictionModelService` | Deals completos para ML features | `GetWonRevenueMonthlyAverageAsync` + `GetOpenPipelineAmountSumAsync` + forecast engine |
| `ExpansionRevenueEngine` | Health + customers + deals | Health agregado + `GetExpansionCustomerProjectionsAsync` + `GetWonAmountByCustomerAsync` |
| `CustomerHealthEngine` *(dependencia crítica)* | 3× tenant completo | Proyecciones + GROUP BY deals/tasks |

---

## 3. Nuevos métodos de repositorio (agregados SQL)

### `IDealRepository`
- `GetWinRateCountsAsync`
- `GetForecastHorizonsAsync`
- `GetRepPerformanceAggregatesAsync`
- `GetOpenPipelineAmountSumAsync`
- `GetWonRevenueMonthlyAverageAsync`
- `GetWonAmountByCustomerAsync` / `GetWonAmountForCustomerAsync`
- `GetJourneyDealMetricsAsync`
- `GetOpenAssignmentLoadByUserAsync`

### `ILeadRepository`
- `GetActiveAssignmentLoadByUserAsync`

### `IUserRepository`
- `GetActiveUserSummariesAsync`

### `ICustomerRepository`
- `GetJourneyCustomerCountsAsync` (jsonb `?` operator vía SQL)
- `GetHealthEligibleProjectionsAsync`
- `GetExpansionCustomerProjectionsAsync`

### `IWorkflowTaskRepository`
- `GetHealthTaskAggregatesByCustomerAsync` (JOIN Deals para tareas ligadas)
- `GetHealthTaskAggregateForCustomerAsync` (scoped, sin scan tenant)

### `ISalesQuotaRepository`
- `GetActiveMonthlyQuotaTargetsAsync` (batch)

---

## 4. Índices — Fase 2

### Migración `20260612100745_Phase2AdvancedDatabaseOptimization`

| Índice | Tipo | Tabla | Motivo |
|--------|------|-------|--------|
| `IX_Deals_TenantId_AssignedToUserId` | B-tree compuesto | Deals | Leaderboard, asignación inteligente |
| `IX_Leads_Name_trgm` | GIN trigram | Leads | `ILike` en búsqueda |
| `IX_Leads_Email_trgm` | GIN trigram | Leads | `ILike` |
| `IX_Leads_Company_trgm` | GIN trigram | Leads | `ILike` |
| `IX_Customers_Name_trgm` | GIN trigram | Customers | `ILike` |
| `IX_Customers_Email_trgm` | GIN trigram | Customers | `ILike` |
| `IX_Customers_Company_trgm` | GIN trigram | Customers | `ILike` |
| `IX_Deals_Title_trgm` | GIN trigram | Deals | `ILike` |
| `IX_Users_Email_trgm` | GIN trigram | Users | `ILike` |

Extensión: `CREATE EXTENSION IF NOT EXISTS pg_trgm`

**Nota:** Con tablas pequeñas en dev el planner puede seguir eligiendo seq scan; beneficio principal a >10k filas por tenant.

---

## 5. DomainEvents — evaluación de particionamiento

| Métrica (dev) | Valor |
|---------------|-------|
| Tamaño actual | ~184 kB |
| Filas vivas | ~21 |
| Índice existente | `IX_DomainEvents_TenantId_OccurredOn` |

### Recomendación (>1M eventos / tenant)

1. **Particionamiento RANGE por `OccurredOn`** (mensual o trimestral).
2. Mantener PK compuesta `(Id, OccurredOn)` o usar tabla particionada con `PARTITION BY RANGE`.
3. Política de retención: detach + archive a cold storage (>24 meses).
4. **No implementado en código** en Fase 2 — volumen dev insuficiente; script de análisis en `ops/database/phase2-scalability-explain.sql`.

### Umbral sugerido para activar

- >500k filas en `DomainEvents` **o**
- >2 GB tabla + queries de auditoría con filtro temporal frecuente

---

## 6. Deuda residual (fuera de alcance Fase 2)

Servicios que aún cargan tenant completo (candidatos Fase 3):

- `SalesProductivityService`
- `CustomerKpiService`
- `DataQualityRevenueService`
- `RetentionAutomationEngine`
- `WinLossAnalyticsService` (agrupa en memoria tras carga)
- `ChurnRiskEngine` / varios intelligence dashboards

---

## 7. Verificación

- **Build:** OK  
- **Migración:** `Phase2AdvancedDatabaseOptimization` aplicada  
- **Tests unitarios:** 210 passed (`FullyQualifiedName!~Integration&FullyQualifiedName!~Phase4OperationalValidation`)  
- **Integración/E2E:** requieren Docker / credenciales / `INTEGRATION_TEST_CONNECTION_STRING`
